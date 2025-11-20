namespace Catstronauts.GraphQL.Models;

/// <summary>
/// Represents an author who creates tracks and modules.
/// </summary>
public class Author
{
    /// <summary>
    /// Unique identifier for the author.
    /// </summary>
    /// <example>cat-1, aut_01</example>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Author's full name (first and last name).
    /// </summary>
    /// <example>Henri, The Cat</example>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL to the author's profile photo.
    /// </summary>
    /// <example>https://res.cloudinary.com/dety84pbu/image/upload/v1606816219/kitty-veyron-sm_mctf3c.jpg</example>
    public string? Photo { get; set; }
}
