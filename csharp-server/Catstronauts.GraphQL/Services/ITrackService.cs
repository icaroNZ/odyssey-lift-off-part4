using Catstronauts.GraphQL.Models;

namespace Catstronauts.GraphQL.Services;

/// <summary>
/// Service interface for interacting with the Catstronauts REST API.
/// Provides methods for fetching tracks, authors, modules, and incrementing view counts.
/// </summary>
/// <remarks>
/// This interface follows the Dependency Inversion Principle (SOLID).
/// Consumers depend on this abstraction rather than the concrete implementation,
/// enabling easy testing, flexibility, and loose coupling.
/// </remarks>
public interface ITrackService
{
    /// <summary>
    /// Retrieves all tracks for display on the homepage.
    /// </summary>
    /// <returns>A list of tracks. Returns empty list if none found.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    Task<List<Track>> GetTracksForHomeAsync();

    /// <summary>
    /// Retrieves a single track by its unique identifier.
    /// </summary>
    /// <param name="id">The unique track ID (e.g., "c_0", "t_01").</param>
    /// <returns>The track if found; null if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when id is null or empty.</exception>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    Task<Track?> GetTrackAsync(string id);

    /// <summary>
    /// Retrieves an author by their unique identifier.
    /// </summary>
    /// <param name="id">The unique author ID (e.g., "cat-1", "aut_01").</param>
    /// <returns>The author if found; null if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when id is null or empty.</exception>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    Task<Author?> GetAuthorAsync(string id);

    /// <summary>
    /// Retrieves all modules for a specific track.
    /// </summary>
    /// <param name="trackId">The unique track ID.</param>
    /// <returns>A list of modules for the track. Returns empty list if none found.</returns>
    /// <exception cref="ArgumentException">Thrown when trackId is null or empty.</exception>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    Task<List<Module>> GetTrackModulesAsync(string trackId);

    /// <summary>
    /// Retrieves a single module by its unique identifier.
    /// </summary>
    /// <param name="id">The unique module ID (e.g., "l_0", "mod_01").</param>
    /// <returns>The module if found; null if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when id is null or empty.</exception>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    Task<Module?> GetModuleAsync(string id);

    /// <summary>
    /// Increments the view count for a specific track.
    /// This operation is idempotent - calling multiple times is safe.
    /// </summary>
    /// <param name="trackId">The unique track ID.</param>
    /// <returns>The updated track with new view count.</returns>
    /// <exception cref="ArgumentException">Thrown when trackId is null or empty.</exception>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    Task<Track> IncrementTrackViewsAsync(string trackId);
}
