using MiniTestLoad.Models;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;

namespace MiniTestLoad.Helpers;

internal static class HttpRequestMessageCreator
{
    private static readonly string ContentTypeHeaderName = "Content-Type";
    private static readonly string[] ContentHeaderNames =
    [
        "Allow", "Content-Disposition", "Content-Encoding", "Content-Language", "Content-Length", "Content-Location",
        "Content-MD5", "Content-Range", ContentTypeHeaderName, "Expires", "Last-Modified"
    ];

    internal static List<HttpRequestMessage> Create(List<Request> requests, string authorization)
    {
        var result = new List<HttpRequestMessage>();
        foreach (var request in requests)
        {
            var httpRequest = new HttpRequestMessage(request.Method, request.Uri);

            foreach (var (key, value) in request.Headers
                .Where(kvp => !ContentHeaderNames.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase)))
            {
                httpRequest.Headers.Add(key, value);
            }

            if (!string.IsNullOrEmpty(authorization))
            {
                var separatorPosition = authorization.IndexOf(' ');
                if (separatorPosition < 1)
                {
                    throw new InvalidOperationException($"Authorization data is invalid.\n{authorization}");
                }
                var scheme = authorization[..separatorPosition].Trim();
                var value = authorization[(separatorPosition + 1)..].Trim();

                httpRequest.Headers.Authorization = new AuthenticationHeaderValue(scheme, value);
            }

            if (!string.IsNullOrEmpty(request.Body))
            {
                // Set the most common content type and encoding. Can be overridden by Content-Type header in the
                // request file.
                httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, MediaTypeNames.Application.Json);

                foreach (var (key, value) in request.Headers
                    .Where(kvp => ContentHeaderNames.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase)))
                {
                    // Content-Type header does not allow multiple values, so remove previous to set a new one.
                    if (key.Equals(ContentTypeHeaderName, StringComparison.OrdinalIgnoreCase))
                    {
                        httpRequest.Content.Headers.Remove(ContentTypeHeaderName);
                    }

                    httpRequest.Content.Headers.Add(key, value);
                }
            }

            result.Add(httpRequest);
        }

        return result;
    }
}
