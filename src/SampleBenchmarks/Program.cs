using System.CommandLine;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

var artifacts = new Option<string>("--artifacts", () => "BenchmarkDotNet.Artifacts", "Artifacts output dir");
var exporters = new Option<string>("--exporters", () => "json,md", "json,fulljson,md,csv");
var filter    = new Option<string?>("--filter", "Glob filter: e.g. *Hash*");
var job       = new Option<string>("--job", () => "Medium", "Short|Medium|Long");

var root = new RootCommand("Sample Benchmarks");
root.AddOption(artifacts); root.AddOption(exporters); root.AddOption(filter); root.AddOption(job);

root.SetHandler(async (string art, string exp, string? f, string jobName) =>
{
    var cfg = ManualConfig.CreateEmpty()
        .AddLogger(ConsoleLogger.Default)
        .WithArtifactsPath(art);

    cfg = Jobs.ApplyPreset(cfg, jobName);

    foreach (var e in exp.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        switch (e.ToLowerInvariant())
        {
            case "json":     cfg.AddExporter(JsonExporter.Default); break;
            case "fulljson": cfg.AddExporter(JsonExporter.Full); break;
            case "md":       cfg.AddExporter(MarkdownExporter.GitHub); break;
            case "csv":      cfg.AddExporter(CsvExporter.Default); break;
        }
    }

    var switcher = new BenchmarkSwitcher(new[] { typeof(HashBench) });
    await Task.Run(() => switcher.Run(
        args: f is null ? Array.Empty<string>() : new[] { $"--filter={f}" }, config: cfg));
}, artifacts, exporters, filter, job);

return await root.InvokeAsync(args);
