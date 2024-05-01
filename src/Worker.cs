using MiniTestLoad.CommandLine;
using MiniTestLoad.Helpers;
using MiniTestLoad.Models;
using System.Diagnostics;

namespace MiniTestLoad;

internal class Worker(int threadNumber, Mode mode, Options options, List<Request> requests, string authorization,
    CancellationToken cancellationToken)
{
    private int _send = 0;
    private int _successful = 0;
    private int _failed = 0;
    private int _long = 0;
    private readonly List<TimeSpan> _durations = [];
    private HttpClient? _httpClient;

    internal async Task Start()
    {
        UpdateWorkerSummaryRow();

        try
        {
            _httpClient = new HttpClient();
            switch (mode)
            {
                case Mode.Repetitions:
                    await DoRepetitions();
                    break;
                case Mode.Duration:
                    await DoDuration();
                    break;
            }
        }
        catch (TaskCanceledException)
        {
            Display.Instance.AddLogRow($"T#{threadNumber,-2} has been stopped");
        }
        catch (OperationCanceledException)
        {
            Display.Instance.AddLogRow($"T#{threadNumber,-2} has been stopped");
        }
        finally
        {
            _httpClient?.Dispose();
        }
    }

    private async Task DoDuration()
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(options.Duration.GetValueOrDefault() * 1000);

        while (true)
        {
            foreach (var request in HttpRequestMessageCreator.Create(requests, authorization))
            {
                cts.Token.ThrowIfCancellationRequested();
                await ProcessRequest(request, cts.Token);
            }
        }
    }

    private async Task DoRepetitions()
    {
        for (int i = 0; i < options.RequestCount; i++)
        {
            foreach (var request in HttpRequestMessageCreator.Create(requests, authorization))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ProcessRequest(request, cancellationToken);
            }
        }
    }

    private async Task ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_httpClient is null)
        {
            throw new UnreachableException("HttpClient instance is missing");
        }

        var sw = Stopwatch.StartNew();
        _send++;
        var responseLength = 0;
        int statusCode;
        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            statusCode = (int)response.StatusCode;
            if (response.IsSuccessStatusCode)
            {
                _successful++;
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                responseLength = body.Length;
            }
            else
            {
                _failed++;
            }
        }
        catch (HttpRequestException)
        {
            _failed++;
            statusCode = -1;
        }

        var elapsed = sw.Elapsed;
        _durations.Add(elapsed);
        if (elapsed.TotalMilliseconds >= options.LongRequestThreshold)
        {
            _long++;
            Display.Instance.AddLogRow(
                $"T#{threadNumber,-2} {statusCode} Len:{responseLength,-6} Elapsed:{elapsed:mm\\:ss\\.fff}");
        }

        UpdateWorkerSummaryRow();
    }

    private void UpdateWorkerSummaryRow()
    {
        var durationAggregate = _durations.Aggregate(
            new DurationAggregate(0, 0, int.MaxValue, 0),
            (acc, timeSpan) =>
            {
                var totalMilliseconds = (int)timeSpan.TotalMilliseconds;
                return new DurationAggregate(acc.Count + 1, acc.Duration + totalMilliseconds,
                    Math.Min(acc.Min, totalMilliseconds), Math.Max(acc.Max, totalMilliseconds));
            });

        var minDuration = durationAggregate.Count == 0 ? "N/A" : $"{durationAggregate.Min} ms";
        var maxDuration = durationAggregate.Count == 0 ? "N/A" : $"{durationAggregate.Max} ms";
        var averageDuration = durationAggregate.Count == 0 ?
            "N/A" :
            $"{durationAggregate.Duration / durationAggregate.Count} ms";

        Display.Instance.SetThreadRow(threadNumber,
            $"T#{threadNumber,-2} S:{_send,-5} R:{_successful,-5} F:{_failed,-5} L:{_long,-5} " +
            $"Min:{minDuration,10} Max:{maxDuration,10} Avg:{averageDuration,10}");
    }

    private readonly record struct DurationAggregate(int Count, int Duration, int Min, int Max);
}
