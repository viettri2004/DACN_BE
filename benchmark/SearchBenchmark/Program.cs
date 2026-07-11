using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleTables;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Microsoft.EntityFrameworkCore;

namespace SearchBenchmark;

// ============================================================
// EF Core Entities (schema "benchmark")
// ============================================================
[Table("courses", Schema = "benchmark")]
public class BenchmarkCourse
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = null!;

    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string Description { get; set; } = null!;

    [Column("instructor_name")]
    public string InstructorName { get; set; } = null!;

    [Column("price")]
    public decimal Price { get; set; }

    [Column("status")]
    public string Status { get; set; } = "public";

    [Column("create_time")]
    public DateTime CreateTime { get; set; }

    [Column("average_rating")]
    public double AverageRating { get; set; }

    [Column("total_students")]
    public int TotalStudents { get; set; }

    [Column("tags")]
    public string Tags { get; set; } = "";
}

// ============================================================
// DbContext for benchmark schema
// ============================================================
public class BenchmarkDbContext : DbContext
{
    private readonly string _connectionString;

    public BenchmarkDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbSet<BenchmarkCourse> Courses { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(_connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("benchmark");

        modelBuilder.Entity<BenchmarkCourse>(entity =>
        {
            entity.ToTable("courses", "benchmark");
            entity.HasKey(e => e.Id);
        });
    }
}

// ============================================================
// Vietnamese Mock Data Generator
// ============================================================
public static class MockDataGenerator
{
    private static readonly string[] Prefixes = {
        "Lập trình", "Thiết kế", "Phát triển", "Xây dựng", "Tối ưu",
        "Nâng cao", "Cơ bản", "Chuyên sâu", "Thực hành", "Hướng dẫn",
        "Khám phá", "Tìm hiểu", "Nhập môn", "Master", "Pro",
        "Bootcamp", "Workshop", "Intensive", "Crash Course", "Deep Dive"
    };

    private static readonly string[] Topics = {
        "C#", "Java", "Python", "JavaScript", "TypeScript",
        "React", "Angular", "Vue.js", "Node.js", "ASP.NET Core",
        "Entity Framework", "Docker", "Kubernetes", "AWS", "Azure",
        "Machine Learning", "Deep Learning", "AI", "Data Science", "Big Data",
        "Flutter", "React Native", "Swift", "Kotlin", "Go",
        "Rust", "PHP", "Laravel", "Django", "Spring Boot",
        "SQL Server", "PostgreSQL", "MongoDB", "Redis", "Elasticsearch",
        "HTML CSS", "Tailwind CSS", "Bootstrap", "Figma", "UI/UX",
        "DevOps", "CI/CD", "Git", "Linux", "Networking",
        "Blockchain", "Web3", "Smart Contract", "Solidity", "NFT",
        "Cybersecurity", "Ethical Hacking", "Penetration Testing", "Cloud Computing", "Microservices",
        "GraphQL", "REST API", "gRPC", "WebSocket", "SignalR",
        "Unity", "Unreal Engine", "Game Development", "3D Modeling", "Blender"
    };

    private static readonly string[] Suffixes = {
        "từ Zero đến Hero", "cho người mới bắt đầu", "nâng cao",
        "trong 30 ngày", "thực chiến", "qua dự án thực tế",
        "dành cho Developer", "full stack", "từ A đến Z",
        "2024", "2025", "phiên bản mới nhất",
        "với ví dụ thực tế", "cho sinh viên", "cho doanh nghiệp",
        "hoàn chỉnh", "tối ưu hiệu suất", "best practices",
        "design patterns", "clean architecture"
    };

