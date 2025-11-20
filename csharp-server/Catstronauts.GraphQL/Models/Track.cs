namespace Catstronauts.GraphQL.Models;

/// <summary>
/// Represents a learning track (course) containing multiple modules.
/// A track is a collection of educational content about a specific topic.
/// </summary>
public class Track
{
    /// <summary>
    /// Unique identifier for the track.
    /// </summary>
    /// <example>c_0, c_1, t_01</example>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The track's title.
    /// </summary>
    /// <example>Catstronauts: Full Stack GraphQL Tutorial</example>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the track's main author.
    /// Used to fetch the Author object via a field resolver.
    /// </summary>
    /// <remarks>
    /// This is a foreign key-like relationship. The actual Author object
    /// is resolved separately through GraphQL field resolvers, not stored here.
    /// </remarks>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>
    /// URL to the track's thumbnail image for display in cards and detail pages.
    /// </summary>
    /// <example>https://res.cloudinary.com/dety84pbu/image/upload/v1606816219/kitty-veyron-sm_mctf3c.jpg</example>
    public string? Thumbnail { get; set; }

    /// <summary>
    /// The track's approximate length to complete, in minutes.
    /// </summary>
    /// <example>120</example>
    public int? Length { get; set; }

    /// <summary>
    /// The number of modules contained in this track.
    /// </summary>
    /// <example>12</example>
    public int? ModulesCount { get; set; }

    /// <summary>
    /// The track's complete description. May contain markdown formatting.
    /// </summary>
    /// <example>Learn how to build a full-stack GraphQL application...</example>
    public string? Description { get; set; }

    /// <summary>
    /// The number of times this track has been viewed by users.
    /// Incremented via the incrementTrackViews mutation.
    /// </summary>
    /// <example>1337</example>
    public int? NumberOfViews { get; set; }
}
