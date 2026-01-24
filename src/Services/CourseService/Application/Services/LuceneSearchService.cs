using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using CourseService.Domain.Enums;
using Data.Context;
using Entities;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.QueryParsers.Classic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Shared.Domain.Entities;
using src.Shared.Domain.Entities;
using src.Shared.Resources;

namespace CourseService.Application.Services
{
    public class LuceneSearchService : ILuceneSearchService
    {
        private const LuceneVersion LUCENE_VERSION = LuceneVersion.LUCENE_48;
        private readonly FSDirectory _directory;
        private readonly StandardAnalyzer _analyzer;
        private readonly IndexWriter _writer;
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly SearcherManager _searcherManager;

        public LuceneSearchService(
            AppDbContext context,
            IStringLocalizer<SharedResources> localizer,
            IWebHostEnvironment env)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));

            var indexPath = Path.Combine(env.ContentRootPath, "lucene_index");
            _directory = FSDirectory.Open(indexPath);
            _analyzer = new StandardAnalyzer(LUCENE_VERSION);

            var indexConfig = new IndexWriterConfig(LUCENE_VERSION, _analyzer)
            {
                OpenMode = OpenMode.CREATE_OR_APPEND
            };

            _writer = new IndexWriter(_directory, indexConfig);
            _searcherManager = new SearcherManager(_writer, applyAllDeletes: true, null);
        }
        public async Task<ApiResponse> SearchCoursesAsync(CourseSearchDTO searchDto, string studentId)
        {
            _searcherManager.MaybeRefreshBlocking();
            var searcher = _searcherManager.Acquire();

            try
            {
                Query finalQuery;

                if (!string.IsNullOrWhiteSpace(searchDto?.SearchTerm))
                {
                    var boolQuery = new BooleanQuery();
                    var searchTerm = searchDto.SearchTerm.ToLowerInvariant().Trim();

                    var terms = searchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var term in terms)
                    {
                        var termBoolQuery = new BooleanQuery();

                        var nameQuery = new WildcardQuery(new Term("name", $"*{term}*"));
                        termBoolQuery.Add(nameQuery, Occur.SHOULD);

                        var instructorQuery = new WildcardQuery(new Term("instructorName", $"*{term}*"));
                        termBoolQuery.Add(instructorQuery, Occur.SHOULD);

                        boolQuery.Add(termBoolQuery, Occur.MUST);
                    }

                    finalQuery = boolQuery;
                }
                else
                {
                    finalQuery = new MatchAllDocsQuery();
                }

                var page = Math.Max(1, searchDto?.Page ?? 1);
                var pageSize = Math.Max(1, searchDto?.PageSize ?? 10);
                int numHits = Math.Max(1, page * pageSize);

                TopDocs topDocs = searcher.Search(finalQuery, numHits);
                var totalHits = topDocs.TotalHits;

                var pagedScoreDocs = topDocs.ScoreDocs
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var courseIds = new List<string>();
                foreach (var scoreDoc in pagedScoreDocs)
                {
                    var doc = searcher.Doc(scoreDoc.Doc);
                    var id = doc.Get("id");
                    if (!string.IsNullOrEmpty(id)) courseIds.Add(id);
                }

                if (courseIds.Count == 0)
                {
                    var emptyResult = new PagedResult<CourseCardDTO>
                    {
                        Items = new List<CourseCardDTO>(),
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalHits
                    };
                    return new ApiResponse("Success", _localizer["Success"].Value, emptyResult, true);
                }

                var coursesQuery = _context.Courses
                    .AsNoTracking()
                    .Where(c => courseIds.Contains(c.Id) && c.Status != CourseStatus.Private)
                    .Include(c => c.Instructor)
                    .Include(c => c.Enrollments)
                        .ThenInclude(e => e.Comments);

                var coursesFromDb = await coursesQuery.ToListAsync();

                if (!string.IsNullOrEmpty(studentId))
                {
                    var enrolledCourseIds = await _context.Enrollments
                        .Where(e => e.StudentId == studentId && e.Status == true)
                        .Select(e => e.CourseId)
                        .ToListAsync();
                    
                    coursesFromDb = coursesFromDb.Where(c => !enrolledCourseIds.Contains(c.Id)).ToList();
                }

                var coursesWithPrice = coursesFromDb.Select(c =>
                {
                    var calculatedPrice = CalculatePrice(c);
                    return new
                    {
                        Course = c,
                        Price = calculatedPrice
                    };
                }).AsQueryable();

                if (searchDto.MinPrice.HasValue)
                {
                    coursesWithPrice = coursesWithPrice.Where(x => x.Price >= searchDto.MinPrice.Value);
                }

                if (searchDto.MaxPrice.HasValue)
                {
                    coursesWithPrice = coursesWithPrice.Where(x => x.Price <= searchDto.MaxPrice.Value);
                }

                switch (searchDto.SortBy?.ToLower())
                {
                    case "rating":
                        coursesWithPrice = coursesWithPrice.OrderByDescending(x => x.Course.Enrollments.SelectMany(e => e.Comments).Any()
                                                        ? x.Course.Enrollments.SelectMany(e => e.Comments).Average(cm => cm.Rate)
                                                        : 0);
                        break;
                    case "newest":
                        coursesWithPrice = coursesWithPrice.OrderByDescending(x => x.Course.CreateTime);
                        break;
                    case "priceasc":
                        coursesWithPrice = coursesWithPrice.OrderBy(x => x.Price);
                        break;
                    case "pricedesc":
                        coursesWithPrice = coursesWithPrice.OrderByDescending(x => x.Price);
                        break;
                    case "popularity":
                    default:
                        coursesWithPrice = coursesWithPrice.OrderByDescending(x => x.Course.Enrollments.Count);
                        break;
                }

                var sortedCourses = coursesWithPrice.ToList();

                var pagedSortedCourses = sortedCourses
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var pagedCoursesData = pagedSortedCourses.Select(x =>
                {
                    var course = x.Course;
                    var calculatedPrice = x.Price;
                    var comments = (course.Enrollments ?? new List<Enrollment>())
                        .SelectMany(e => e.Comments ?? new List<Comment>())
                        .ToList();
                    var avgRating = comments.Any() ? comments.Average(cm => cm.Rate) : 0;

                    var dto = new CourseCardDTO
                    {
                        Id = course.Id,
                        Name = course.Name,
                        ImageUrl = course.ImageUrl,
                        InstructorName = course.Instructor?.FullName ?? string.Empty,
                        AverageRating = Math.Round(avgRating, 1),
                        TotalReviews = comments.Count,
                        TotalStudents = course.Enrollments?.Count ?? 0,
                        OriginalPrice = course.Price,
                        Price = calculatedPrice,
                        IsBestseller = (course.Enrollments?.Count ?? 0) > 5,
                        TotalHours = 25,
                        // Status = course.Status.ToString()
                    };

                    if (dto.Price == dto.OriginalPrice)
                        dto.OriginalPrice = null;

                    return dto;
                }).ToList();

                var pagedResult = new PagedResult<CourseCardDTO>
                {
                    Items = pagedCoursesData,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = sortedCourses.Count
                };

                return new ApiResponse("Success", _localizer["Success"].Value, pagedResult, true);
            }
            finally
            {
                _searcherManager.Release(searcher);
            }
        }
        private Sort GetSort(string? sortBy)
        {
            return sortBy?.ToLowerInvariant() switch
            {
                "rating" => new Sort(new SortField("averageRating", SortFieldType.DOUBLE, true)),
                "newest" => new Sort(new SortField("createTime", SortFieldType.INT64, true)),
                "priceasc" => new Sort(new SortField("calculatedPrice", SortFieldType.DOUBLE, false)),
                "pricedesc" => new Sort(new SortField("calculatedPrice", SortFieldType.DOUBLE, true)),
                "popularity" => new Sort(new SortField("totalStudents", SortFieldType.INT32, true)),
                "relevance" => new Sort(SortField.FIELD_SCORE),
                _ => new Sort(new SortField("totalStudents", SortFieldType.INT32, true))
            };
        }

        public Task IndexCourseAsync(Course course)
        {
            var calculatedPrice = CalculatePrice(course);
            var comments = (course.Enrollments ?? new List<Enrollment>()).SelectMany(e => e.Comments ?? new List<Comment>()).ToList();
            var avgRating = comments.Any() ? comments.Average(c => c.Rate) : 0;
            var enrollmentCount = course.Enrollments?.Count ?? 0;

            var doc = new Lucene.Net.Documents.Document
            {
                new StringField("id", course.Id ?? string.Empty, Field.Store.YES),
                new TextField("name", course.Name ?? string.Empty, Field.Store.NO),
                new TextField("description", course.Description ?? string.Empty, Field.Store.NO),
                new TextField("instructorName", course.Instructor?.FullName ?? string.Empty, Field.Store.NO),
                new DoubleField("price", (double)course.Price, Field.Store.NO),
                new DoubleField("calculatedPrice", (double)calculatedPrice, Field.Store.NO),
                new DoubleField("averageRating", avgRating, Field.Store.NO),
                new Int32Field("totalStudents", enrollmentCount, Field.Store.NO),
                new Int32Field("totalReviews", comments.Count, Field.Store.NO),
                new Int64Field("createTime", course.CreateTime.Ticks, Field.Store.NO),
            };

            var tags = course.CourseTags?.Select(ct => ct.TagId.ToLowerInvariant()) ?? Enumerable.Empty<string>();
            foreach (var tag in tags)
                doc.Add(new StringField("tags", tag, Field.Store.NO));

            _writer.UpdateDocument(new Term("id", course.Id ?? string.Empty), doc);

            return Task.CompletedTask;
        }

        public async Task IndexAllCoursesAsync()
        {
            try
            {
                _writer.DeleteAll();
                _writer.Commit();

                const int batchSize = 1000;
                int page = 0;
                int totalIndexed = 0;
                List<Course> courses;

                do
                {
                    courses = await _context.Courses
                        .Include(c => c.Instructor)
                        .Include(c => c.CourseTags)
                        .Include(c => c.Enrollments)
                            .ThenInclude(e => e.Comments)
                        .AsNoTracking()
                        .OrderBy(c => c.Id)
                        .Skip(page * batchSize)
                        .Take(batchSize)
                        .ToListAsync();

                    if (courses.Count == 0) break;

                    foreach (var course in courses)
                    {
                        var calculatedPrice = CalculatePrice(course);
                        var comments = (course.Enrollments ?? new List<Enrollment>())
                            .SelectMany(e => e.Comments ?? new List<Comment>())
                            .ToList();
                        var avgRating = comments.Any() ? comments.Average(c => c.Rate) : 0;

                        var doc = new Lucene.Net.Documents.Document
                {
                    new StringField("id", course.Id ?? string.Empty, Field.Store.YES),
                    new TextField("name", course.Name ?? string.Empty, Field.Store.NO),
                    new TextField("description", course.Description ?? string.Empty, Field.Store.NO),
                    new TextField("instructorName", course.Instructor?.FullName ?? string.Empty, Field.Store.NO),
                    new DoubleField("price", (double)course.Price, Field.Store.NO),
                    new DoubleField("calculatedPrice", (double)calculatedPrice, Field.Store.NO),
                    new DoubleField("averageRating", avgRating, Field.Store.NO),
                    new Int32Field("totalStudents", course.Enrollments?.Count ?? 0, Field.Store.NO),
                    new Int32Field("totalReviews", comments.Count, Field.Store.NO),
                    new Int64Field("createTime", course.CreateTime.Ticks, Field.Store.NO),
                };

                        foreach (var tag in course.CourseTags ?? Enumerable.Empty<CourseTag>())
                            doc.Add(new StringField("tags", tag.TagId.ToLowerInvariant(), Field.Store.NO));

                        _writer.AddDocument(doc);
                    }

                    _writer.Commit();
                    totalIndexed += courses.Count;

                    page++;

                } while (courses.Count == batchSize);

                _searcherManager.MaybeRefresh();

                Console.WriteLine($"Successfully indexed {totalIndexed} courses");
            }
            catch (Exception ex)
            {
                _writer.Rollback();
                Console.WriteLine($"Error indexing courses: {ex.Message}");
                throw;
            }
        }

        public Task DeleteCourseFromIndexAsync(string courseId)
        {
            _writer.DeleteDocuments(new Term("id", courseId ?? string.Empty));

            return Task.CompletedTask;
        }

        private decimal CalculatePrice(Course course)
        {
            if (course == null) return 0m;
            if (string.IsNullOrEmpty(course.Id)) return course.Price;
            try
            {
                var hexChar = course.Id.Substring(0, 1);
                var value = int.Parse(hexChar, NumberStyles.HexNumber);
                return (value % 2 != 0) ? (course.Price * 0.5m) : course.Price;
            }
            catch
            {
                return course.Price;
            }
        }

        public void Dispose()
        {
            _searcherManager?.Dispose();
            _writer?.Dispose();
            _analyzer?.Dispose();
            _directory?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}