    private static readonly string[] DescriptionTemplates = {
        "Khóa học {0} giúp bạn nắm vững kiến thức {1} từ cơ bản đến nâng cao. Bạn sẽ được thực hành qua nhiều bài tập và dự án thực tế. Khóa học được thiết kế dành cho những ai muốn phát triển sự nghiệp trong lĩnh vực công nghệ thông tin.",
        "Tham gia khóa học {0} để trở thành chuyên gia trong lĩnh vực {1}. Được thiết kế bởi các chuyên gia hàng đầu trong ngành với hơn 10 năm kinh nghiệm thực tế.",
        "Học {0} một cách hiệu quả với phương pháp giảng dạy hiện đại. Khóa học bao gồm {1} và nhiều chủ đề liên quan. Bạn sẽ được hướng dẫn từng bước từ cơ bản đến nâng cao.",
        "Khóa học {0} cung cấp kiến thức toàn diện về {1}, bao gồm lý thuyết và thực hành dự án. Sau khi hoàn thành, bạn sẽ có đủ kỹ năng để làm việc trong môi trường chuyên nghiệp.",
        "Đào tạo {0} chuyên sâu với trọng tâm vào {1}. Phù hợp cho mọi trình độ từ beginner đến advanced. Bao gồm hơn 50 bài thực hành và 5 dự án thực tế."
    };

    private static readonly string[] InstructorFirstNames = {
        "Nguyễn Văn", "Trần Thị", "Lê Hoàng", "Phạm Minh", "Hoàng Đức",
        "Võ Thanh", "Đặng Kim", "Bùi Quốc", "Đỗ Xuân", "Hồ Ngọc",
        "Ngô Hữu", "Dương Thành", "Lý Hải", "Trương Công", "Đinh Bảo",
        "Phan Đình", "Vũ Minh", "Mai Anh", "Tạ Quang", "Chu Văn"
    };

    private static readonly string[] InstructorLastNames = {
        "An", "Bình", "Cường", "Dũng", "Em",
        "Hải", "Hùng", "Khoa", "Long", "Minh",
        "Nam", "Phong", "Quân", "Sơn", "Thắng",
        "Trung", "Tuấn", "Vinh", "Duy", "Tâm"
    };

    private static readonly string[] TagNames = {
        "web-development", "mobile-development", "data-science", "machine-learning",
        "devops", "cloud-computing", "cybersecurity", "game-development",
        "database", "frontend", "backend", "fullstack",
        "beginner", "intermediate", "advanced", "project-based"
    };

    public static List<BenchmarkCourse> Generate(int count, int seed = 42)
    {
        var rng = new Random(seed);
        var courses = new List<BenchmarkCourse>(count);

        for (int i = 0; i < count; i++)
        {
            var prefix = Prefixes[rng.Next(Prefixes.Length)];
            var topic = Topics[rng.Next(Topics.Length)];
            var suffix = Suffixes[rng.Next(Suffixes.Length)];
            var name = $"{prefix} {topic} {suffix}";

            var descTemplate = DescriptionTemplates[rng.Next(DescriptionTemplates.Length)];
            var description = string.Format(descTemplate, topic, topic);

            var instructor = $"{InstructorFirstNames[rng.Next(InstructorFirstNames.Length)]} {InstructorLastNames[rng.Next(InstructorLastNames.Length)]}";

            var tagCount = rng.Next(1, 5);
            var tags = new List<string>();
            for (int t = 0; t < tagCount; t++)
            {
                var tag = TagNames[rng.Next(TagNames.Length)];
                if (!tags.Contains(tag)) tags.Add(tag);
            }

            courses.Add(new BenchmarkCourse
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                Description = description,
                InstructorName = instructor,
                Price = (decimal)(rng.Next(0, 2000000) / 1000) * 1000,
                Status = "public",
                CreateTime = DateTime.UtcNow.AddDays(-rng.Next(1, 730)),
                AverageRating = Math.Round(rng.NextDouble() * 3 + 2, 1),
                TotalStudents = rng.Next(0, 50000),
                Tags = string.Join(",", tags)
            });
        }

        return courses;
    }
}

// ============================================================
// Custom Analyzer (matches project's CaseInsensitiveAnalyzer)
// ============================================================
public class CaseInsensitiveAnalyzer : Analyzer
{
    private readonly LuceneVersion _version;
    public CaseInsensitiveAnalyzer(LuceneVersion version) => _version = version;

    protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
    {
        var tokenizer = new WhitespaceTokenizer(_version, reader);
        TokenStream filter = new LowerCaseFilter(_version, tokenizer);
        return new TokenStreamComponents(tokenizer, filter);
    }
}

