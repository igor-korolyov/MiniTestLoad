using CommandLine;

namespace MiniTestLoad.CommandLine;

internal class Options
{
    [Option('t', "threads", Default = 1, HelpText = "Number of threads to spawn")]
    public int ThreadCount { get; set; }

    [Option('r', "threshold", Default = 1000, HelpText = "Long request detection threshold in milliseconds")]
    public int LongRequestThreshold { get; set; }

    [Option('n', "count", Default = 5, HelpText = "Number of times requests are sent from each thread")]
    public int RequestCount { get; set; }

    [Option('d', "duration", HelpText = "Duration of run in seconds")]
    public int? Duration { get; set; }

    [Option('a', "auth", HelpText = "File containing Authorization header. Used for all requests")]
    public string? AuthorizationFile { get; set; }

    [Value(0, Min = 1, MetaName = "RequestFiles", HelpText = "Request files")]
    public required IEnumerable<string> RequestFiles { get; set; }
}
