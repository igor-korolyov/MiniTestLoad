using MiniTestLoad.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MiniTestLoad.Helpers;

internal static class RequestFileParser
{
    internal static async Task<Request> ReadFormFile(string fileName, CancellationToken cancellationToken)
    {
        var content = await File.ReadAllLinesAsync(fileName, Encoding.UTF8, cancellationToken);

        var requestPart = RequestFilePart.MethodAndUrl;
        HttpMethod? method = null;
        Uri? uri = null;
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var body = new StringBuilder();
        foreach (var line in content)
        {
            switch (requestPart)
            {
                case RequestFilePart.MethodAndUrl:
                    if (IsEmptyLineOrComment(line))
                    {
                        continue;
                    }

                    if (!TryParseRequestLine(line, out method, out uri))
                    {
                        throw new InvalidOperationException(
                            $"Request file {fileName} contains an invalid request line.\n{line}");
                    }

                    requestPart = RequestFilePart.Headers;
                    break;

                case RequestFilePart.Headers:
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        requestPart = RequestFilePart.Body;
                        continue;
                    }

                    if (IsComment(line))
                    {
                        continue;
                    }

                    var colonPosition = line.IndexOf(':');
                    if (colonPosition < 1)
                    {
                        throw new InvalidOperationException(
                            $"Request file {fileName} contains an invalid request header.\n{line}");
                    }

                    var key = line[..colonPosition].Trim();
                    var value = line[(colonPosition + 1)..].Trim();

                    // Support multi-valued headers by combining them into a single one, separated with commas.
                    if (!headers.TryAdd(key, value))
                    {
                        headers[key] += ", " + value;
                    }

                    break;

                case RequestFilePart.Body:
                    body.Append(line);
                    break;
            }
        }

        if (method is null || uri is null)
        {
            throw new InvalidOperationException($"Request file {fileName} does not contain a valid request line.");
        }

        return new Request(method, uri, headers, body.Length > 0 ? body.ToString() : null);
    }

    private static bool TryParseRequestLine(string requestLine, [NotNullWhen(true)] out HttpMethod? method,
        [NotNullWhen(true)] out Uri? uri)
    {
        method = null;
        uri = null;
        var parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 2)
        {
            return false;
        }

        method = HttpMethod.Parse(parts[0]);
        return Uri.TryCreate(parts[1], UriKind.Absolute, out uri);
    }

    private static bool IsEmptyLineOrComment(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return true;
        }

        var text = line.TrimStart();

        if (text.StartsWith('#') || text.StartsWith("//"))
        {
            return true;
        }

        return false;
    }

    private static bool IsComment(string line)
    {
        var text = line.TrimStart();

        if (text.StartsWith('#') || text.StartsWith("//"))
        {
            return true;
        }

        return false;
    }

    private enum RequestFilePart
    {
        MethodAndUrl,
        Headers,
        Body
    }
}