// ============================================================
// Lucene Search Engine (RAM directory, mirrors real service)
// ============================================================
public class LuceneSearchEngine : IDisposable
{
    private const LuceneVersion VERSION = LuceneVersion.LUCENE_48;
    private readonly RAMDirectory _directory;
    private readonly CaseInsensitiveAnalyzer _analyzer;
    private readonly IndexWriter _writer;
    private SearcherManager _searcherManager;

    public LuceneSearchEngine()
    {
        _directory = new RAMDirectory();
        _analyzer = new CaseInsensitiveAnalyzer(VERSION);
        var config = new IndexWriterConfig(VERSION, _analyzer) { OpenMode = OpenMode.CREATE_OR_APPEND };
        _writer = new IndexWriter(_directory, config);
        _searcherManager = new SearcherManager(_writer, applyAllDeletes: true, null);
    }

    public void IndexAll(List<BenchmarkCourse> courses)
    {
        foreach (var course in courses)
        {
            var doc = new Document
            {
                new StringField("id", course.Id, Field.Store.YES),
                new TextField("name", course.Name, Field.Store.NO),
                new StringField("name_stored", course.Name, Field.Store.YES),
                new StringField("name_lower", course.Name.ToLowerInvariant(), Field.Store.NO),
                new TextField("description", course.Description, Field.Store.NO),
                new TextField("instructorName", course.InstructorName, Field.Store.NO),
                new StringField("instructorName_lower", course.InstructorName.ToLowerInvariant(), Field.Store.NO),
                new StringField("instructorName_stored", course.InstructorName, Field.Store.YES),
                new DoubleField("price", (double)course.Price, Field.Store.NO),
                new DoubleField("averageRating", course.AverageRating, Field.Store.NO),
                new Int32Field("totalStudents", course.TotalStudents, Field.Store.NO),
                new Int64Field("createTime", course.CreateTime.Ticks, Field.Store.NO),
                new StringField("status", course.Status, Field.Store.NO)
            };

            foreach (var tag in course.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                doc.Add(new StringField("tag_id", tag.Trim(), Field.Store.NO));
            }

            _writer.AddDocument(doc);
        }

        _writer.Commit();
        _searcherManager.MaybeRefreshBlocking();
    }

    public List<string> Search(string searchTerm, int maxResults = 100)
    {
        _searcherManager.MaybeRefreshBlocking();
        var searcher = _searcherManager.Acquire();

        try
        {
            var boolQuery = new BooleanQuery();
            var term = searchTerm.ToLowerInvariant().Trim();

            var parser = new QueryParser(VERSION, "name", _analyzer);
            parser.DefaultOperator = Operator.AND;

            try
            {
                var parsedQuery = parser.Parse(term);
                boolQuery.Add(parsedQuery, Occur.SHOULD);
            }
            catch (ParseException)
            {
                var escaped = QueryParserBase.Escape(term);
                boolQuery.Add(new WildcardQuery(new Term("name", $"*{escaped}*")), Occur.SHOULD);
            }

            var escapedTerm = QueryParserBase.Escape(term);
            boolQuery.Add(new WildcardQuery(new Term("name_lower", $"*{escapedTerm}*")), Occur.SHOULD);
            boolQuery.Add(new WildcardQuery(new Term("instructorName", $"*{escapedTerm}*")), Occur.SHOULD);

            var finalQuery = new BooleanQuery();
            finalQuery.Add(boolQuery, Occur.MUST);
            finalQuery.Add(new TermQuery(new Term("status", "public")), Occur.MUST);

            var topDocs = searcher.Search(finalQuery, maxResults);

            var results = new List<string>();
            foreach (var scoreDoc in topDocs.ScoreDocs)
            {
                var doc = searcher.Doc(scoreDoc.Doc);
                results.Add(doc.Get("id"));
            }
            return results;
        }
        finally
        {
            _searcherManager.Release(searcher);
        }
    }

    public void Dispose()
    {
        _searcherManager?.Dispose();
        _writer?.Dispose();
        _analyzer?.Dispose();
        _directory?.Dispose();
    }
}

