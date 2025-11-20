using Catstronauts.GraphQL.Models;
using Catstronauts.GraphQL.Services;

namespace Catstronauts.GraphQL.GraphQL.DataLoaders;

/// <summary>
/// DataLoader for batching and caching Author fetches.
/// Solves the N+1 problem when querying multiple tracks with their authors.
/// </summary>
/// <remarks>
/// The N+1 Problem:
/// Without DataLoader, querying 10 tracks with authors results in:
/// - 1 query for tracks (N=10)
/// - 10 separate queries for authors (one per track)
/// - Total: 11 queries
///
/// With DataLoader:
/// - 1 query for tracks
/// - 1 batched query for all 10 authors
/// - Total: 2 queries (83% reduction!)
///
/// How DataLoaders Work:
/// 1. During a single GraphQL request execution, Hot Chocolate collects all
///    author IDs that need to be fetched
/// 2. Before executing, it groups them and calls LoadBatchAsync once
/// 3. LoadBatchAsync fetches all authors in parallel
/// 4. Results are cached for the duration of the request
/// 5. Subsequent requests for the same author ID return the cached value
///
/// Example:
/// Query requesting 3 tracks with authors:
///   query {
///     tracksForHome {
///       id
///       title
///       author { name }  ← Author field resolver called 3 times
///     }
///   }
///
/// Without DataLoader:
///   - GetAuthor("cat-1") → HTTP request
///   - GetAuthor("cat-2") → HTTP request
///   - GetAuthor("cat-1") → HTTP request (duplicate!)
///   Total: 3 HTTP requests
///
/// With DataLoader:
///   - Collects: ["cat-1", "cat-2", "cat-1"]
///   - Deduplicates: ["cat-1", "cat-2"]
///   - LoadBatchAsync(["cat-1", "cat-2"]) → 2 parallel HTTP requests
///   - Caches results
///   - Returns cached "cat-1" for the duplicate request
///   Total: 2 HTTP requests
///
/// Benefits:
/// - Reduces API calls by batching
/// - Eliminates duplicate requests via caching
/// - Improves performance dramatically with nested queries
/// - Automatic - no changes needed in client queries
/// </remarks>
public class AuthorDataLoader : BatchDataLoader<string, Author?>
{
    private readonly ITrackService _trackService;

    /// <summary>
    /// Constructor for AuthorDataLoader.
    /// </summary>
    /// <param name="trackService">Service for fetching author data</param>
    /// <param name="batchScheduler">
    /// Hot Chocolate's batch scheduler that manages when batches execute.
    /// Injected automatically by the framework.
    /// </param>
    /// <param name="options">
    /// DataLoader options (cache settings, batch size limits, etc.).
    /// Injected automatically by the framework.
    /// </param>
    public AuthorDataLoader(
        ITrackService trackService,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _trackService = trackService ?? throw new ArgumentNullException(nameof(trackService));
    }

    /// <summary>
    /// Loads a batch of authors by their IDs in parallel.
    /// Called automatically by Hot Chocolate when authors need to be fetched.
    /// </summary>
    /// <param name="keys">
    /// The list of author IDs to fetch. Hot Chocolate automatically:
    /// - Collects all author IDs requested during query execution
    /// - Deduplicates them (no duplicate IDs in this list)
    /// - Passes them to this method
    /// </param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>
    /// A dictionary mapping author IDs to Author objects.
    /// - Key: Author ID (from the input keys)
    /// - Value: Author object (or null if not found)
    ///
    /// IMPORTANT: The dictionary MUST contain an entry for every input key,
    /// even if the author wasn't found (use null for missing authors).
    /// </returns>
    /// <remarks>
    /// Implementation Strategy:
    /// 1. Create tasks for fetching each author in parallel
    /// 2. Use Task.WhenAll to execute all fetches concurrently
    /// 3. Build dictionary mapping IDs to results
    /// 4. Handle nulls gracefully (author not found)
    ///
    /// Performance:
    /// - Fetches all authors in parallel (not sequential)
    /// - Maximum parallelism = number of authors
    /// - Limited by HttpClient connection pool size
    ///
    /// Error Handling:
    /// - If one author fetch fails, the entire batch fails
    /// - Hot Chocolate will return an error for the affected fields
    /// - Other fields in the query may still return successfully
    /// </remarks>
    protected override async Task<IReadOnlyDictionary<string, Author?>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        // Create a task for each author ID to fetch them in parallel
        var authorTasks = keys.Select(async authorId =>
        {
            try
            {
                // Fetch the author from the service
                var author = await _trackService.GetAuthorAsync(authorId);
                return new KeyValuePair<string, Author?>(authorId, author);
            }
            catch (Exception)
            {
                // If fetching fails, return null for this author
                // The query will still succeed for other authors
                return new KeyValuePair<string, Author?>(authorId, null);
            }
        });

        // Execute all tasks in parallel and wait for completion
        var results = await Task.WhenAll(authorTasks);

        // Convert results to dictionary
        // Hot Chocolate expects a dictionary with an entry for every input key
        return results.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value);
    }
}
