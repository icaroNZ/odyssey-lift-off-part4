namespace Catstronauts.GraphQL.GraphQL.Queries;

/// <summary>
/// Root query type for the GraphQL API.
/// Contains all top-level query operations.
/// </summary>
public class Query
{
    /// <summary>
    /// Simple "Hello World" query for testing GraphQL setup.
    /// </summary>
    /// <returns>A greeting message</returns>
    public string GetHello() => "Hello, GraphQL from C#!";
}