// ============================================================
// PostgreSQL Search Helper
// ============================================================
public static class PostgresSearch
{
    /// <summary>
    /// WHERE status='public' AND (name ILIKE '%term%' OR description ILIKE '%term%' OR instructor_name ILIKE '%term%')
    /// LIMIT maxResults
    /// </summary>
    public static async Task<List<string>> SearchAsync(BenchmarkDbContext db, string searchTerm, int maxResults = 100)
    {
        var term = $"%{searchTerm}%";

        return await db.Courses
            .Where(c => c.Status == "public" &&
                (EF.Functions.ILike(c.Name, term) ||
                 EF.Functions.ILike(c.Description, term) ||
                 EF.Functions.ILike(c.InstructorName, term)))
            .Select(c => c.Id)
            .Take(maxResults)
            .ToListAsync();
    }

    /// <summary>
    /// Full search with sorting and paging (like real app)
    /// </summary>
    public static async Task<List<string>> SearchWithPagingAsync(BenchmarkDbContext db, string searchTerm, int page = 1, int pageSize = 10)
    {
        var term = $"%{searchTerm}%";

        return await db.Courses
            .Where(c => c.Status == "public" &&
                (EF.Functions.ILike(c.Name, term) ||
                 EF.Functions.ILike(c.Description, term) ||
                 EF.Functions.ILike(c.InstructorName, term)))
            .OrderByDescending(c => c.TotalStudents)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => c.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Search only on name column (simpler query)
    /// </summary>
    public static async Task<List<string>> SearchNameOnlyAsync(BenchmarkDbContext db, string searchTerm, int maxResults = 100)
    {
        var term = $"%{searchTerm}%";

        return await db.Courses
            .Where(c => c.Status == "public" && EF.Functions.ILike(c.Name, term))
            .Select(c => c.Id)
            .Take(maxResults)
            .ToListAsync();
    }
}

// ============================================================
// Main Program
// ============================================================
public class Program
{
    const string CONNECTION_STRING = "Host=dacn-3t-tonviettri2004-8168.l.aivencloud.com;Port=26599;Database=defaultdb;Username=avnadmin;Password=AVNS_TqbXUwvX9WgXhT-Pbd5;SSL Mode=Require;";

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  🔍 BENCHMARK: Lucene.NET vs PostgreSQL (Real Database)     ║");
        Console.WriteLine("║  📊 10,000 Mock Courses · Vietnamese Data · Aiven Cloud     ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // ── Step 1: Setup database ──
        Console.Write("⏳ Connecting to PostgreSQL and creating benchmark schema... ");
        var sw = Stopwatch.StartNew();

        await using var setupDb = new BenchmarkDbContext(CONNECTION_STRING);
        
        // Create schema + table
        await setupDb.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS benchmark");
        await setupDb.Database.ExecuteSqlRawAsync(@"
            DROP TABLE IF EXISTS benchmark.courses;
            CREATE TABLE benchmark.courses (
                id VARCHAR(32) PRIMARY KEY,
                name TEXT NOT NULL,
                description TEXT NOT NULL,
                instructor_name TEXT NOT NULL,
                price DECIMAL NOT NULL,
                status VARCHAR(20) NOT NULL DEFAULT 'public',
                create_time TIMESTAMP NOT NULL,
                average_rating DOUBLE PRECISION NOT NULL,
                total_students INTEGER NOT NULL,
                tags TEXT NOT NULL DEFAULT ''
            )");
        sw.Stop();
        Console.WriteLine($"✅ Done in {sw.ElapsedMilliseconds}ms");

        // ── Step 2: Generate and seed data ──
        Console.Write("⏳ Generating 10,000 mock courses... ");
        sw.Restart();
        var courses = MockDataGenerator.Generate(10_000);
        sw.Stop();
        Console.WriteLine($"✅ Done in {sw.ElapsedMilliseconds}ms");

        Console.WriteLine("\n📋 Sample courses:");
        for (int i = 0; i < 5; i++)
        {
            Console.WriteLine($"   [{i + 1}] {courses[i].Name}");
            Console.WriteLine($"       👨‍🏫 {courses[i].InstructorName} | 💰 {courses[i].Price:N0} VND | ⭐ {courses[i].AverageRating}");
        }

        Console.Write($"\n⏳ Inserting 10,000 courses into PostgreSQL (schema: benchmark)... ");
        sw.Restart();

        // Bulk insert in batches
        const int batchSize = 500;
        for (int i = 0; i < courses.Count; i += batchSize)
        {
            await using var batchDb = new BenchmarkDbContext(CONNECTION_STRING);
            var batch = courses.Skip(i).Take(batchSize);
            batchDb.Courses.AddRange(batch);
            await batchDb.SaveChangesAsync();
        }

        sw.Stop();
        var seedTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"✅ Done in {seedTime}ms");

        // Verify
        await using var verifyDb = new BenchmarkDbContext(CONNECTION_STRING);
        var totalInDb = await verifyDb.Courses.CountAsync();
        Console.WriteLine($"   📊 Verified: {totalInDb} courses in database");

        // ── Step 3: Build Lucene index ──
        Console.Write("\n⏳ Building Lucene index (RAM) for 10,000 courses... ");
        sw.Restart();
        var lucene = new LuceneSearchEngine();
        lucene.IndexAll(courses);
        sw.Stop();
        var indexTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"✅ Done in {indexTime}ms");

