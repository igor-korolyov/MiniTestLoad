using CommandLine;
using MiniTestLoad.CommandLine;
using MiniTestLoad.Helpers;
using MiniTestLoad.Models;
using System.Text;

namespace MiniTestLoad
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            return await Parser.Default.ParseArguments<Options>(args)
                .MapResult(RunAsync, _ => Task.FromResult(1));
        }

        private static async Task<int> RunAsync(Options options)
        {
            if (!ValidateOptions(options))
            {
                return 1;
            }

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                cts.Cancel();
                e.Cancel = true;
            };

            var requests = new List<Request>();

            foreach (var file in options.RequestFiles)
            {
                var request = await RequestFileParser.ReadFormFile(file, cts.Token);
                requests.Add(request);
            }

            var authorizationDisplay = "None";
            var authorization = string.Empty;
            if (!string.IsNullOrWhiteSpace(options.AuthorizationFile))
            {
                authorizationDisplay = options.AuthorizationFile;
                authorization = await File.ReadAllTextAsync(options.AuthorizationFile, Encoding.UTF8, cts.Token);
                if (string.IsNullOrWhiteSpace(authorization))
                {
                    throw new InvalidOperationException($"Bearer file {options.AuthorizationFile} is empty.");
                }
            }

            Console.CursorVisible = false;
            Display.Instance.ThreadCount = options.ThreadCount;
            var mode = options.Duration.HasValue ? Mode.Duration : Mode.Repetitions;
            var modeDisplay = mode == Mode.Repetitions ?
                $"Repetitions={options.RequestCount}" :
                $"Duration={options.Duration}s";
            Display.Instance.TitleRow = $"Threads: {options.ThreadCount,-2} " +
                $"Requests: {options.RequestFiles.Count(),-2} Mode: {modeDisplay,-20} " +
                $"LongReqThreshold: {options.LongRequestThreshold}ms Authorization: {authorizationDisplay}";
            Display.Instance.BottomRow = "Press Ctrl+C to exit";

            var tasks = Enumerable.Range(0, options.ThreadCount)
                .Select(i => Task.Run(
                    () => ThreadProc(i, mode, options, requests, authorization, cts.Token), cts.Token));
            await Task.WhenAll(tasks);
            Display.Instance.AddLogRow("All done");
            Console.CursorVisible = true;

            return 0;
        }

        private static Task ThreadProc(int threadNumber, Mode mode, Options options, List<Request> requests,
            string authorization, CancellationToken cancellationToken)
        {
            var processor = new Worker(threadNumber, mode, options, requests, authorization, cancellationToken);
            return processor.Start();
        }

        private static bool ValidateOptions(Options options)
        {
            const int MaxThreadCount = 20;
            const int MaxRequestCount = 10_000;
            const int MaxDuration = 60 * 60 * 24;

            if (options.ThreadCount < 1 || options.ThreadCount > MaxThreadCount)
            {
                Console.WriteLine($"{nameof(options.ThreadCount)} should be in range 1..{MaxThreadCount}");
                return false;
            }

            if (options.LongRequestThreshold < 1)
            {
                Console.WriteLine($"{nameof(options.LongRequestThreshold)} should be greater than 0");
                return false;
            }

            if (options.RequestCount < 1 || options.RequestCount > MaxRequestCount)
            {
                Console.WriteLine($"{nameof(options.RequestCount)} should be in range 0..{MaxRequestCount}");
                return false;
            }

            if (options.Duration.HasValue && options.RequestCount != 5)
            {
                Console.WriteLine($"{nameof(options.RequestCount)} should not be specified if " +
                    $"{nameof(options.Duration)} is specified");
                return false;
            }

            if (options.Duration.HasValue && (options.Duration < 1 || options.Duration > MaxDuration))
            {
                Console.WriteLine($"{nameof(options.Duration)} should be in range 1..{MaxDuration}");
                return false;
            }

            if (options.AuthorizationFile is not null && (string.IsNullOrWhiteSpace(options.AuthorizationFile) ||
                !File.Exists(options.AuthorizationFile)))
            {
                Console.WriteLine($"{nameof(options.AuthorizationFile)} '{options.AuthorizationFile}' does not exist");
                return false;
            }

            foreach (var requestFile in options.RequestFiles)
            {
                if (string.IsNullOrWhiteSpace(requestFile) || !File.Exists(requestFile))
                {
                    Console.WriteLine($"{nameof(options.RequestFiles)} '{requestFile}' does not exist");
                    return false;
                }
            }

            return true;
        }
    }
}
