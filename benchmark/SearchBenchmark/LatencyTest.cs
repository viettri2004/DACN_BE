using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SearchBenchmark;

/// <summary>
/// Đo network latency đến Aiven Cloud PostgreSQL
/// và tính lại kết quả benchmark sau khi trừ latency
/// </summary>
public class LatencyTest
{
    const string CONNECTION_STRING = "Host=dacn-3t-tonviettri2004-8168.l.aivencloud.com;Port=26599;Database=defaultdb;Username=avnadmin;Password=AVNS_TqbXUwvX9WgXhT-Pbd5;SSL Mode=Require;";

    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  🌐 ĐO NETWORK LATENCY → Aiven Cloud PostgreSQL            ║");
        Console.WriteLine("║  📊 Tính lại benchmark sau khi trừ latency                  ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

        var sw = new Stopwatch();

        // ── Step 1: Đo latency bằng SELECT 1 ──
        Console.WriteLine("🔬 Test 1: Network Latency (SELECT 1) — 100 iterations\n");

        var latencies = new List<double>();

        // Warmup
        for (int i = 0; i < 5; i++)
        {
            await using var warmDb = new BenchmarkDbContext(CONNECTION_STRING);
            await warmDb.Database.ExecuteSqlRawAsync("SELECT 1");
        }

        for (int i = 0; i < 100; i++)
        {
            await using var db = new BenchmarkDbContext(CONNECTION_STRING);
            sw.Restart();
            await db.Database.ExecuteSqlRawAsync("SELECT 1");
            sw.Stop();
            latencies.Add(sw.Elapsed.TotalMilliseconds);
        }

        var avgLatency = latencies.Average();
        var medLatency = Median(latencies);
        var minLatency = latencies.Min();
        var maxLatency = latencies.Max();
        var p95Latency = Percentile(latencies, 95);
        var p5Latency = Percentile(latencies, 5);

        Console.WriteLine($"  ┌──────────────────────────────────────────┐");
        Console.WriteLine($"  │  SELECT 1 (= pure network round-trip)   │");
        Console.WriteLine($"  ├──────────────────────────────────────────┤");
        Console.WriteLine($"  │  Average:   {avgLatency,10:F3} ms                │");
        Console.WriteLine($"  │  Median:    {medLatency,10:F3} ms                │");
        Console.WriteLine($"  │  Min:       {minLatency,10:F3} ms                │");
        Console.WriteLine($"  │  Max:       {maxLatency,10:F3} ms                │");
        Console.WriteLine($"  │  P5:        {p5Latency,10:F3} ms                │");
        Console.WriteLine($"  │  P95:       {p95Latency,10:F3} ms                │");
        Console.WriteLine($"  └──────────────────────────────────────────┘\n");

        // ── Step 2: Đo latency bằng SELECT COUNT(*) từ bảng thật ──
        Console.WriteLine("🔬 Test 2: SELECT COUNT(*) FROM courses (bảng production) — 50 iterations\n");

        var countLatencies = new List<double>();

        for (int i = 0; i < 5; i++)
        {
            await using var warmDb = new BenchmarkDbContext(CONNECTION_STRING);
            await warmDb.Database.ExecuteSqlRawAsync("SELECT COUNT(*) FROM public.\"Courses\"");
        }

        for (int i = 0; i < 50; i++)
        {
            await using var db = new BenchmarkDbContext(CONNECTION_STRING);
            sw.Restart();
            await db.Database.ExecuteSqlRawAsync("SELECT COUNT(*) FROM public.\"Courses\"");
            sw.Stop();
            countLatencies.Add(sw.Elapsed.TotalMilliseconds);
        }

        var avgCount = countLatencies.Average();
        Console.WriteLine($"  SELECT COUNT(*) Avg: {avgCount:F3} ms\n");

        // ── Step 3: Tính lại benchmark results ──
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  📊 TÍNH LẠI KẾT QUẢ SAU KHI TRỪ NETWORK LATENCY");
        Console.WriteLine($"  (Trừ {medLatency:F1}ms median latency mỗi query)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

        // Kết quả benchmark gốc (từ lần chạy trước)
        var results = new (string Query, double LuceneMs, double PgIlikeMs, double PgPagedMs)[]
        {
            ("Python",           4.84,  142.00, 153.49),
            ("React",            8.42,   87.77, 152.64),
            ("C#",               3.17,  127.94, 163.79),
            ("Docker",           2.75,  131.52, 171.21),
            ("Machine Learning", 9.61,  158.28, 167.78),
            ("Lập trình",        5.41,   73.87, 158.37),
            ("nâng cao",         3.44,   57.45, 223.70),
            ("thực chiến",       7.53,   97.98, 195.80),
            ("cơ bản",           2.38,   60.70, 192.88),
            ("dự án thực tế",    9.90,   59.13, 160.50),
            ("Java",             1.66,   90.05, 159.59),
            ("SQL",              1.50,   97.88, 164.81),
            ("Web",              1.39,   95.11, 153.69),
            ("AI",               1.61,   57.08, 167.31),
            ("Nguyễn",           7.52,   72.41, 157.57),
            ("Trần",             1.51,   76.53, 152.56),
            ("Blockchain",       2.23,  148.01, 157.56),
            ("Rust",             1.62,  128.33, 163.46),
            ("gRPC",             3.29,  116.03, 150.51),
            ("NFT",              3.11,  121.23, 160.36),
        };

        var netLatency = medLatency; // Dùng median cho ổn định

        Console.WriteLine("  Bảng 1: Lucene vs PG ILIKE (sau khi trừ network latency)");
        Console.WriteLine("  ┌──────────────────┬──────────┬──────────┬──────────┬──────────┐");
        Console.WriteLine("  │ Query            │ Lucene   │ PG (raw) │ PG (net) │ Speedup  │");
        Console.WriteLine("  │                  │   (ms)   │   (ms)   │   (ms)   │          │");
        Console.WriteLine("  ├──────────────────┼──────────┼──────────┼──────────┼──────────┤");

        double sumLucene = 0, sumPgRaw = 0, sumPgNet = 0;

        foreach (var r in results)
        {
            var pgNet = Math.Max(r.PgIlikeMs - netLatency, 0.1);
            var speedup = pgNet / Math.Max(r.LuceneMs, 0.001);
            var q = r.Query.PadRight(16);
            if (q.Length > 16) q = q.Substring(0, 14) + "…";
            Console.WriteLine($"  │ {q} │ {r.LuceneMs,8:F2} │ {r.PgIlikeMs,8:F2} │ {pgNet,8:F2} │ {speedup,7:F1}x │");
            sumLucene += r.LuceneMs;
            sumPgRaw += r.PgIlikeMs;
            sumPgNet += pgNet;
        }

        Console.WriteLine("  ├──────────────────┼──────────┼──────────┼──────────┼──────────┤");
        var avgL = sumLucene / results.Length;
        var avgPr = sumPgRaw / results.Length;
        var avgPn = sumPgNet / results.Length;
        var avgSp = avgPn / Math.Max(avgL, 0.001);
        Console.WriteLine($"  │ TRUNG BÌNH       │ {avgL,8:F2} │ {avgPr,8:F2} │ {avgPn,8:F2} │ {avgSp,7:F1}x │");
        Console.WriteLine("  └──────────────────┴──────────┴──────────┴──────────┴──────────┘");

        Console.WriteLine($"\n  Bảng 2: Lucene vs PG Paged (sau khi trừ network latency)");
        Console.WriteLine("  ┌──────────────────┬──────────┬──────────┬──────────┬──────────┐");
        Console.WriteLine("  │ Query            │ Lucene   │ PG (raw) │ PG (net) │ Speedup  │");
        Console.WriteLine("  │                  │   (ms)   │   (ms)   │   (ms)   │          │");
        Console.WriteLine("  ├──────────────────┼──────────┼──────────┼──────────┼──────────┤");

        double sumPgPRaw = 0, sumPgPNet = 0;

        foreach (var r in results)
        {
            var pgNet = Math.Max(r.PgPagedMs - netLatency, 0.1);
            var speedup = pgNet / Math.Max(r.LuceneMs, 0.001);
            var q = r.Query.PadRight(16);
            if (q.Length > 16) q = q.Substring(0, 14) + "…";
            Console.WriteLine($"  │ {q} │ {r.LuceneMs,8:F2} │ {r.PgPagedMs,8:F2} │ {pgNet,8:F2} │ {speedup,7:F1}x │");
            sumPgPRaw += r.PgPagedMs;
            sumPgPNet += pgNet;
        }

        Console.WriteLine("  ├──────────────────┼──────────┼──────────┼──────────┼──────────┤");
        var avgPpR = sumPgPRaw / results.Length;
        var avgPpN = sumPgPNet / results.Length;
        var avgSpP = avgPpN / Math.Max(avgL, 0.001);
        Console.WriteLine($"  │ TRUNG BÌNH       │ {avgL,8:F2} │ {avgPpR,8:F2} │ {avgPpN,8:F2} │ {avgSpP,7:F1}x │");
        Console.WriteLine("  └──────────────────┴──────────┴──────────┴──────────┴──────────┘");

        // ── Summary ──
        Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  📈 TỔNG KẾT");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");
        Console.WriteLine($"  Network latency (median SELECT 1):  {medLatency:F1} ms");
        Console.WriteLine($"  Network latency (avg SELECT 1):     {avgLatency:F1} ms\n");
        Console.WriteLine($"  ┌──────────────────────────────────────────────────────────┐");
        Console.WriteLine($"  │                  │ Bao gồm network │ Sau khi trừ network │");
        Console.WriteLine($"  ├──────────────────┼─────────────────┼─────────────────────┤");
        Console.WriteLine($"  │ Lucene Avg       │     {avgL,6:F2} ms    │       {avgL,6:F2} ms      │");
        Console.WriteLine($"  │ PG ILIKE Avg     │    {avgPr,6:F2} ms    │      {avgPn,6:F2} ms      │");
        Console.WriteLine($"  │ PG Paged Avg     │   {avgPpR,6:F2} ms    │     {avgPpN,6:F2} ms      │");
        Console.WriteLine($"  │ Speedup (ILIKE)  │      {(avgPr/avgL),6:F1}x     │        {avgSp,6:F1}x       │");
        Console.WriteLine($"  │ Speedup (Paged)  │      {(avgPpR/avgL),6:F1}x     │        {avgSpP,6:F1}x       │");
        Console.WriteLine($"  └──────────────────┴─────────────────┴─────────────────────┘\n");

        Console.WriteLine("  📝 GHI CHÚ:");
        Console.WriteLine("  ─────────────────────────────────────────────────────────────");
        Console.WriteLine("  • 'PG (net)' = thời gian PG gốc - network latency (SELECT 1)");
        Console.WriteLine("  • Đây là ước tính thời gian THUẦN của PostgreSQL query");
        Console.WriteLine("    (loại bỏ overhead mạng, giữ lại: parse SQL, plan, scan, sort)");
        Console.WriteLine("  • Lucene không cần trừ gì vì chạy local in-process");
        Console.WriteLine("  • Trong thực tế, app và DB thường cùng region/VPC nên latency");
        Console.WriteLine("    thấp hơn (~1-5ms), nhưng query execution time vẫn giữ nguyên");
        Console.WriteLine();
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
}
