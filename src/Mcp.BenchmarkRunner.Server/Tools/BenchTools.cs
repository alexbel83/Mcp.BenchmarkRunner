using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace Mcp.BenchmarkRunner.Server.Tools;

[McpServerToolType]
public static class BenchTools
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")); 

    private static readonly string SampleProj =
        Path.Combine(SolutionRoot, "src", "SampleBenchmarks", "SampleBenchmarks.csproj");

    private static readonly string RunsRoot =
        Path.Combine(SolutionRoot, "runs");


    public record RunBenchRequest(
        [Description("BenchmarkDotNet glob filter, e.g. *Hash*")] string? Filter = null,
        [Description("Job preset: Short|Medium|Long")] string Job = "Medium",
        [Description("Exporters: json,fulljson,md,csv")] string Exporters = "json,md",
        [Description("Timeout in seconds")] int TimeoutSec = 600
    );

    public record RunBenchResponse(string RunId, string ArtifactsDir, object? Summary);

    [McpServerTool, Description("Run BenchmarkDotNet with optional filter and job preset; returns runId and brief summary.")]
    public static async Task<RunBenchResponse> run_bench(RunBenchRequest req)
    {
        Directory.CreateDirectory(RunsRoot);
        var runId = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        var artifactsDir = Path.Combine(RunsRoot, runId);
        Directory.CreateDirectory(artifactsDir);

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = SolutionRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            // dotnet run --project SampleBenchmarks -- --artifacts <dir> --exporters <...> --job <...> [--filter <...>]
            ArgumentList = { "run", "--project", SampleProj, "-c", "Release", "--",
            "--artifacts", artifactsDir,
            "--exporters", req.Exporters,
            "--job", req.Job }
        };

        if (!string.IsNullOrWhiteSpace(req.Filter))
        {
            psi.ArgumentList.Add("--filter");
            psi.ArgumentList.Add(req.Filter!);
        }

        using var proc = Process.Start(psi)!;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(req.TimeoutSec));
        var stdoutTask = proc.StandardOutput.ReadToEndAsync(cts.Token);
        var stderrTask = proc.StandardError.ReadToEndAsync(cts.Token);

        if (!proc.WaitForExit((int)TimeSpan.FromSeconds(req.TimeoutSec).TotalMilliseconds))
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException($"Benchmark run timed out after {req.TimeoutSec}s");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (proc.ExitCode != 0)
            throw new ApplicationException($"Benchmark process failed:\n{stderr}\n{stdout}");

        var json = Directory.EnumerateFiles(artifactsDir, "*.json", SearchOption.AllDirectories)
                            .OrderByDescending(File.GetLastWriteTimeUtc)
                            .Select(File.ReadAllText)
                            .FirstOrDefault();

        object? summary = null;
        try { summary = json is null ? null : JsonSerializer.Deserialize<object>(json); } catch { }

        return new RunBenchResponse(runId, artifactsDir, summary);
    }

    public record GetResultsRequest([Description("runId returned by run_bench")] string RunId);
    public record GetResultsResponse(string RunId, object? JsonSummary);

    [McpServerTool, Description("Return parsed JSON summary for a given runId.")]
    public static GetResultsResponse get_results(GetResultsRequest req)
    {
        var dir = Path.Combine(RunsRoot, req.RunId);
        if (!Directory.Exists(dir)) throw new DirectoryNotFoundException(dir);

        var jsonPath = Directory.EnumerateFiles(dir, "*.json", SearchOption.AllDirectories)
                                .OrderByDescending(File.GetLastWriteTimeUtc)
                                .FirstOrDefault();

        object? obj = jsonPath is null ? null : JsonSerializer.Deserialize<object>(File.ReadAllText(jsonPath));
        return new GetResultsResponse(req.RunId, obj);
    }

    public record ListArtifactsRequest([Description("runId returned by run_bench")] string RunId);
    public record ListArtifactsResponse(string RunId, string[] Files);

    [McpServerTool, Description("List artifact files for a given runId (JSON/MD/CSV/Log).")]
    public static ListArtifactsResponse list_artifacts(ListArtifactsRequest req)
    {
        var dir = Path.Combine(RunsRoot, req.RunId);
        if (!Directory.Exists(dir)) throw new DirectoryNotFoundException(dir);

        var files = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).ToArray();
        return new ListArtifactsResponse(req.RunId, files);
    }
}
