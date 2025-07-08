using System.Net.Http.Headers;

namespace frontend.Services;

public class AuthenticatedHttpClientHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;

    public AuthenticatedHttpClientHandler(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Get the token from storage
        var token = await _localStorage.GetItemAsync<string>("authToken") ?? 
                   await _localStorage.GetSessionItemAsync<string>("authToken");

        // Add Authorization header if token exists
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}