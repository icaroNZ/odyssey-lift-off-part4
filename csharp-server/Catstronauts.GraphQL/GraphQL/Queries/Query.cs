using Catstronauts.GraphQL.Models;
using Catstronauts.GraphQL.Services;

namespace Catstronauts.GraphQL.GraphQL.Queries;

/// <summary>
/// Root query type for the GraphQL API.
/// Contains all top-level query operations for querying tracks, authors, and modules.
/// </summary>
/// <remarks>
/// In Hot Chocolate, methods prefixed with "Get" automatically become GraphQL fields
/// with the "Get" prefix removed. For example:
/// - GetTracksForHome() → tracksForHome field
/// - GetTrack() → track field
///
/// The [Service] attribute enables dependency injection of services directly into
/// resolver methods, allowing each query to access the required dependencies.
/// </remarks>
public class Query
{
    /// <summary>
    /// Retrieves all tracks for display on the homepage.
    /// This is the main entry point for browsing available learning content.
    /// </summary>
    /// <param name="trackService">Injected service for fetching track data from REST API</param>
    /// <returns>A list of all available tracks. Returns empty list if none found.</returns>
    /// <exception cref="HttpRequestException">Thrown when the upstream REST API is unavailable</exception>
    public async Task<List<Track>> GetTracksForHome(
        [Service] ITrackService trackService)
    {
        return await trackService.GetTracksForHomeAsync();
    }

    /// <summary>
    /// Retrieves a single track by its unique identifier.
    /// Used for track detail pages where full information is needed.
    /// </summary>
    /// <param name="id">The unique track identifier (e.g., "c_0", "c_1")</param>
    /// <param name="trackService">Injected service for fetching track data from REST API</param>
    /// <returns>The track if found; null if not found</returns>
    /// <exception cref="ArgumentException">Thrown when id is null or empty</exception>
    /// <exception cref="HttpRequestException">Thrown when the upstream REST API is unavailable</exception>
    public async Task<Track?> GetTrack(
        string id,
        [Service] ITrackService trackService)
    {
        return await trackService.GetTrackAsync(id);
    }

    /// <summary>
    /// Field resolver for the 'author' field on the Track type.
    /// This is called by Hot Chocolate when a query requests track.author.
    /// </summary>
    /// <param name="track">The parent Track object (provided by Hot Chocolate)</param>
    /// <param name="trackService">Injected service for fetching author data</param>
    /// <returns>The author of the track; null if not found</returns>
    /// <remarks>
    /// This is a "field resolver" pattern in GraphQL. When a query requests:
    ///   query { track(id: "c_0") { title author { name } } }
    ///
    /// Hot Chocolate:
    /// 1. Calls GetTrack() to get the Track object
    /// 2. Sees that 'author' is requested
    /// 3. Calls this method, passing the Track as [Parent]
    /// 4. Uses track.AuthorId to fetch the Author
    ///
    /// This pattern solves the "N+1 problem" when combined with DataLoaders (Stage 6).
    /// Without DataLoaders: If you query 10 tracks, this runs 10 separate author queries.
    /// With DataLoaders: All 10 author IDs are batched into a single query.
    /// </remarks>
    [GraphQLName("author")]
    public async Task<Author?> GetAuthor(
        [Parent] Track track,
        [Service] ITrackService trackService)
    {
        return await trackService.GetAuthorAsync(track.AuthorId);
    }

    /// <summary>
    /// Field resolver for the 'modules' field on the Track type.
    /// This is called by Hot Chocolate when a query requests track.modules.
    /// </summary>
    /// <param name="track">The parent Track object (provided by Hot Chocolate)</param>
    /// <param name="trackService">Injected service for fetching module data</param>
    /// <returns>A list of modules for the track. Returns empty list if none found.</returns>
    /// <remarks>
    /// Similar to the author resolver, this is a field resolver that fetches
    /// nested data on-demand. Only executes when the 'modules' field is requested
    /// in the GraphQL query.
    ///
    /// Example query:
    ///   query {
    ///     track(id: "c_0") {
    ///       title
    ///       modules {
    ///         id
    ///         title
    ///         length
    ///       }
    ///     }
    ///   }
    /// </remarks>
    [GraphQLName("modules")]
    public async Task<List<Module>> GetModules(
        [Parent] Track track,
        [Service] ITrackService trackService)
    {
        return await trackService.GetTrackModulesAsync(track.Id);
    }
}
