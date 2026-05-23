using System.Net.Http.Headers;
using System.Threading;
using Blazored.LocalStorage;
using Microsoft.Extensions.DependencyInjection;
using Titan.Client.Services.Apibase;

namespace Titan.Client.Services.AppServices;

public class JwtHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;
    private readonly IServiceProvider _serviceProvider;
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public JwtHandler(ILocalStorageService localStorage, IServiceProvider serviceProvider)
    {
        _localStorage = localStorage;
        _serviceProvider = serviceProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;

        // Skip token injection for authentication endpoints
        if (path.Contains("/api/auth/"))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        // Add current token
        var token = await _localStorage.GetItemAsStringAsync("titan_token");
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Auto Refresh on 401
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                // Double-check: Read the latest token from storage.
                // If it is different from the token we used for this request, it has already been refreshed!
                var currentToken = await _localStorage.GetItemAsStringAsync("titan_token");
                if (!string.IsNullOrEmpty(currentToken) && currentToken != token)
                {
                    // Token was already refreshed by another concurrent request!
                    // Just retry the request with the new token.
                    var retryRequest = await CloneRequestAsync(request);
                    retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentToken);
                    return await base.SendAsync(retryRequest, cancellationToken);
                }

                // If they are the same (or we have no token), we are the first to encounter the 401.
                // So we perform the refresh.
                var authService = _serviceProvider.GetRequiredService<AuthService>();
                var refreshed = await authService.RefreshAsync();
                if (refreshed)
                {
                    var newToken = await _localStorage.GetItemAsStringAsync("titan_token");
                    if (!string.IsNullOrEmpty(newToken))
                    {
                        var retryRequest = await CloneRequestAsync(request);
                        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                        return await base.SendAsync(retryRequest, cancellationToken);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage source)
    {
        var clone = new HttpRequestMessage(source.Method, source.RequestUri);

        if (source.Content != null)
        {
            var bytes = await source.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in source.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var header in source.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }
}