        // ── Step 4: Define search queries ──
        var searchQueries = new[]
        {
            "Python",
            "React",
            "C#",
            "Docker",
            "Machine Learning",
            "Lập trình",
            "nâng cao",
            "thực chiến",
            "cơ bản",
            "dự án thực tế",
            "Java",
            "SQL",
            "Web",
            "AI",
            "Nguyễn",
            "Trần",
            "Blockchain",
            "Rust",
            "gRPC",
            "NFT",
        };

        // ── Step 5: Warmup ──
        Console.WriteLine("\n🔥 Warming up (2 iterations)...");
        for (int w = 0; w < 2; w++)
        {
            foreach (var q in searchQueries)
            {
                lucene.Search(q);
                await using var warmDb = new BenchmarkDbContext(CONNECTION_STRING);
                await PostgresSearch.SearchAsync(warmDb, q);
            }
        }
        Console.WriteLine("   ✅ Warmup complete");

        // ── Step 6: Benchmark ──
        const int ITERATIONS = 50; // 50 iterations (DB queries take longer)
        Console.WriteLine($"\n🏁 Running benchmark ({ITERATIONS} iterations per query × {searchQueries.Length} queries)...\n");

        var results = new List<BenchmarkResult>();

        foreach (var query in searchQueries)
        {
            Console.Write($"   Testing \"{query}\"... ");

            // Lucene benchmark
            var luceneTimes = new List<double>();
            int luceneResultCount = 0;

            for (int i = 0; i < ITERATIONS; i++)
            {
                sw.Restart();
                var r = lucene.Search(query);
                sw.Stop();
                luceneTimes.Add(sw.Elapsed.TotalMilliseconds);
                if (i == 0) luceneResultCount = r.Count;
            }

            // PostgreSQL benchmark (ILIKE on name + description + instructor)
            var dbTimes = new List<double>();
            int dbResultCount = 0;

            for (int i = 0; i < ITERATIONS; i++)
            {
                await using var db = new BenchmarkDbContext(CONNECTION_STRING);
                sw.Restart();
                var r = await PostgresSearch.SearchAsync(db, query);
                sw.Stop();
                dbTimes.Add(sw.Elapsed.TotalMilliseconds);
                if (i == 0) dbResultCount = r.Count;
            }

            // PostgreSQL with paging benchmark
            var dbPagedTimes = new List<double>();
            int dbPagedResultCount = 0;

            for (int i = 0; i < ITERATIONS; i++)
            {
                await using var db = new BenchmarkDbContext(CONNECTION_STRING);
                sw.Restart();
                var r = await PostgresSearch.SearchWithPagingAsync(db, query);
                sw.Stop();
                dbPagedTimes.Add(sw.Elapsed.TotalMilliseconds);
                if (i == 0) dbPagedResultCount = r.Count;
            }

            results.Add(new BenchmarkResult
            {
                Query = query,
                LuceneAvgMs = luceneTimes.Average(),
                LuceneMedianMs = Median(luceneTimes),
                LuceneMinMs = luceneTimes.Min(),
                LuceneMaxMs = luceneTimes.Max(),
                LuceneP95Ms = Percentile(luceneTimes, 95),
                LuceneResults = luceneResultCount,
                DbAvgMs = dbTimes.Average(),
                DbMedianMs = Median(dbTimes),
                DbMinMs = dbTimes.Min(),
                DbMaxMs = dbTimes.Max(),
                DbP95Ms = Percentile(dbTimes, 95),
                DbResults = dbResultCount,
                DbPagedAvgMs = dbPagedTimes.Average(),
                DbPagedResults = dbPagedResultCount,
            });

            var speedup = dbTimes.Average() / Math.Max(luceneTimes.Average(), 0.001);
            Console.WriteLine($"Lucene: {luceneTimes.Average():F2}ms | PG: {dbTimes.Average():F2}ms | {speedup:F1}x");
        }

