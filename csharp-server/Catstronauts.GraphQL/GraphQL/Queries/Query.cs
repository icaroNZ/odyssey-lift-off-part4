using Catstronauts.GraphQL.GraphQL.DataLoaders;
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
    /// NOW OPTIMIZED: Uses DataLoader to batch and cache author requests!
    /// </summary>
    /// <param name="track">The parent Track object (provided by Hot Chocolate)</param>
    /// <param name="authorDataLoader">Injected DataLoader for batching author fetches</param>
    /// <returns>The author of the track; null if not found</returns>
    /// <remarks>
    /// This is a "field resolver" pattern in GraphQL WITH DataLoader optimization.
    /// When a query requests multiple tracks with authors:
    ///   query {
    ///     tracksForHome {
    ///       title
    ///       author { name }  ← GetAuthor() called for each track
    ///     }
    ///   }
    ///
    /// Hot Chocolate with DataLoader:
    /// 1. Calls GetTracksForHome() to get all Track objects
    /// 2. Sees that 'author' is requested for each track
    /// 3. Calls this method for each track (e.g., 10 times)
    /// 4. DataLoader collects all author IDs: ["cat-1", "cat-2", "cat-1", "cat-3"]
    /// 5. Deduplicates: ["cat-1", "cat-2", "cat-3"]
    /// 6. Batches into ONE call: LoadBatchAsync(["cat-1", "cat-2", "cat-3"])
    /// 7. Fetches all 3 authors in PARALLEL
    /// 8. Caches results for the request duration
    /// 9. Returns cached "cat-1" for the duplicate request
    ///
    /// Performance Improvement:
    /// - WITHOUT DataLoader: 10 sequential HTTP requests (one per track)
    /// - WITH DataLoader: 3 parallel HTTP requests (one per unique author)
    /// - Result: 70% reduction in requests + parallel execution!
    ///
    /// The beauty: No changes needed in the GraphQL query! The optimization
    /// is transparent to the client.
    /// </remarks>
    [GraphQLName("author")]
    public async Task<Author?> GetAuthor(
        [Parent] Track track,
        AuthorDataLoader authorDataLoader)
    {
        // LoadAsync queues the author ID and returns a Task<Author?>
        // Hot Chocolate batches all queued IDs and calls LoadBatchAsync
        return await authorDataLoader.LoadAsync(track.AuthorId);
    }

    /// <summary>
    /// Field resolver for the 'modules' field on the Track type.
    /// This is called by Hot Chocolate when a query requests track.modules.
    /// NOW OPTIMIZED: Uses DataLoader to batch and cache module requests!
    /// </summary>
    /// <param name="track">The parent Track object (provided by Hot Chocolate)</param>
    /// <param name="moduleDataLoader">Injected DataLoader for batching module fetches</param>
    /// <returns>A list of modules for the track. Returns empty list if none found.</returns>
    /// <remarks>
    /// Similar to the author resolver, this uses DataLoader for optimization.
    /// When a query requests multiple tracks with modules:
    ///   query {
    ///     tracksForHome {
    ///       title
    ///       modules {  ← GetModules() called for each track
    ///         id
    ///         title
    ///       }
    ///     }
    ///   }
    ///
    /// Hot Chocolate with DataLoader:
    /// 1. Calls GetTracksForHome() to get all Track objects
    /// 2. Calls this method for each track (e.g., 10 times)
    /// 3. DataLoader collects all track IDs: ["c_0", "c_1", "c_2", ...]
    /// 4. Batches into ONE call: LoadBatchAsync(["c_0", "c_1", ...])
    /// 5. Fetches all module lists in PARALLEL
    /// 6. Caches results for the request duration
    ///
    /// Performance Improvement:
    /// - WITHOUT DataLoader: 10 sequential HTTP requests
    /// - WITH DataLoader: 10 parallel HTTP requests (much faster!)
    /// - Bonus: Caching prevents duplicate requests
    ///
    /// Note: Unlike authors (one-to-one), modules are one-to-many.
    /// Each track has its own list of modules.
    /// </remarks>
    [GraphQLName("modules")]
    public async Task<IReadOnlyList<Module>> GetModules(
        [Parent] Track track,
        ModuleDataLoader moduleDataLoader)
    {
        // LoadAsync queues the track ID and returns a Task<IReadOnlyList<Module>>
        // Hot Chocolate batches all queued IDs and calls LoadBatchAsync
        return await moduleDataLoader.LoadAsync(track.Id);
    }
}
