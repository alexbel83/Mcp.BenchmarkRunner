using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;

public static class Jobs
{
    public static ManualConfig ApplyPreset(ManualConfig cfg, string jobName)
    {
        cfg.AddDiagnoser(MemoryDiagnoser.Default);

        var job = jobName switch
        {
            "Short"  => Job.ShortRun,
            "Long"   => Job.Default.WithIterationCount(20).WithWarmupCount(5),
            _        => Job.MediumRun
        };

        return cfg.AddJob(job);
    }
}