        // ── Step 7: Display Results ──
        Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("  📊 BENCHMARK RESULTS — Lucene.NET vs PostgreSQL ILIKE (ms, lower = better)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════\n");

        var table = new ConsoleTable(
            "Query", "Lucene Avg", "PG Avg", "Speedup",
            "Lucene P95", "PG P95", "L.Hits", "PG.Hits");

        foreach (var r in results)
        {
            var speedup = r.DbAvgMs / Math.Max(r.LuceneAvgMs, 0.001);
            table.AddRow(
                r.Query.Length > 18 ? r.Query.Substring(0, 18) + "…" : r.Query,
                r.LuceneAvgMs.ToString("F3"),
                r.DbAvgMs.ToString("F3"),
                $"{speedup:F1}x",
                r.LuceneP95Ms.ToString("F3"),
                r.DbP95Ms.ToString("F3"),
                r.LuceneResults,
                r.DbResults
            );
        }

        table.Write(Format.Minimal);

        // ── Paged Search Results ──
        Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("  📊 PostgreSQL ILIKE + ORDER BY + LIMIT 10 (Paged Search)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════\n");

        var pagedTable = new ConsoleTable("Query", "Lucene Avg", "PG Paged Avg", "Speedup");

        foreach (var r in results)
        {
            var speedup = r.DbPagedAvgMs / Math.Max(r.LuceneAvgMs, 0.001);
            pagedTable.AddRow(
                r.Query.Length > 18 ? r.Query.Substring(0, 18) + "…" : r.Query,
                r.LuceneAvgMs.ToString("F3"),
                r.DbPagedAvgMs.ToString("F3"),
                $"{speedup:F1}x"
            );
        }

        pagedTable.Write(Format.Minimal);

        // ── Summary Stats ──
        var avgLucene = results.Average(r => r.LuceneAvgMs);
        var avgDb = results.Average(r => r.DbAvgMs);
        var avgDbPaged = results.Average(r => r.DbPagedAvgMs);
        var overallSpeedup = avgDb / Math.Max(avgLucene, 0.001);
        var overallPagedSpeedup = avgDbPaged / Math.Max(avgLucene, 0.001);

        Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("  📈 SUMMARY");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine($"  Total courses:          10,000");
        Console.WriteLine($"  DB seed time:           {seedTime}ms");
        Console.WriteLine($"  Lucene index time:      {indexTime}ms");
        Console.WriteLine($"  Iterations per query:   {ITERATIONS}");
        Console.WriteLine($"  Number of queries:      {searchQueries.Length}");
        Console.WriteLine();
        Console.WriteLine($"  ┌──────────────────────────────────────────────────────┐");
        Console.WriteLine($"  │  Lucene Average:          {avgLucene,10:F3} ms               │");
        Console.WriteLine($"  │  PostgreSQL ILIKE Avg:    {avgDb,10:F3} ms               │");
        Console.WriteLine($"  │  PG + Paging + Sort Avg:  {avgDbPaged,10:F3} ms               │");
        Console.WriteLine($"  │                                                      │");
        Console.WriteLine($"  │  Lucene vs PG ILIKE:      {overallSpeedup,10:F1}x faster         │");
        Console.WriteLine($"  │  Lucene vs PG Paged:      {overallPagedSpeedup,10:F1}x faster         │");
        Console.WriteLine($"  └──────────────────────────────────────────────────────┘");

