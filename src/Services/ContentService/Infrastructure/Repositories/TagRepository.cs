using SearchService.Application.DTOs;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Entities;
using OrderingService.Application.DTOs;
using OrderingService.Application.Interfaces;
using OrderingService.Domain.Entities;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using LearningService.Application.Services;
using LearningService.Application.Interfaces;
using LearningService.Domain.Entities;
using InteractionService.Application.DTOs;
using InteractionService.Application.Interfaces;
using InteractionService.Domain.Enums;
using InteractionService.Domain.Entities;
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using SearchService.Application.Services;
using SearchService.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using src.Shared.Domain.Entities;
using src.Shared.Resources;

namespace ContentService.Infrastructure.Repositories
{
    public class TagRepository : ITagRepository
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly ILuceneSearchService _luceneSearchService;

        public TagRepository(AppDbContext context, IStringLocalizer<SharedResources> localizer, ILuceneSearchService luceneSearchService)
        {
            _localizer = localizer;
            _context = context;
            _luceneSearchService = luceneSearchService;
        }
        
        public async Task<ApiResponse> GetAllTagsAsync()
        {
            var tags = await _context.Tags.ToListAsync();
            return new ApiResponse("Success", _localizer["Success"].Value, tags, true);
        }

        public async Task<ApiResponse> CreateTagAsync(CreateTagDTO createTagDTO)
        {
            var existingTag = await _context.Tags
                .FirstOrDefaultAsync(t => t.Name.ToLower() == createTagDTO.Name.ToLower());

            if (existingTag != null)
            {
                return new ApiResponse("Conflict", _localizer["TagAlreadyExists"].Value, null, false);
            }

            var newTag = new Tag
            {
                Id = Guid.NewGuid().ToString(),
                Name = createTagDTO.Name,
                Description = createTagDTO.Description
            };

            _context.Tags.Add(newTag);

            await _context.SaveChangesAsync();

            return new ApiResponse("Created", _localizer["CreateTagSuccess"].Value, newTag, true);
        }

        public async Task<ApiResponse> DeleteTagAsync(string tagId)
        {
            var tag = await _context.Tags.FindAsync(tagId);
            if (tag == null)
            {
                return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
            }

            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();

            return new ApiResponse("Success", _localizer["TagDeleted"].Value, null, true);
        }

        public async Task<ApiResponse> AssignTagToCourseAsync(AssignTagToCourseDTO assignTagToCourseDTO)
        {
            var course = await _context.Courses
                .Include(c => c.CourseTags)
                .Include(c => c.Instructor)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Comments)
                .FirstOrDefaultAsync(c => c.Id == assignTagToCourseDTO.CourseId);

            if (course == null)
            {
                return new ApiResponse("NotFound", _localizer["CourseNotFound"].Value, null, false);
            }

            foreach (var tagId in assignTagToCourseDTO.TagId)
            {
                var tag = await _context.Tags.FindAsync(tagId);
                if (tag == null)
                {
                    return new ApiResponse("NotFound", _localizer["TagNotFound"].Value, null, false);
                }

                if (!course.CourseTags.Any(ct => ct.TagId == tagId))
                {
                    course.CourseTags.Add(new CourseTag
                    {
                        CourseId = course.Id,
                        TagId = tagId
                    });
                }
            }

            await _context.SaveChangesAsync();

            try
            {
                await _luceneSearchService.IndexCourseAsync(course);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Indexing failed after tag assignment: {ex.Message}");
            }

            return new ApiResponse("Success", _localizer["TagAssignedSuccessfully"].Value, null, true);
        }
    }
}



