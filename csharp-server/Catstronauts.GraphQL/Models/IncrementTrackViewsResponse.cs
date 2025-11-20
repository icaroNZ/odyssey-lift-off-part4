namespace Catstronauts.GraphQL.Models;

/// <summary>
/// Response object for the incrementTrackViews mutation.
/// Follows a standard response pattern with status information and optional data.
/// </summary>
/// <remarks>
/// This response pattern is a best practice for GraphQL mutations, providing:
/// - HTTP-style status code for categorizing results
/// - Boolean success flag for quick checking
/// - Human-readable message for UI display
/// - The modified entity (or null on failure)
/// </remarks>
public class IncrementTrackViewsResponse
{
    /// <summary>
    /// HTTP-style status code representing the result of the operation.
    /// </summary>
    /// <example>200 for success, 404 for not found, 500 for server error</example>
    public int Code { get; set; }

    /// <summary>
    /// Indicates whether the mutation completed successfully.
    /// </summary>
    /// <example>true if views were incremented, false otherwise</example>
    public bool Success { get; set; }

    /// <summary>
    /// Human-readable message describing the result.
    /// Useful for displaying feedback to users or debugging.
    /// </summary>
    /// <example>"Successfully incremented number of views for track c_0"</example>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The updated track with new view count.
    /// Null if the operation failed (e.g., track not found).
    /// </summary>
    public Track? Track { get; set; }
}
