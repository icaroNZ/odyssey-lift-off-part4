using System.Net;
using System.Net.Http.Json;
using Catstronauts.GraphQL.Models;

namespace Catstronauts.GraphQL.Services;

/// <summary>
/// Service implementation for interacting with the Catstronauts REST API.
/// Uses HttpClient to make HTTP requests to the external API.
/// </summary>
/// <remarks>
/// This service follows best practices:
/// - HttpClient injected via constructor (uses IHttpClientFactory)
/// - Async/await for all I/O operations
/// - Proper null handling and validation
/// - Consistent error handling
/// - DRY principle with reusable methods
/// </remarks>
public class TrackService : ITrackService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://odyssey-lift-off-rest-api.herokuapp.com/";

    /// <summary>
    /// Initializes a new instance of the TrackService.
    /// </summary>
    /// <param name="httpClient">
    /// The HttpClient instance injected by IHttpClientFactory.
    /// Do NOT create HttpClient with 'new' - this can cause socket exhaustion.
    /// </param>
    public TrackService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    /// <inheritdoc />
    public async Task<List<Track>> GetTracksForHomeAsync()
    {
        // Use the generic GET helper method
        var tracks = await GetAsync<List<Track>>("tracks");

        // Return empty list if API returns null (defensive programming)
        return tracks ?? new List<Track>();
    }

    /// <inheritdoc />
    public async Task<Track?> GetTrackAsync(string id)
    {
        ValidateId(id, nameof(id));

        try
        {
            return await GetAsync<Track>($"track/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // 404 is expected when track doesn't exist - return null
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<Author?> GetAuthorAsync(string id)
    {
        ValidateId(id, nameof(id));

        try
        {
            return await GetAsync<Author>($"author/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // 404 is expected when author doesn't exist - return null
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<Module>> GetTrackModulesAsync(string trackId)
    {
        ValidateId(trackId, nameof(trackId));

        var modules = await GetAsync<List<Module>>($"track/{trackId}/modules");
        return modules ?? new List<Module>();
    }

    /// <inheritdoc />
    public async Task<Module?> GetModuleAsync(string id)
    {
        ValidateId(id, nameof(id));

        try
        {
            return await GetAsync<Module>($"module/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // 404 is expected when module doesn't exist - return null
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<Track> IncrementTrackViewsAsync(string trackId)
    {
        ValidateId(trackId, nameof(trackId));

        // PATCH request to increment views
        // Note: This endpoint is idempotent per API design
        var response = await _httpClient.PatchAsync(
            $"track/{trackId}/numberOfViews",
            content: null);  // No request body needed

        response.EnsureSuccessStatusCode();

        var track = await response.Content.ReadFromJsonAsync<Track>();

        return track ?? throw new InvalidOperationException(
            $"API returned null track for ID {trackId}");
    }

    #region Private Helper Methods

    /// <summary>
    /// Generic helper method for GET requests.
    /// Reduces code duplication and ensures consistent error handling.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="endpoint">The API endpoint (relative to base URL).</param>
    /// <returns>The deserialized response object, or null if not found.</returns>
    /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
    private async Task<T?> GetAsync<T>(string endpoint)
    {
        // GetFromJsonAsync handles:
        // 1. Sending GET request
        // 2. Reading response body
        // 3. Deserializing JSON to T
        // 4. Throwing HttpRequestException on non-success status codes
        return await _httpClient.GetFromJsonAsync<T>(endpoint);
    }

    /// <summary>
    /// Validates that an ID parameter is not null or whitespace.
    /// Throws ArgumentException if invalid.
    /// </summary>
    /// <param name="id">The ID to validate.</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <exception cref="ArgumentException">Thrown when ID is null or whitespace.</exception>
    private static void ValidateId(string id, string paramName)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                $"ID cannot be null or empty.",
                paramName);
        }
    }

    #endregion
}
