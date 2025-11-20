using Catstronauts.GraphQL.Models;
using Catstronauts.GraphQL.Services;

namespace Catstronauts.GraphQL.GraphQL.DataLoaders;

/// <summary>
/// DataLoader for batching and caching Module list fetches by track ID.
/// Solves the N+1 problem when querying multiple tracks with their modules.
/// </summary>
/// <remarks>
/// Similar to AuthorDataLoader, but returns lists of modules instead of single objects.
///
/// The N+1 Problem for Modules:
/// Without DataLoader, querying 10 tracks with modules results in:
/// - 1 query for tracks (N=10)
/// - 10 separate queries for modules (one per track)
/// - Total: 11 queries
///
/// With DataLoader:
/// - 1 query for tracks
/// - 1 batched operation fetching all module lists in parallel
/// - Total: Effectively 2 operations (modules fetched in parallel)
///
/// Example:
/// Query requesting 3 tracks with modules:
///   query {
///     tracksForHome {
///       id
///       title
///       modules { title length }  ← Modules field resolver called 3 times
///     }
///   }
///
/// Without DataLoader:
///   - GetModules("c_0") → HTTP request
///   - GetModules("c_1") → HTTP request
///   - GetModules("c_2") → HTTP request
///   Total: 3 sequential HTTP requests
///
/// With DataLoader:
///   - Collects: ["c_0", "c_1", "c_2"]
///   - LoadBatchAsync(["c_0", "c_1", "c_2"]) → 3 parallel HTTP requests
///   - Caches results
///   Total: 3 parallel HTTP requests (much faster!)
///
/// Key Difference from AuthorDataLoader:
/// - Authors: One-to-one (Track → Author)
/// - Modules: One-to-many (Track → List of Modules)
/// - Both benefit from batching and caching
/// </remarks>
public class ModuleDataLoader : BatchDataLoader<string, IReadOnlyList<Module>>
{
    private readonly ITrackService _trackService;

    /// <summary>
    /// Constructor for ModuleDataLoader.
    /// </summary>
    /// <param name="trackService">Service for fetching module data</param>
    /// <param name="batchScheduler">
    /// Hot Chocolate's batch scheduler that manages when batches execute.
    /// Injected automatically by the framework.
    /// </param>
    /// <param name="options">
    /// DataLoader options (cache settings, batch size limits, etc.).
    /// Injected automatically by the framework.
    /// </param>
    public ModuleDataLoader(
        ITrackService trackService,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _trackService = trackService ?? throw new ArgumentNullException(nameof(trackService));
    }

    /// <summary>
    /// Loads a batch of module lists by track IDs in parallel.
    /// Called automatically by Hot Chocolate when modules need to be fetched.
    /// </summary>
    /// <param name="keys">
    /// The list of track IDs to fetch modules for. Hot Chocolate automatically:
    /// - Collects all track IDs during query execution
    /// - Deduplicates them
    /// - Passes them to this method
    /// </param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>
    /// A dictionary mapping track IDs to lists of modules.
    /// - Key: Track ID (from the input keys)
    /// - Value: List of modules for that track (empty list if none found)
    ///
    /// IMPORTANT: The dictionary MUST contain an entry for every input key.
    /// If a track has no modules, return an empty list (not null).
    /// </returns>
    /// <remarks>
    /// Implementation Strategy:
    /// 1. Create tasks for fetching modules for each track in parallel
    /// 2. Use Task.WhenAll to execute all fetches concurrently
    /// 3. Build dictionary mapping track IDs to module lists
    /// 4. Return empty lists for tracks with no modules
    ///
    /// Performance:
    /// - Fetches all module lists in parallel
    /// - Converts List to IReadOnlyList for immutability
    /// - Much faster than sequential fetches
    /// </remarks>
    protected override async Task<IReadOnlyDictionary<string, IReadOnlyList<Module>>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        // Create a task for each track ID to fetch its modules in parallel
        var moduleTasks = keys.Select(async trackId =>
        {
            try
            {
                // Fetch the modules for this track from the service
                var modules = await _trackService.GetTrackModulesAsync(trackId);

                // Convert List<Module> to IReadOnlyList<Module> for immutability
                IReadOnlyList<Module> readOnlyModules = modules;

                return new KeyValuePair<string, IReadOnlyList<Module>>(trackId, readOnlyModules);
            }
            catch (Exception)
            {
                // If fetching fails, return an empty list for this track
                // The query will still succeed with an empty modules array
                return new KeyValuePair<string, IReadOnlyList<Module>>(
                    trackId,
                    Array.Empty<Module>());
            }
        });

        // Execute all tasks in parallel and wait for completion
        var results = await Task.WhenAll(moduleTasks);

        // Convert results to dictionary
        // Hot Chocolate expects a dictionary with an entry for every input key
        return results.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value);
    }
}
