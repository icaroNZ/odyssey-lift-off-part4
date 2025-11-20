namespace Catstronauts.GraphQL.Models;

/// <summary>
/// Represents a single module (lesson) within a track.
/// Multiple modules compose a complete track (course).
/// </summary>
public class Module
{
    /// <summary>
    /// Unique identifier for the module.
    /// </summary>
    /// <example>l_0, mod_01</example>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The module's title.
    /// </summary>
    /// <example>What is GraphQL?</example>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Module duration in minutes.
    /// </summary>
    /// <example>15</example>
    public int? Length { get; set; }

    /// <summary>
    /// The module's text-based description or transcript.
    /// May contain markdown formatting. For video modules, this contains
    /// the enriched video transcript.
    /// </summary>
    /// <example>In this module, we'll learn about GraphQL fundamentals...</example>
    public string? Content { get; set; }

    /// <summary>
    /// URL to the module's video (for video-based modules).
    /// Null for text-only modules.
    /// </summary>
    /// <example>https://www.youtube.com/watch?v=dQw4w9WgXcQ</example>
    public string? VideoUrl { get; set; }
}