        if (overallSpeedup > 1)
            Console.WriteLine($"\n  🚀 Lucene is ~{overallSpeedup:F1}x FASTER than PostgreSQL ILIKE!");
        else
            Console.WriteLine($"\n  📉 PostgreSQL is ~{1 / overallSpeedup:F1}x faster (unusual for cloud DB).");

        Console.WriteLine("\n  📝 NOTES:");
        Console.WriteLine("  ─────────────────────────────────────────────────────────────");
        Console.WriteLine("  • PostgreSQL ILIKE '%term%' does FULL TABLE SCAN (no index help)");
        Console.WriteLine("  • Each PG query includes: network → Aiven Cloud → parse → scan → return");
        Console.WriteLine("  • Lucene runs entirely in local RAM — no network overhead");
        Console.WriteLine("  • In real app, PG would also JOIN Instructor, Tags, Enrollments");
        Console.WriteLine("  • Lucene also provides: relevance scoring, fuzzy, spell check, facets");
        Console.WriteLine();

        // ── Step 8: Export CSV ──
        var csvPath = Path.Combine(AppContext.BaseDirectory, "benchmark_results_pg.csv");
        ExportToCsv(results, csvPath, indexTime, seedTime);
        Console.WriteLine($"  📄 Results exported to: {csvPath}");

        // ── Step 9: Cleanup ──
        Console.Write("\n⏳ Cleaning up benchmark schema... ");
        await using var cleanupDb = new BenchmarkDbContext(CONNECTION_STRING);
        await cleanupDb.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS benchmark.courses");
        await cleanupDb.Database.ExecuteSqlRawAsync("DROP SCHEMA IF EXISTS benchmark");
        Console.WriteLine("✅ Done");

        lucene.Dispose();
        Console.WriteLine("\n🏁 Benchmark complete!");
    }

    static double Median(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        int mid = sorted.Count / 2;
        return sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2.0 : sorted[mid];
    }

    static double Percentile(List<double> values, int percentile)
    {
        var sorted = values.OrderBy(v => v).ToList();
        int index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
    }

    static void ExportToCsv(List<BenchmarkResult> results, string path, long indexTimeMs, long seedTimeMs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Query,LuceneAvgMs,LuceneMedianMs,LuceneMinMs,LuceneMaxMs,LuceneP95Ms,LuceneResults,DbAvgMs,DbMedianMs,DbMinMs,DbMaxMs,DbP95Ms,DbResults,DbPagedAvgMs,SpeedupX");

        foreach (var r in results)
        {
            var speedup = r.DbAvgMs / Math.Max(r.LuceneAvgMs, 0.001);
            sb.AppendLine($"\"{r.Query}\",{r.LuceneAvgMs:F4},{r.LuceneMedianMs:F4},{r.LuceneMinMs:F4},{r.LuceneMaxMs:F4},{r.LuceneP95Ms:F4},{r.LuceneResults},{r.DbAvgMs:F4},{r.DbMedianMs:F4},{r.DbMinMs:F4},{r.DbMaxMs:F4},{r.DbP95Ms:F4},{r.DbResults},{r.DbPagedAvgMs:F4},{speedup:F2}");
        }

        sb.AppendLine();
        sb.AppendLine($"# Lucene Index Build Time: {indexTimeMs}ms");
        sb.AppendLine($"# DB Seed Time: {seedTimeMs}ms");
        sb.AppendLine($"# Total Courses: 10000");
        sb.AppendLine($"# Iterations: 50");

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }
}

public class BenchmarkResult
{
    public string Query { get; set; } = null!;
    public double LuceneAvgMs { get; set; }
    public double LuceneMedianMs { get; set; }
    public double LuceneMinMs { get; set; }
    public double LuceneMaxMs { get; set; }
    public double LuceneP95Ms { get; set; }
    public int LuceneResults { get; set; }
    public double DbAvgMs { get; set; }
    public double DbMedianMs { get; set; }
    public double DbMinMs { get; set; }
    public double DbMaxMs { get; set; }
    public double DbP95Ms { get; set; }
    public int DbResults { get; set; }
    public double DbPagedAvgMs { get; set; }
    public int DbPagedResults { get; set; }
}
