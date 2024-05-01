namespace MiniTestLoad.Models;

internal readonly record struct Request(HttpMethod Method, Uri Uri, Dictionary<string, string> Headers,
    string? Body = null);
