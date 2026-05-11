namespace HierarchyAccounts.Console.Services;

using System.Net.Http.Json;
using HierarchyAccounts.Application.DTOs;

/// <summary>
/// HTTP client wrapper for calling the Hierarchy Accounts REST API.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    /// <summary>
    /// Fetches the full account tree from GET /api/accounts/tree.
    /// </summary>
    public async Task<AccountTreeDto?> GetFullTreeAsync()
    {
        var response = await _http.GetAsync("api/accounts/tree");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AccountTreeDto>();
    }

    /// <summary>
    /// Fetches the subtree rooted at the given accountId from GET /api/accounts/{id}/subtree.
    /// </summary>
    public async Task<AccountTreeDto?> GetSubtreeAsync(Guid accountId)
    {
        var response = await _http.GetAsync($"api/accounts/{accountId}/subtree");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AccountTreeDto>();
    }
}
