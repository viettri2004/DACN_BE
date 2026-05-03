using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using CourseService.Domain.Entities;
using CourseService.Domain.Enums;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Shared.Domain.Entities;
using src.Shared.Domain.Entities;
using src.Shared.Resources;
using Hangfire;

namespace CourseService.Application.Services
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly ILuceneSearchService _luceneSearchService;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public CommentService(ICommentRepository commentRepository, 
                              ICourseRepository courseRepository,
                              IStringLocalizer<SharedResources> localizer,
                              ILuceneSearchService luceneSearchService,
                              IBackgroundJobClient backgroundJobClient)
        {
            _commentRepository = commentRepository;
            _courseRepository = courseRepository;
            _localizer = localizer;
            _luceneSearchService = luceneSearchService;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task<ApiResponse> GetCourseCommentsAsync(string courseId, string? userId, CommentType type, int pageNumber, int pageSize, int? rating = null)
        {
            var ratingQuery = _commentRepository.GetCommentsQueryable()
                .AsNoTracking()
                .Where(c => c.CourseId == courseId && c.Type == type && c.ReplyId == null);

            var ratingsList = await ratingQuery.Select(c => c.Rate).ToListAsync();
            var totalRatings = ratingsList.Count;
            var averageRating = totalRatings > 0 ? Math.Round(ratingsList.Average(), 1) : 0.0;
            var star5Count = ratingsList.Count(r => r == 5);
            var star4Count = ratingsList.Count(r => r == 4);
            var star3Count = ratingsList.Count(r => r == 3);
            var star2Count = ratingsList.Count(r => r == 2);
            var star1Count = ratingsList.Count(r => r == 1);

            var query = _commentRepository.GetCommentsQueryable()
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Replies).ThenInclude(r => r.User)
                .Where(c => c.CourseId == courseId && c.Type == type && c.ReplyId == null);

            if (rating.HasValue) query = query.Where(c => c.Rate == rating.Value);

            var totalCount = await query.CountAsync();
            var comments = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CommentDTO
                {
                    CommentId = c.Id,
                    UserName = c.User.FullName,
                    AvatarUrl = c.User.AvatarUrl,
                    Content = c.Content,
                    Rate = c.Rate,
                    Timestamp = c.CreatedAt,
                    IsMyComment = userId != null && c.UserId == userId,
                    Replies = c.Replies.Select(r => new ReplyDTO
                    {
                        CommentId = r.Id,
                        Content = r.Content,
                        Timestamp = r.CreatedAt,
                        IsMyComment = userId != null && r.UserId == userId
                    }).ToList()
                }).ToListAsync();

            var pagedResult = new PagedCommentResultDTO 
            { 
                Items = comments, 
                Page = pageNumber, 
                PageSize = pageSize, 
                TotalCount = totalCount,
                AverageRating = averageRating,
                TotalRatingCount = totalRatings,
                Star5Count = star5Count,
                Star4Count = star4Count,
                Star3Count = star3Count,
                Star2Count = star2Count,
                Star1Count = star1Count
            };
            return new ApiResponse("Success", _localizer["Success"].Value, pagedResult, true);
        }

        public async Task<ApiResponse> AddCommentAsync(AddCommentDTO addCommentDTO, string userId)
        {
            var enrollment = await _courseRepository.GetEnrollmentAsync(userId, addCommentDTO.CourseId);
            if (enrollment == null) 
                return new ApiResponse("Forbidden", _localizer["NotEnrolledInCourse"].Value, null, false);

            var comment = new Comment
            {
                Id = Guid.NewGuid().ToString(),
                Content = addCommentDTO.Content,
                Rate = addCommentDTO.Type == CommentType.Review ? addCommentDTO.Rate : 0,
                EnrollmentId = enrollment.Id,
                UserId = userId,
                CourseId = addCommentDTO.CourseId,
                CreatedAt = DateTime.UtcNow,
                Type = addCommentDTO.Type
            };

            await _commentRepository.AddAsync(comment);
            await _commentRepository.SaveChangesAsync();

            // Re-index async if it's a review (affects rating)
            if (addCommentDTO.Type == CommentType.Review)
            {
                var course = await _courseRepository.GetByIdAsync(addCommentDTO.CourseId);
                if (course != null)
                    _backgroundJobClient.Enqueue(() => _luceneSearchService.IndexCourseAsync(course.Id));
            }

            return new ApiResponse("Created", _localizer["Success"].Value, comment.Id, true);
        }

        public async Task<ApiResponse> UpdateCommentAsync(string commentId, UpdateCommentDTO updateCommentDTO, string userId)
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null) return new ApiResponse("NotFound", _localizer["CommentNotFound"].Value, null, false);
            if (comment.UserId != userId) return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);

            comment.Content = updateCommentDTO.Content;
            if (comment.Type == CommentType.Review) comment.Rate = updateCommentDTO.Rate;
            comment.UpdatedAt = DateTime.UtcNow;

            await _commentRepository.UpdateAsync(comment);
            await _commentRepository.SaveChangesAsync();

            return new ApiResponse("Success", _localizer["CommentUpdated"].Value, null, true);
        }

        public async Task<ApiResponse> DeleteCommentAsync(string commentId, string userId)
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null) return new ApiResponse("NotFound", _localizer["CommentNotFound"].Value, null, false);
            if (comment.UserId != userId) return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);

            await _commentRepository.DeleteAsync(comment);
            await _commentRepository.SaveChangesAsync();

            return new ApiResponse("Success", _localizer["CommentDeleted"].Value, null, true);
        }

        public async Task<ApiResponse> ReplyToCommentAsync(AddReplyCommentDTO replyDTO, string userId)
        {
            var parent = await _commentRepository.GetByIdAsync(replyDTO.ParentCommentId);
            if (parent == null) return new ApiResponse("NotFound", _localizer["ParentCommentNotFound"].Value, null, false);

            var reply = new Comment
            {
                Id = Guid.NewGuid().ToString(),
                Content = replyDTO.Content,
                UserId = userId,
                CourseId = parent.CourseId,
                ReplyId = parent.Id,
                CreatedAt = DateTime.UtcNow,
                Type = CommentType.Reply
            };

            await _commentRepository.AddAsync(reply);
            await _commentRepository.SaveChangesAsync();

            return new ApiResponse("Created", _localizer["ReplyAdded"].Value, null, true);
        }
    }
}
