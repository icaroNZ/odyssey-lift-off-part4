using FluentAssertions;
using NSubstitute;
using Catstronauts.GraphQL.GraphQL.DataLoaders;
using Catstronauts.GraphQL.Models;
using Catstronauts.GraphQL.Services;
using GreenDonut;
using Xunit;

namespace Catstronauts.Tests.GraphQL.DataLoaders;

/// <summary>
/// Unit tests for DataLoader implementations.
/// These tests verify that DataLoaders correctly batch requests, handle errors,
/// and return complete dictionaries as required by Hot Chocolate.
/// </summary>
/// <remarks>
/// DataLoader Testing Philosophy:
/// - Test LoadBatchAsync directly (internal batching logic)
/// - Verify all input keys are present in output dictionary
/// - Test error handling (individual failures shouldn't crash the batch)
/// - Test with single and multiple keys
/// - Verify null handling for missing data
///
/// Note: We're testing the LoadBatchAsync method directly, not the full
/// DataLoader pipeline. Integration tests (Stage 7) will test the complete
/// batching and caching behavior in a real GraphQL request.
/// </remarks>
public class DataLoaderTests
{
    #region AuthorDataLoader Tests

    [Fact]
    public async Task AuthorDataLoader_WithSingleKey_ReturnsAuthor()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var expectedAuthor = new Author
        {
            Id = "cat-1",
            Name = "Henri the Cat",
            Photo = "https://example.com/henri.jpg"
        };
        mockService.GetAuthorAsync("cat-1").Returns(expectedAuthor);

        var dataLoader = new AuthorDataLoader(
            mockService,
            AutoBatchScheduler.Default);

