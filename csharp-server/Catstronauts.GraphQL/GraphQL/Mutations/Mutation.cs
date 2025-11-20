using Catstronauts.GraphQL.Models;
using Catstronauts.GraphQL.Services;

namespace Catstronauts.GraphQL.GraphQL.Mutations;

/// <summary>
/// Root mutation type for the GraphQL API.
/// Contains all operations that modify data (create, update, delete).
/// </summary>
/// <remarks>
/// GraphQL Mutations vs Queries:
/// - Queries: Read-only operations, can run in parallel
/// - Mutations: Write operations, run sequentially in order
///
/// Mutation Best Practices:
/// 1. Return a response type (not just the modified entity)
/// 2. Include status information (code, success, message)
/// 3. Include the modified data for client cache updates
/// 4. Use descriptive names that indicate the action (increment, create, update, delete)
///
/// Example mutation request:
///   mutation IncrementViews {
///     incrementTrackViews(id: "c_0") {
///       code
///       success
///       message
///       track {
///         id
///         numberOfViews
///       }
///     }
///   }
/// </remarks>
public class Mutation
{
    /// <summary>
    /// Increments the view count for a specific track.
    /// This mutation is called when a user views a track's details page.
    /// </summary>
    /// <param name="id">The unique identifier of the track to increment</param>
    /// <param name="trackService">Injected service for track operations</param>
    /// <returns>
    /// A response object containing:
    /// - code: HTTP-style status code (200 for success, 4xx/5xx for errors)
    /// - success: Boolean indicating if the operation succeeded
    /// - message: Human-readable description of the result
    /// - track: The updated track with new view count (null on error)
    /// </returns>
    /// <remarks>
    /// Response Pattern Explained:
    /// GraphQL mutations should return structured responses instead of throwing exceptions.
    /// This approach:
    /// 1. Allows clients to handle errors gracefully in the UI
    /// 2. Provides context about what went wrong (via message)
    /// 3. Follows HTTP status code conventions (200, 404, 500, etc.)
    /// 4. Returns partial data when possible (track on success, null on error)
    ///
    /// The REST API PATCH endpoint is idempotent, meaning calling it multiple
    /// times with the same ID is safe and won't cause duplicate increments.
    ///
    /// Error Handling:
    /// - ArgumentException (null/empty ID): Returns 400 with error message
    /// - HttpRequestException (API failure): Returns status code from exception with error message
    /// - Other exceptions: Returns 500 with generic error message
    /// </remarks>
    public async Task<IncrementTrackViewsResponse> IncrementTrackViews(
        string id,
        [Service] ITrackService trackService)
    {
        // Validate input - return 400 Bad Request for invalid input
        if (string.IsNullOrWhiteSpace(id))
        {
            return new IncrementTrackViewsResponse
            {
                Code = 400,
                Success = false,
                Message = "Track ID cannot be null or empty.",
                Track = null
            };
        }

        try
        {
            // Call service to increment views
            var track = await trackService.IncrementTrackViewsAsync(id);

            // Return success response with updated track
            return new IncrementTrackViewsResponse
            {
                Code = 200,
                Success = true,
                Message = $"Successfully incremented views for track {id}.",
                Track = track
            };
        }
        catch (HttpRequestException ex)
        {
            // Handle HTTP errors from the upstream REST API
            // Extract status code from exception (default to 500 if not available)
            var statusCode = ex.StatusCode.HasValue
                ? (int)ex.StatusCode.Value
                : 500;

            return new IncrementTrackViewsResponse
            {
                Code = statusCode,
                Success = false,
                Message = $"Failed to increment views: {ex.Message}",
                Track = null
            };
        }
        catch (Exception ex)
        {
            // Handle unexpected errors
            return new IncrementTrackViewsResponse
            {
                Code = 500,
                Success = false,
                Message = $"An unexpected error occurred: {ex.Message}",
                Track = null
            };
        }
    }
}
