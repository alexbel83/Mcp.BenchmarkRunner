using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace Mcp.BenchmarkRunner.Server.Tools;

[McpServerToolType]
public static class CompareTools
{
    private static readonly string RunsRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "runs"));

    public record CompareRequest(
        [Description("Base runId")] string BaseRunId,
        [Description("Head runId")] string HeadRunId);

    public record DiffRow(string Benchmark, double? MeanBase, double? MeanHead, double? MeanDeltaPct,
                          double? AllocBase, double? AllocHead, double? AllocDeltaPct);

    public record CompareResponse(string BaseRunId, string HeadRunId, DiffRow[] Rows);

    [McpServerTool, Description("Compare two runs and return deltas for common benchmarks (Mean, AllocatedBytes/Op).")]
    public static CompareResponse compare_runs(CompareRequest req)
    {
        var baseJson = LoadJson(req.BaseRunId);
        var headJson = LoadJson(req.HeadRunId);

        var baseMap = IndexByTitle(baseJson);
        var headMap = IndexByTitle(headJson);

        var keys = baseMap.Keys.Intersect(headMap.Keys).OrderBy(k => k);
        var rows = new List<DiffRow>();

        foreach (var k in keys)
        {
            var b = baseMap[k];
            var h = headMap[k];

            double? meanB = TryGet(b, "Statistics", "Mean");
            double? meanH = TryGet(h, "Statistics", "Mean");
            double? meanPct = (meanB.HasValue && meanH.HasValue && meanB != 0)
                ? (meanH / meanB - 1.0) * 100.0 : null;

            double? allocB = TryGet(b, "Memory", "AllocatedBytes/Op");
            double? allocH = TryGet(h, "Memory", "AllocatedBytes/Op");
            double? allocPct = (allocB.HasValue && allocH.HasValue && allocB != 0)
                ? (allocH / allocB - 1.0) * 100.0 : null;

            rows.Add(new DiffRow(k, meanB, meanH, meanPct, allocB, allocH, allocPct));
        }

        return new CompareResponse(req.BaseRunId, req.HeadRunId, rows.ToArray());
    }

    private static JsonElement LoadJson(string runId)
    {
        var dir = Path.Combine(RunsRoot, runId);
        if (!Directory.Exists(dir)) throw new DirectoryNotFoundException(dir);
        var jsonPath = Directory.EnumerateFiles(dir, "*.json", SearchOption.AllDirectories)
                                .OrderByDescending(File.GetLastWriteTimeUtc)
                                .FirstOrDefault()
            ?? throw new FileNotFoundException($"No JSON found for {runId}");
        using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
        return doc.RootElement.Clone();
    }

    private static Dictionary<string, JsonElement> IndexByTitle(JsonElement root)
    {
        var map = new Dictionary<string, JsonElement>();
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in root.EnumerateArray())
            {
                var title = el.GetPropertyOrNull("FullName")?.GetString()
                         ?? el.GetPropertyOrNull("Title")?.GetString()
                         ?? el.GetPropertyOrNull("Method")?.GetString()
                         ?? Guid.NewGuid().ToString();

                map[title!] = el;
            }
        }
        return map;
    }

    private static double? TryGet(JsonElement el, params string[] path)
    {
        var cur = el;
        foreach (var p in path)
        {
            var next = GetPropertyOrNull(cur, p);
            if (next is null) return null;
            cur = next.Value;
        }
        return cur.ValueKind == JsonValueKind.Number ? cur.GetDouble() : (double?)null;
    }

    private static JsonElement? GetPropertyOrNull(this JsonElement el, string name)
        => el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) ? v : (JsonElement?)null;
}