        // Act
        var result = await dataLoader.LoadAsync("cat-1");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedAuthor);
        await mockService.Received(1).GetAuthorAsync("cat-1");
    }

    [Fact]
    public async Task AuthorDataLoader_WithMultipleKeys_ReturnsAllAuthors()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var author1 = new Author { Id = "cat-1", Name = "Henri" };
        var author2 = new Author { Id = "cat-2", Name = "Luna" };
        var author3 = new Author { Id = "cat-3", Name = "Milo" };

        mockService.GetAuthorAsync("cat-1").Returns(author1);
        mockService.GetAuthorAsync("cat-2").Returns(author2);
        mockService.GetAuthorAsync("cat-3").Returns(author3);

        var dataLoader = new AuthorDataLoader(
            mockService,
            AutoBatchScheduler.Default);

        // Act - Request multiple authors
        var task1 = dataLoader.LoadAsync("cat-1");
        var task2 = dataLoader.LoadAsync("cat-2");
        var task3 = dataLoader.LoadAsync("cat-3");
        var results = await Task.WhenAll(task1, task2, task3);

        // Assert
        results.Should().HaveCount(3);
        results[0].Should().BeEquivalentTo(author1);
        results[1].Should().BeEquivalentTo(author2);
        results[2].Should().BeEquivalentTo(author3);

        // Verify all three were fetched (in parallel, via LoadBatchAsync)
        await mockService.Received(1).GetAuthorAsync("cat-1");
        await mockService.Received(1).GetAuthorAsync("cat-2");
        await mockService.Received(1).GetAuthorAsync("cat-3");
    }

    [Fact]
    public async Task AuthorDataLoader_WithNonExistentAuthor_ReturnsNull()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        mockService.GetAuthorAsync("nonexistent").Returns((Author?)null);

        var dataLoader = new AuthorDataLoader(
            mockService,
            AutoBatchScheduler.Default);

        // Act
        var result = await dataLoader.LoadAsync("nonexistent");

        // Assert
        result.Should().BeNull();
        await mockService.Received(1).GetAuthorAsync("nonexistent");
    }

    [Fact]
    public async Task AuthorDataLoader_WithDuplicateKeys_CachesAndReturnsOnce()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var author = new Author { Id = "cat-1", Name = "Henri" };
        mockService.GetAuthorAsync("cat-1").Returns(author);

        var dataLoader = new AuthorDataLoader(
            mockService,
            AutoBatchScheduler.Default);

        // Act - Request same author multiple times
        var task1 = dataLoader.LoadAsync("cat-1");
        var task2 = dataLoader.LoadAsync("cat-1");
        var task3 = dataLoader.LoadAsync("cat-1");
        var results = await Task.WhenAll(task1, task2, task3);

        // Assert - All three should return the same author
        results.Should().HaveCount(3);
        results.Should().AllBeEquivalentTo(author);

        // CRITICAL: Service should be called ONCE due to deduplication
        await mockService.Received(1).GetAuthorAsync("cat-1");
    }

    [Fact]
    public async Task AuthorDataLoader_WhenServiceThrows_HandlesGracefully()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        mockService.GetAuthorAsync("cat-error")
            .ThrowsAsync(new HttpRequestException("API unavailable"));
        mockService.GetAuthorAsync("cat-ok")
            .Returns(new Author { Id = "cat-ok", Name = "Working Author" });

        var dataLoader = new AuthorDataLoader(
            mockService,
            AutoBatchScheduler.Default);

        // Act - Request both failing and successful author
        var task1 = dataLoader.LoadAsync("cat-error");
        var task2 = dataLoader.LoadAsync("cat-ok");
        var results = await Task.WhenAll(task1, task2);

        // Assert - Error author returns null, successful author works
        results[0].Should().BeNull();  // Error handled gracefully
        results[1].Should().NotBeNull();
        results[1]!.Name.Should().Be("Working Author");
    }

    [Fact]
    public void AuthorDataLoader_Constructor_ThrowsOnNullService()
    {
        // Arrange & Act
        Action act = () => new AuthorDataLoader(
            null!,
            AutoBatchScheduler.Default);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("trackService");
    }

    #endregion

    #region ModuleDataLoader Tests

    [Fact]
    public async Task ModuleDataLoader_WithSingleKey_ReturnsModules()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var expectedModules = new List<Module>
        {
            new Module { Id = "l_0", Title = "Introduction", Length = 20 },
            new Module { Id = "l_1", Title = "Advanced Topics", Length = 30 }
        };
        mockService.GetTrackModulesAsync("c_0").Returns(expectedModules);

        var dataLoader = new ModuleDataLoader(
            mockService,
            AutoBatchScheduler.Default);

        // Act
        var result = await dataLoader.LoadAsync("c_0");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedModules);
        await mockService.Received(1).GetTrackModulesAsync("c_0");
    }

    [Fact]
    public async Task ModuleDataLoader_WithMultipleKeys_ReturnsAllModuleLists()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var modules1 = new List<Module>
        {
            new Module { Id = "l_0", Title = "Track 1 Module 1" },
            new Module { Id = "l_1", Title = "Track 1 Module 2" }
        };
        var modules2 = new List<Module>
        {
            new Module { Id = "l_2", Title = "Track 2 Module 1" }
        };
        var modules3 = new List<Module>
        {
            new Module { Id = "l_3", Title = "Track 3 Module 1" },
            new Module { Id = "l_4", Title = "Track 3 Module 2" },
            new Module { Id = "l_5", Title = "Track 3 Module 3" }
        };

        mockService.GetTrackModulesAsync("c_0").Returns(modules1);
        mockService.GetTrackModulesAsync("c_1").Returns(modules2);
        mockService.GetTrackModulesAsync("c_2").Returns(modules3);

        var dataLoader = new ModuleDataLoader(
            mockService,
            AutoBatchScheduler.Default);

        // Act - Request modules for multiple tracks
        var task1 = dataLoader.LoadAsync("c_0");
        var task2 = dataLoader.LoadAsync("c_1");
        var task3 = dataLoader.LoadAsync("c_2");
        var results = await Task.WhenAll(task1, task2, task3);

        // Assert
        results.Should().HaveCount(3);
        results[0].Should().HaveCount(2).And.BeEquivalentTo(modules1);
        results[1].Should().HaveCount(1).And.BeEquivalentTo(modules2);
        results[2].Should().HaveCount(3).And.BeEquivalentTo(modules3);

        // Verify all three were fetched
        await mockService.Received(1).GetTrackModulesAsync("c_0");
        await mockService.Received(1).GetTrackModulesAsync("c_1");
        await mockService.Received(1).GetTrackModulesAsync("c_2");
    }

    [Fact]
    public async Task ModuleDataLoader_WithTrackHavingNoModules_ReturnsEmptyList()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        mockService.GetTrackModulesAsync("empty_track")
            .Returns(new List<Module>());

        var dataLoader = new ModuleDataLoader(
            mockService,
            AutoBatchScheduler.Default);

        // Act
        var result = await dataLoader.LoadAsync("empty_track");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        await mockService.Received(1).GetTrackModulesAsync("empty_track");
    }

    [Fact]
    public async Task ModuleDataLoader_WithDuplicateKeys_CachesAndReturnsOnce()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var modules = new List<Module>
        {
            new Module { Id = "l_0", Title = "Module 1" }
        };
        mockService.GetTrackModulesAsync("c_0").Returns(modules);

        var dataLoader = new ModuleDataLoader(
            mockService,
            AutoBatchScheduler.Default);

        // Act - Request same track's modules multiple times
        var task1 = dataLoader.LoadAsync("c_0");
        var task2 = dataLoader.LoadAsync("c_0");
        var task3 = dataLoader.LoadAsync("c_0");
        var results = await Task.WhenAll(task1, task2, task3);

        // Assert - All three should return the same modules
        results.Should().HaveCount(3);
        results.Should().AllBeEquivalentTo(modules);

        // CRITICAL: Service should be called ONCE due to deduplication
        await mockService.Received(1).GetTrackModulesAsync("c_0");
    }

    [Fact]
    public async Task ModuleDataLoader_WhenServiceThrows_HandlesGracefully()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        mockService.GetTrackModulesAsync("error_track")
            .ThrowsAsync(new HttpRequestException("API unavailable"));
        mockService.GetTrackModulesAsync("ok_track")
            .Returns(new List<Module>
            {
                new Module { Id = "l_0", Title = "Working Module" }
            });

        var dataLoader = new ModuleDataLoader(
            mockService,
            AutoBatchScheduler.Default);

        // Act - Request both failing and successful track
        var task1 = dataLoader.LoadAsync("error_track");
        var task2 = dataLoader.LoadAsync("ok_track");
        var results = await Task.WhenAll(task1, task2);

        // Assert - Error track returns empty list, successful track works
        results[0].Should().NotBeNull().And.BeEmpty();  // Error handled gracefully
        results[1].Should().HaveCount(1);
        results[1][0].Title.Should().Be("Working Module");
    }

    [Fact]
    public void ModuleDataLoader_Constructor_ThrowsOnNullService()
    {
        // Arrange & Act
        Action act = () => new ModuleDataLoader(
            null!,
            AutoBatchScheduler.Default);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("trackService");
    }

    [Fact]
    public async Task ModuleDataLoader_ReturnsImmutableList()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var modules = new List<Module>
        {
            new Module { Id = "l_0", Title = "Module 1" }
        };
        mockService.GetTrackModulesAsync("c_0").Returns(modules);

        var dataLoader = new ModuleDataLoader(
            mockService,
            AutoBatchScheduler.Default);

        // Act
        var result = await dataLoader.LoadAsync("c_0");

        // Assert - Should be IReadOnlyList (immutable)
        result.Should().BeAssignableTo<IReadOnlyList<Module>>();
    }

    #endregion

    #region DataLoader Contract Tests

    [Fact]
    public void AuthorDataLoader_InheritsFromBatchDataLoader()
    {
        // Assert
        typeof(AuthorDataLoader).Should().BeDerivedFrom<BatchDataLoader<string, Author?>>();
    }

    [Fact]
    public void ModuleDataLoader_InheritsFromBatchDataLoader()
    {
        // Assert
        typeof(ModuleDataLoader).Should().BeDerivedFrom<BatchDataLoader<string, IReadOnlyList<Module>>>();
    }

    #endregion

    #region Performance & Batching Behavior Tests

    [Fact]
    public async Task AuthorDataLoader_BatchesMultipleRequestsEfficiently()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        for (int i = 1; i <= 10; i++)
        {
            var authorId = $"cat-{i}";
            mockService.GetAuthorAsync(authorId)
                .Returns(new Author { Id = authorId, Name = $"Author {i}" });
        }

        var dataLoader = new AuthorDataLoader(
            mockService,
            AutoBatchScheduler.Default);

        // Act - Request 10 authors "simultaneously"
        var tasks = Enumerable.Range(1, 10)
            .Select(i => dataLoader.LoadAsync($"cat-{i}"))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().AllSatisfy(author => author.Should().NotBeNull());

        // Each author should be fetched exactly once
        for (int i = 1; i <= 10; i++)
        {
            await mockService.Received(1).GetAuthorAsync($"cat-{i}");
        }
    }

    [Fact]
    public async Task ModuleDataLoader_BatchesMultipleRequestsEfficiently()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        for (int i = 0; i < 10; i++)
        {
            var trackId = $"c_{i}";
            mockService.GetTrackModulesAsync(trackId)
                .Returns(new List<Module>
                {
                    new Module { Id = $"l_{i}", Title = $"Module {i}" }
                });
        }

        var dataLoader = new ModuleDataLoader(
            mockService,
            AutoBatchScheduler.Default);

        // Act - Request modules for 10 tracks "simultaneously"
        var tasks = Enumerable.Range(0, 10)
            .Select(i => dataLoader.LoadAsync($"c_{i}"))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().AllSatisfy(modules => modules.Should().HaveCount(1));

        // Each track's modules should be fetched exactly once
        for (int i = 0; i < 10; i++)
        {
            await mockService.Received(1).GetTrackModulesAsync($"c_{i}");
        }
    }

    #endregion
}
