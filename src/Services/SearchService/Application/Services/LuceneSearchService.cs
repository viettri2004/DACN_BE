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
using ContentService.Application.Interfaces;
using ContentService.Domain.Entities;
using ContentService.Application.DTOs;
using ContentService.Domain.Enums;
using SearchService.Application.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
using ContentService.Domain.Enums;
using Data.Context;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Search.Spell;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Shared.Domain.Entities;
using src.Shared.Domain.Entities;
using src.Shared.Resources;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;

namespace SearchService.Application.Services
{
    // Custom analyzer to preserve special characters (Whitespace) but convert to lowercase
    public class CaseInsensitiveAnalyzer : Analyzer
    {
        private readonly LuceneVersion _version;
        public CaseInsensitiveAnalyzer(LuceneVersion version)
        {
            _version = version;
        }
        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer tokenizer = new WhitespaceTokenizer(_version, reader);
            TokenStream filter = new LowerCaseFilter(_version, tokenizer);
            return new TokenStreamComponents(tokenizer, filter);
        }
    }

    public class LuceneSearchService : ILuceneSearchService
    {
        private const LuceneVersion LUCENE_VERSION = LuceneVersion.LUCENE_48;
        private FSDirectory _directory;
        private FSDirectory _taxonomyDirectory;
        private FSDirectory _spellDirectory;
        private readonly CaseInsensitiveAnalyzer _analyzer;
        private IndexWriter _writer;
        private DirectoryTaxonomyWriter _taxonomyWriter;
        private readonly FacetsConfig _facetsConfig;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private SearcherManager _searcherManager;
        private SpellChecker _spellChecker;
        private DirectoryTaxonomyReader? _taxonomyReader;

        // Thread-safety for writers
        private static readonly System.Threading.SemaphoreSlim _writerLock = new System.Threading.SemaphoreSlim(1, 1);

        public LuceneSearchService(
            IServiceScopeFactory scopeFactory,
            IStringLocalizer<SharedResources> localizer,
            IWebHostEnvironment env)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));

            var baseDataPath = Path.Combine(env.ContentRootPath, "lucene_data");
            var indexPath = Path.Combine(baseDataPath, "main");
            var taxonomyPath = Path.Combine(baseDataPath, "taxonomy");
            var spellcheckerPath = Path.Combine(baseDataPath, "spellchecker");

            _analyzer = new CaseInsensitiveAnalyzer(LUCENE_VERSION);
            _facetsConfig = new FacetsConfig();
            _facetsConfig.SetMultiValued("tags", true);

            try
            {
                InitializeIndex(baseDataPath, indexPath, taxonomyPath, spellcheckerPath);
            }
            catch (Exception ex) when (ex is IOException || ex is FileNotFoundException)
            {
                Console.WriteLine($"Lucene index corrupted: {ex.Message}. Attempting to wipe and rebuild...");
                
                // Dispose current objects if they exist
                _searcherManager?.Dispose();
                _spellChecker?.Dispose();
                _taxonomyWriter?.Dispose();
                _writer?.Dispose();
                _directory?.Dispose();
                _taxonomyDirectory?.Dispose();
                _spellDirectory?.Dispose();

                try
                {
                    // Wipe directories
                    if (System.IO.Directory.Exists(baseDataPath))
                        System.IO.Directory.Delete(baseDataPath, true);
                }
                catch (Exception deleteEx)
                {
                    Console.WriteLine($"Failed to delete corrupted index directory: {deleteEx.Message}");
                }

                // Re-initialize
                InitializeIndex(baseDataPath, indexPath, taxonomyPath, spellcheckerPath);
            }
        }

        private void InitializeIndex(string baseDataPath, string indexPath, string taxonomyPath, string spellcheckerPath)
        {
            if (!System.IO.Directory.Exists(baseDataPath)) System.IO.Directory.CreateDirectory(baseDataPath);
            if (!System.IO.Directory.Exists(indexPath)) System.IO.Directory.CreateDirectory(indexPath);
            if (!System.IO.Directory.Exists(taxonomyPath)) System.IO.Directory.CreateDirectory(taxonomyPath);
            if (!System.IO.Directory.Exists(spellcheckerPath)) System.IO.Directory.CreateDirectory(spellcheckerPath);

            _directory = FSDirectory.Open(new DirectoryInfo(indexPath), new SimpleFSLockFactory());
            _taxonomyDirectory = FSDirectory.Open(new DirectoryInfo(taxonomyPath), new SimpleFSLockFactory());
            _spellDirectory = FSDirectory.Open(new DirectoryInfo(spellcheckerPath), new SimpleFSLockFactory());

            UnlockDirectoryIfLocked(_directory);
            UnlockDirectoryIfLocked(_taxonomyDirectory);
            UnlockDirectoryIfLocked(_spellDirectory);

            var indexConfig = new IndexWriterConfig(LUCENE_VERSION, _analyzer)
            {
                OpenMode = OpenMode.CREATE_OR_APPEND
            };

            _writer = new IndexWriter(_directory, indexConfig);
            _taxonomyWriter = new DirectoryTaxonomyWriter(_taxonomyDirectory, OpenMode.CREATE_OR_APPEND);
            _spellChecker = new SpellChecker(_spellDirectory);
            _spellChecker.Accuracy = 0.7f;

            _searcherManager = new SearcherManager(_writer, applyAllDeletes: true, null);
        }
        public async Task<ApiResponse> SearchCoursesAsync(CourseSearchDTO searchDto, string studentId)
        {
            _searcherManager.MaybeRefreshBlocking();
            var searcher = _searcherManager.Acquire();
            
            // Refresh taxonomy reader if needed
            try
            {
                if (_taxonomyReader == null)
                {
                    _taxonomyReader = new DirectoryTaxonomyReader(_taxonomyDirectory);
                }
                else
                {
                    var newReader = TaxonomyReader.OpenIfChanged(_taxonomyReader);
                    if (newReader != null)
                    {
                        _taxonomyReader.Dispose();
                        _taxonomyReader = (DirectoryTaxonomyReader)newReader;
                    }
                }
            }
            catch (Lucene.Net.Index.IndexNotFoundException)
            {
                // Taxonomy index empty or not committed yet
                _taxonomyReader = null;
            }

            try
            {
                Query baseQuery;

                if (!string.IsNullOrWhiteSpace(searchDto?.SearchTerm))
                {
                    var boolQuery = new BooleanQuery();
                    var searchTerm = searchDto.SearchTerm.ToLowerInvariant().Trim();
                    
                    // Use QueryParser for natural language search on the 'name' field
                    var parser = new QueryParser(LUCENE_VERSION, "name", _analyzer);
                    parser.DefaultOperator = Operator.AND;
                    
                    try 
                    {
                        var parsedQuery = parser.Parse(searchTerm);
                        boolQuery.Add(parsedQuery, Occur.SHOULD);
                    }
                    catch (ParseException)
                    {
                        // Fallback to wildcard if parsing fails
                        var escaped = QueryParserBase.Escape(searchTerm);
                        boolQuery.Add(new WildcardQuery(new Term("name", $"*{escaped}*")), Occur.SHOULD);
                    }

                    // Add wildcard search for partial matches on name_lower and instructor
                    var escapedTerm = QueryParserBase.Escape(searchTerm);
                    boolQuery.Add(new WildcardQuery(new Term("name_lower", $"*{escapedTerm}*")), Occur.SHOULD);
                    boolQuery.Add(new WildcardQuery(new Term("instructorName", $"*{escapedTerm}*")), Occur.SHOULD);

                    baseQuery = boolQuery;
                }
                else
                {
                    baseQuery = new MatchAllDocsQuery();
                }

                // Filter by Public status
                var finalBoolQuery = new BooleanQuery();
                finalBoolQuery.Add(baseQuery, Occur.MUST);
                finalBoolQuery.Add(new TermQuery(new Term("status", CourseStatus.Public.ToString().ToLowerInvariant())), Occur.MUST);

                // Filter by Price in Lucene
                if (searchDto.MinPrice.HasValue || searchDto.MaxPrice.HasValue)
                {
                    double min = (double)(searchDto.MinPrice ?? 0);
                    double max = (double)(searchDto.MaxPrice ?? decimal.MaxValue);
                    finalBoolQuery.Add(NumericRangeQuery.NewDoubleRange("calculatedPrice", min, max, true, true), Occur.MUST);
                }

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Filter out enrolled courses
                if (!string.IsNullOrEmpty(studentId))
                {
                    var enrolledCourseIds = await context.Enrollments
                        .Where(e => e.StudentId == studentId && e.Status == true)
                        .Select(e => e.CourseId)
                        .ToListAsync();

                    foreach (var enrolledId in enrolledCourseIds)
                    {
                        finalBoolQuery.Add(new TermQuery(new Term("id", enrolledId)), Occur.MUST_NOT);
                    }
                }

                if (searchDto.SelectedTags != null && searchDto.SelectedTags.Any())
                {
                    foreach (var tagId in searchDto.SelectedTags)
                    {
                        finalBoolQuery.Add(new TermQuery(new Term("tag_id", tagId.ToLowerInvariant())), Occur.MUST);
                    }
                }

                var drillDownQuery = new DrillDownQuery(_facetsConfig, finalBoolQuery);

                var page = Math.Max(1, searchDto?.Page ?? 1);
                var pageSize = Math.Max(1, searchDto?.PageSize ?? 10);
                int numHits = 1000; // Search up to 1000 items to get accurate total count

                var facetsCollector = new FacetsCollector();
                TopDocs topDocs = FacetsCollector.Search(searcher, drillDownQuery, numHits, facetsCollector);
                
                var totalHits = topDocs.TotalHits;

                // Extract Facets
                FacetResult? tagResults = null;
                if (_taxonomyReader != null)
                {
                    var facets = new FastTaxonomyFacetCounts(_taxonomyReader, _facetsConfig, facetsCollector);
                    tagResults = facets.GetTopChildren(100, "tags");
                }
                
                var allTagsFromDb = await context.Tags.AsNoTracking().ToListAsync();

                var availableTags = allTagsFromDb.Select(t => new TagFacetDTO
                {
                    TagId = t.Id,
                    TagName = t.Name,
                    Count = 0
                }).ToList();

                if (tagResults != null)
                {
                    foreach (var labelValue in tagResults.LabelValues)
                    {
                        var parts = labelValue.Label.Split(':');
                        if (parts.Length >= 2)
                        {
                            var tagId = parts[0];
                            var existingTag = availableTags.FirstOrDefault(at => at.TagId == tagId);
                            if (existingTag != null)
                            {
                                existingTag.Count = (int)labelValue.Value;
                            }
                        }
                    }
                }

                // Sort by count descending, then name
                availableTags = availableTags.OrderByDescending(t => t.Count).ThenBy(t => t.TagName).ToList();

                var pagedScoreDocs = topDocs.ScoreDocs.ToList();

                var courseIds = new List<string>();
                foreach (var scoreDoc in pagedScoreDocs)
                {
                    var doc = searcher.Doc(scoreDoc.Doc);
                    var id = doc.Get("id");
                    if (!string.IsNullOrEmpty(id)) courseIds.Add(id);
                }

                if (courseIds.Count == 0)
                {
                    string? suggestion = null;
                    if (!string.IsNullOrWhiteSpace(searchDto?.SearchTerm))
                    {
                        var words = searchDto.SearchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var suggestedWords = new List<string>();

                        foreach (var word in words)
                        {
                            var suggestions = _spellChecker.SuggestSimilar(word, 1);
                            suggestedWords.Add(suggestions.Length > 0 ? suggestions[0] : word);
                        }

                        var suggestedPhrase = string.Join(" ", suggestedWords);
                        if (suggestedPhrase != searchDto.SearchTerm.ToLowerInvariant())
                        {
                            // Validate that the suggestion actually returns results
                            var suggestionQuery = new BooleanQuery();
                            var suggestionTerms = suggestedPhrase.Split(' ');
                            foreach (var sTerm in suggestionTerms)
                            {
                                var sTermBoolQuery = new BooleanQuery();
                                sTermBoolQuery.Add(new WildcardQuery(new Term("name", $"*{sTerm}*")), Occur.SHOULD);
                                sTermBoolQuery.Add(new WildcardQuery(new Term("instructorName", $"*{sTerm}*")), Occur.SHOULD);
                                suggestionQuery.Add(sTermBoolQuery, Occur.MUST);
                            }
                            
                            var validatedTopDocs = searcher.Search(suggestionQuery, 1);
                            if (validatedTopDocs.TotalHits > 0)
                            {
                                suggestion = suggestedPhrase;
                            }
                        }
                    }

                    var emptyResult = new PagedResult<CourseCardDTO>
                    {
                        Items = new List<CourseCardDTO>(),
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalHits
                    };
                    var responseData = new CourseSearchResponseDTO
                    {
                        Courses = emptyResult,
                        AvailableTags = availableTags,
                        DidYouMean = suggestion
                    };
                    return new ApiResponse("Success", _localizer["Success"].Value, responseData, true);
                }

                var coursesQuery = context.Courses
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(c => courseIds.Contains(c.Id) && c.Status != CourseStatus.Private)
                    .Include(c => c.Instructor)
                    .Include(c => c.Enrollments)
                        .ThenInclude(e => e.Comments)
                    .Include(c => c.Lectures)
                        .ThenInclude(l => l.LectureVideos);

                var coursesFromDb = await coursesQuery.ToListAsync();

                if (!string.IsNullOrEmpty(studentId))
                {
                    var enrolledCourseIds = await context.Enrollments
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
                        coursesWithPrice = coursesWithPrice.OrderByDescending(x => x.Course.Enrollments.SelectMany(e => e.Comments).Any(cm => cm.Type == CommentType.Review)
                                                        ? x.Course.Enrollments.SelectMany(e => e.Comments).Where(cm => cm.Type == CommentType.Review).Average(cm => cm.Rate)
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
                        coursesWithPrice = coursesWithPrice.OrderByDescending(x => x.Course.Enrollments.Count(e => e.Status));
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
                    var reviewComments = (course.Enrollments ?? new List<Enrollment>())
                        .SelectMany(e => e.Comments ?? new List<Comment>())
                        .Where(cm => cm.Type == CommentType.Review)
                        .ToList();
                    var avgRating = reviewComments.Any() ? reviewComments.Average(cm => cm.Rate) : 0;

                    var dto = new CourseCardDTO
                    {
                        Id = course.Id,
                        Name = course.Name,
                        Description = course.Description,
                        ImageUrl = course.ImageUrl,
                        InstructorName = course.Instructor?.FullName ?? string.Empty,
                        AverageRating = Math.Round(avgRating, 1),
                        TotalReviews = reviewComments.Count,
                        TotalStudents = course.Enrollments?.Count ?? 0,
                        OriginalPrice = course.Price,
                        Price = calculatedPrice,
                        IsBestseller = (course.Enrollments?.Count ?? 0) > 5,
                        TotalHours = course.Lectures.SelectMany(l => l.LectureVideos).Any()
                                     ? (int)Math.Max(1, Math.Round(course.Lectures.SelectMany(l => l.LectureVideos).Sum(v => v.Duration) / 3600.0))
                                     : 0,
                        LastUpdate = course.UpdatedAt == default ? course.CreateTime : course.UpdatedAt
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
                    TotalCount = totalHits
                };

                var finalResponse = new CourseSearchResponseDTO
                {
                    Courses = pagedResult,
                    AvailableTags = availableTags
                };

                return new ApiResponse("Success", _localizer["Success"].Value, finalResponse, true);
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

        public async Task<ApiResponse> SearchCoursesPreviewAsync(string searchTerm, string studentId)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return new ApiResponse("Success", _localizer["Success"].Value, new List<CoursePreviewDTO>(), true);
            }

            _searcherManager.MaybeRefreshBlocking();
            var searcher = _searcherManager.Acquire();

            try
            {
                var term = searchTerm.ToLowerInvariant().Trim();
                
                var boolQuery = new BooleanQuery();
                
                // Use QueryParser for name field to support finding terms anywhere in the string
                var nameParser = new QueryParser(LUCENE_VERSION, "name", _analyzer);
                nameParser.DefaultOperator = Operator.AND;
                
                try 
                {
                    var parsedNameQuery = nameParser.Parse(term);
                    boolQuery.Add(parsedNameQuery, Occur.SHOULD);
                }
                catch
                {
                    boolQuery.Add(new PrefixQuery(new Term("name_lower", term)), Occur.SHOULD);
                }

                // Support partial word matching (e.g., 'lea' finds 'learning')
                var escaped = QueryParserBase.Escape(term);
                boolQuery.Add(new WildcardQuery(new Term("name_lower", $"*{escaped}*")), Occur.SHOULD);
                boolQuery.Add(new WildcardQuery(new Term("instructorName_lower", $"*{escaped}*")), Occur.SHOULD);

                // Add prefix match as an additional signal for better relevance
                boolQuery.Add(new PrefixQuery(new Term("name_lower", term)), Occur.SHOULD);
                
                boolQuery.MinimumNumberShouldMatch = 1;
 
                // Filter by Public status
                var finalQuery = new BooleanQuery();
                finalQuery.Add(boolQuery, Occur.MUST);
                finalQuery.Add(new TermQuery(new Term("status", CourseStatus.Public.ToString().ToLowerInvariant())), Occur.MUST);

                // Filter out enrolled courses
                if (!string.IsNullOrEmpty(studentId))
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var enrolledCourseIds = await context.Enrollments
                        .Where(e => e.StudentId == studentId && e.Status == true)
                        .Select(e => e.CourseId)
                        .ToListAsync();

                    foreach (var enrolledId in enrolledCourseIds)
                    {
                        finalQuery.Add(new TermQuery(new Term("id", enrolledId)), Occur.MUST_NOT);
                    }
                }

                // Limit to 5-10 results for preview
                TopDocs topDocs = searcher.Search(finalQuery, 10);
                
                var results = new List<CoursePreviewDTO>();
                foreach (var scoreDoc in topDocs.ScoreDocs)
                {
                    var doc = searcher.Doc(scoreDoc.Doc);
                    results.Add(new CoursePreviewDTO
                    {
                        Id = doc.Get("id"),
                        Name = doc.Get("name_stored"), // We'll add this stored field
                        ImageUrl = doc.Get("imageUrl"),
                        InstructorName = doc.Get("instructorName_stored")
                    });
                }

                return new ApiResponse("Success", _localizer["Success"].Value, results, true);
            }
            finally
            {
                _searcherManager.Release(searcher);
            }
        }

        public Task IndexCourseAsync(Course course)
        {
            if (course == null) return Task.CompletedTask;
            return IndexCourseAsync(course.Id);
        }

        public async Task IndexCourseAsync(string courseId)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var fullCourse = await context.Courses
                .Include(c => c.Instructor)
                .Include(c => c.CourseTags)
                    .ThenInclude(ct => ct.Tag)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Comments)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (fullCourse == null) return;

            var calculatedPrice = CalculatePrice(fullCourse);
            var comments = (fullCourse.Enrollments ?? new List<Enrollment>()).SelectMany(e => e.Comments ?? new List<Comment>()).ToList();
            var avgRating = comments.Any() ? comments.Average(c => c.Rate) : 0;
            var enrollmentCount = fullCourse.Enrollments?.Count ?? 0;

            var doc = new Lucene.Net.Documents.Document
            {
                new StringField("id", fullCourse.Id ?? string.Empty, Field.Store.YES),
                new TextField("name", fullCourse.Name ?? string.Empty, Field.Store.NO),
                new StringField("name_stored", fullCourse.Name ?? string.Empty, Field.Store.YES), // For preview
                new StringField("name_lower", (fullCourse.Name ?? string.Empty).ToLowerInvariant(), Field.Store.NO), // For better searching
                new TextField("description", fullCourse.Description ?? string.Empty, Field.Store.NO),
                new TextField("instructorName", fullCourse.Instructor?.FullName ?? string.Empty, Field.Store.NO),
                new StringField("instructorName_lower", (fullCourse.Instructor?.FullName ?? string.Empty).ToLowerInvariant(), Field.Store.NO),
                new StringField("instructorName_stored", fullCourse.Instructor?.FullName ?? string.Empty, Field.Store.YES), // For preview
                new StringField("imageUrl", fullCourse.ImageUrl ?? string.Empty, Field.Store.YES), // For preview
                new DoubleField("price", (double)fullCourse.Price, Field.Store.NO),
                new DoubleField("calculatedPrice", (double)calculatedPrice, Field.Store.NO),
                new DoubleField("averageRating", avgRating, Field.Store.NO),
                new Int32Field("totalStudents", enrollmentCount, Field.Store.NO),
                new Int32Field("totalReviews", comments.Count, Field.Store.NO),
                new Int64Field("createTime", fullCourse.CreateTime.Ticks, Field.Store.NO),
                new StringField("status", fullCourse.Status.ToString().ToLowerInvariant(), Field.Store.NO)
            };

            if (fullCourse.CourseTags != null)
            {
                foreach (var ct in fullCourse.CourseTags)
                {
                    if (ct.Tag != null)
                    {
                        var tagId = ct.TagId.ToLowerInvariant();
                        var tagName = ct.Tag.Name;
                        doc.Add(new FacetField("tags", $"{tagId}:{tagName}"));
                        doc.Add(new StringField("tag_id", tagId, Field.Store.NO));
                    }
                }
            }

            await _writerLock.WaitAsync();
            try
            {
                var facetedDoc = _facetsConfig.Build(_taxonomyWriter, doc);
                _writer.UpdateDocument(new Term("id", fullCourse.Id ?? string.Empty), facetedDoc);
                _writer.Commit();
                _taxonomyWriter.Commit();
                _searcherManager.MaybeRefresh();
            }
            finally
            {
                _writerLock.Release();
            }
        }

        public async Task IndexAllCoursesAsync()
        {
            await _writerLock.WaitAsync();
            try
            {
                // Clear the main index
                _writer.DeleteAll();
                _writer.Commit();
                
                // Note: Taxonomy index is additive. We don't easily "DeleteAll" it while open.
                // In most cases, it's fine to let it grow as categories are usually stable.
                // If you need a total reset, the app should be restarted after deleting the taxonomy folder.

                const int batchSize = 1000;
                int page = 0;
                int totalIndexed = 0;
                List<Course> courses;

                do
                {
                    using (var batchScope = _scopeFactory.CreateScope())
                    {
                        var batchContext = batchScope.ServiceProvider.GetRequiredService<AppDbContext>();
                        courses = await batchContext.Courses
                            .Include(c => c.Instructor)
                            .Include(c => c.CourseTags)
                                .ThenInclude(ct => ct.Tag)
                            .Include(c => c.Enrollments)
                                .ThenInclude(e => e.Comments)
                            .AsNoTracking()
                            .AsSplitQuery()
                            .OrderBy(c => c.Id)
                            .Skip(page * batchSize)
                            .Take(batchSize)
                            .ToListAsync();
                    }

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
                    new StringField("name_stored", course.Name ?? string.Empty, Field.Store.YES), // For preview
                    new StringField("name_lower", (course.Name ?? string.Empty).ToLowerInvariant(), Field.Store.NO), // For better searching
                    new TextField("description", course.Description ?? string.Empty, Field.Store.NO),
                    new TextField("instructorName", course.Instructor?.FullName ?? string.Empty, Field.Store.NO),
                    new StringField("instructorName_lower", (course.Instructor?.FullName ?? string.Empty).ToLowerInvariant(), Field.Store.NO),
                    new StringField("instructorName_stored", course.Instructor?.FullName ?? string.Empty, Field.Store.YES), // For preview
                    new StringField("imageUrl", course.ImageUrl ?? string.Empty, Field.Store.YES), // For preview
                    new DoubleField("price", (double)course.Price, Field.Store.NO),
                    new DoubleField("calculatedPrice", (double)calculatedPrice, Field.Store.NO),
                    new DoubleField("averageRating", avgRating, Field.Store.NO),
                    new Int32Field("totalStudents", course.Enrollments?.Count ?? 0, Field.Store.NO),
                    new Int32Field("totalReviews", comments.Count, Field.Store.NO),
                    new Int64Field("createTime", course.CreateTime.Ticks, Field.Store.NO),
                    new StringField("status", course.Status.ToString().ToLowerInvariant(), Field.Store.NO)
                };

                        if (course.CourseTags != null)
                        {
                            foreach (var ct in course.CourseTags)
                            {
                                if (ct.Tag != null)
                                {
                                    var tagId = ct.TagId.ToLowerInvariant();
                                    var tagName = ct.Tag.Name;
                                    doc.Add(new FacetField("tags", $"{tagId}:{tagName}"));
                                    doc.Add(new StringField("tag_id", tagId, Field.Store.NO));
                                }
                            }
                        }

                        var facetedDoc = _facetsConfig.Build(_taxonomyWriter, doc);
                        _writer.AddDocument(facetedDoc);
                    }

                    _writer.Commit();
                    _taxonomyWriter.Commit();
                    totalIndexed += courses.Count;

                    page++;

                } while (courses.Count == batchSize);

                _searcherManager.MaybeRefreshBlocking();
                
                // Build SpellChecker dictionary from the 'name' field
                // Use the writer to open the reader to avoid FileNotFoundException on hdd
                using (var reader = DirectoryReader.Open(_writer, applyAllDeletes: true))
                {
                    _spellChecker.IndexDictionary(new LuceneDictionary(reader, "name"), new IndexWriterConfig(LUCENE_VERSION, _analyzer), true);
                }

                Console.WriteLine($"Successfully indexed {totalIndexed} courses");
            }
            catch (Exception ex)
            {
                _writer.Rollback();
                _taxonomyWriter.Rollback();
                Console.WriteLine($"Error indexing courses: {ex.Message}");
                throw;
            }
            finally
            {
                _writerLock.Release();
            }
        }

        public async Task DeleteCourseFromIndexAsync(string courseId)
        {
            await _writerLock.WaitAsync();
            try
            {
                _writer.DeleteDocuments(new Term("id", courseId ?? string.Empty));
                _writer.Commit();
                _searcherManager.MaybeRefresh();
            }
            finally
            {
                _writerLock.Release();
            }
        }

        private void UnlockDirectoryIfLocked(Lucene.Net.Store.Directory directory)
        {
            try
            {
                if (IndexWriter.IsLocked(directory))
                {
                    IndexWriter.Unlock(directory);
                }
            }
            catch (Exception ex)
            {
                // Log and continue, as a lock might be cleared by another process or be transient
                Console.WriteLine($"Warning: Could not check/unlock directory: {ex.Message}");
            }
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
            _taxonomyReader?.Dispose();
            _taxonomyWriter?.Dispose();
            _writer?.Dispose();
            _analyzer?.Dispose();
            _directory?.Dispose();
            _taxonomyDirectory?.Dispose();
            _spellDirectory?.Dispose();
            _spellChecker?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}




