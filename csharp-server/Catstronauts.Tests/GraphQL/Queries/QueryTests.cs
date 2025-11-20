using FluentAssertions;
using NSubstitute;
using Catstronauts.GraphQL.GraphQL.Queries;
using Catstronauts.GraphQL.Models;
using Catstronauts.GraphQL.Services;
using Xunit;

namespace Catstronauts.Tests.GraphQL.Queries;

/// <summary>
/// Unit tests for the GraphQL Query resolver class.
/// These tests verify that query resolvers correctly delegate to the service layer
/// and handle various scenarios (success, null, errors).
/// </summary>
/// <remarks>
/// Testing Philosophy:
/// - Unit tests focus on resolver logic, not actual HTTP calls
/// - We mock ITrackService to isolate the resolver behavior
/// - Tests verify correct service method calls and return value handling
/// - Integration tests (Stage 7) will test the full GraphQL execution pipeline
/// </remarks>
public class QueryTests
{
    #region TracksForHome Query Tests

    [Fact]
    public async Task GetTracksForHome_WithAvailableTracks_ReturnsListOfTracks()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var expectedTracks = new List<Track>
        {
            new Track
            {
                Id = "c_0",
                Title = "Catstronauts",
                AuthorId = "cat-1",
                Thumbnail = "https://example.com/image.jpg",
                Length = 120,
                ModulesCount = 6
            },
            new Track
            {
                Id = "c_1",
                Title = "Advanced GraphQL",
                AuthorId = "cat-2",
                Thumbnail = "https://example.com/image2.jpg",
                Length = 180,
                ModulesCount = 8
            }
        };
        mockService.GetTracksForHomeAsync().Returns(expectedTracks);
        var query = new Query();

        // Act
        var result = await query.GetTracksForHome(mockService);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedTracks);
        await mockService.Received(1).GetTracksForHomeAsync();
    }

    [Fact]
    public async Task GetTracksForHome_WhenNoTracksAvailable_ReturnsEmptyList()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        mockService.GetTracksForHomeAsync().Returns(new List<Track>());
        var query = new Query();

        // Act
        var result = await query.GetTracksForHome(mockService);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        await mockService.Received(1).GetTracksForHomeAsync();
    }

    [Fact]
    public async Task GetTracksForHome_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        mockService.GetTracksForHomeAsync()
            .ThrowsAsync(new HttpRequestException("API unavailable"));
        var query = new Query();

        // Act
        Func<Task> act = async () => await query.GetTracksForHome(mockService);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("API unavailable");
    }

    #endregion

    #region Track Query Tests

    [Fact]
    public async Task GetTrack_WithValidId_ReturnsTrack()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var expectedTrack = new Track
        {
            Id = "c_0",
            Title = "Catstronauts",
            AuthorId = "cat-1",
            Description = "Learn GraphQL with cats!",
            Thumbnail = "https://example.com/image.jpg",
            Length = 120,
            ModulesCount = 6,
            NumberOfViews = 1337
        };
        mockService.GetTrackAsync("c_0").Returns(expectedTrack);
        var query = new Query();

        // Act
        var result = await query.GetTrack("c_0", mockService);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedTrack);
        await mockService.Received(1).GetTrackAsync("c_0");
    }

    [Fact]
    public async Task GetTrack_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        mockService.GetTrackAsync("nonexistent").Returns((Track?)null);
        var query = new Query();

        // Act
        var result = await query.GetTrack("nonexistent", mockService);

        // Assert
        result.Should().BeNull();
        await mockService.Received(1).GetTrackAsync("nonexistent");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetTrack_WithInvalidId_ThrowsArgumentException(string? invalidId)
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        mockService.GetTrackAsync(Arg.Any<string>())
            .ThrowsAsync(new ArgumentException("ID cannot be null or empty."));
        var query = new Query();

        // Act
        Func<Task> act = async () => await query.GetTrack(invalidId!, mockService);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetTrack_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        mockService.GetTrackAsync("c_0")
            .ThrowsAsync(new HttpRequestException("API unavailable"));
        var query = new Query();

        // Act
        Func<Task> act = async () => await query.GetTrack("c_0", mockService);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("API unavailable");
    }

    #endregion

    #region Author Field Resolver Tests

    [Fact]
    public async Task GetAuthor_WithValidTrack_ReturnsAuthor()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var track = new Track
        {
            Id = "c_0",
            Title = "Catstronauts",
            AuthorId = "cat-1"
        };
        var expectedAuthor = new Author
        {
            Id = "cat-1",
            Name = "Henri the Cat",
            Photo = "https://example.com/henri.jpg"
        };
        mockService.GetAuthorAsync("cat-1").Returns(expectedAuthor);
        var query = new Query();

        // Act
        var result = await query.GetAuthor(track, mockService);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedAuthor);
        await mockService.Received(1).GetAuthorAsync("cat-1");
    }

    [Fact]
    public async Task GetAuthor_WhenAuthorNotFound_ReturnsNull()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var track = new Track
        {
            Id = "c_0",
            Title = "Catstronauts",
            AuthorId = "nonexistent"
        };
        mockService.GetAuthorAsync("nonexistent").Returns((Author?)null);
        var query = new Query();

        // Act
        var result = await query.GetAuthor(track, mockService);

        // Assert
        result.Should().BeNull();
        await mockService.Received(1).GetAuthorAsync("nonexistent");
    }

    [Fact]
    public async Task GetAuthor_UsesTrackAuthorId_ToFetchAuthor()
    {
        // Arrange - This test verifies the field resolver pattern
        var mockService = Substitute.For<ITrackService>();
        var track = new Track
        {
            Id = "c_0",
            Title = "Catstronauts",
            AuthorId = "specific-author-id"
        };
        mockService.GetAuthorAsync("specific-author-id")
            .Returns(new Author { Id = "specific-author-id", Name = "Test Author" });
        var query = new Query();

        // Act
        await query.GetAuthor(track, mockService);

        // Assert - Verify the correct AuthorId was used
        await mockService.Received(1).GetAuthorAsync("specific-author-id");
    }

    #endregion

    #region Modules Field Resolver Tests

    [Fact]
    public async Task GetModules_WithValidTrack_ReturnsModules()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var track = new Track
        {
            Id = "c_0",
            Title = "Catstronauts",
            AuthorId = "cat-1"
        };
        var expectedModules = new List<Module>
        {
            new Module
            {
                Id = "l_0",
                Title = "Introduction to GraphQL",
                Length = 20,
                Content = "Learn the basics...",
                VideoUrl = "https://example.com/video1.mp4"
            },
            new Module
            {
                Id = "l_1",
                Title = "Queries and Mutations",
                Length = 30,
                Content = "Deep dive into...",
                VideoUrl = "https://example.com/video2.mp4"
            }
        };
        mockService.GetTrackModulesAsync("c_0").Returns(expectedModules);
        var query = new Query();

        // Act
        var result = await query.GetModules(track, mockService);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedModules);
        await mockService.Received(1).GetTrackModulesAsync("c_0");
    }

    [Fact]
    public async Task GetModules_WhenNoModulesAvailable_ReturnsEmptyList()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var track = new Track
        {
            Id = "c_0",
            Title = "Catstronauts",
            AuthorId = "cat-1"
        };
        mockService.GetTrackModulesAsync("c_0").Returns(new List<Module>());
        var query = new Query();

        // Act
        var result = await query.GetModules(track, mockService);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        await mockService.Received(1).GetTrackModulesAsync("c_0");
    }

    [Fact]
    public async Task GetModules_UsesTrackId_ToFetchModules()
    {
        // Arrange - This test verifies the field resolver pattern
        var mockService = Substitute.For<ITrackService>();
        var track = new Track
        {
            Id = "specific-track-id",
            Title = "Test Track",
            AuthorId = "cat-1"
        };
        mockService.GetTrackModulesAsync("specific-track-id")
            .Returns(new List<Module>());
        var query = new Query();

        // Act
        await query.GetModules(track, mockService);

        // Assert - Verify the correct Track ID was used
        await mockService.Received(1).GetTrackModulesAsync("specific-track-id");
    }

    #endregion

    #region Query Class Verification Tests

    [Fact]
    public void Query_AllMethods_AreAsync()
    {
        // Arrange
        var queryType = typeof(Query);
        var methods = queryType.GetMethods()
            .Where(m => m.DeclaringType == queryType); // Only methods defined in Query

        // Assert - All resolver methods should return Task<T>
        foreach (var method in methods)
        {
            method.ReturnType.Should().Match(t =>
                t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>) ||
                t == typeof(Task),
                $"because {method.Name} should be async");
        }
    }

    [Fact]
    public void GetTracksForHome_ReturnsTaskOfListOfTrack()
    {
        // Arrange
        var method = typeof(Query).GetMethod(nameof(Query.GetTracksForHome));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<List<Track>>));
    }

    [Fact]
    public void GetTrack_ReturnsTaskOfNullableTrack()
    {
        // Arrange
        var method = typeof(Query).GetMethod(nameof(Query.GetTrack));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<Track?>));
    }

    [Fact]
    public void GetAuthor_ReturnsTaskOfNullableAuthor()
    {
        // Arrange
        var method = typeof(Query).GetMethod(nameof(Query.GetAuthor));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<Author?>));
    }

    [Fact]
    public void GetModules_ReturnsTaskOfListOfModule()
    {
        // Arrange
        var method = typeof(Query).GetMethod(nameof(Query.GetModules));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<List<Module>>));
    }

    #endregion

    #region Service Injection Tests

    [Fact]
    public async Task GetTracksForHome_ServiceParameter_IsRequired()
    {
        // This test verifies that the method signature requires the service
        var query = new Query();
        var mockService = Substitute.For<ITrackService>();
        mockService.GetTracksForHomeAsync().Returns(new List<Track>());

        // Should compile and work - service is injected
        var result = await query.GetTracksForHome(mockService);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTrack_ServiceParameter_IsRequired()
    {
        var query = new Query();
        var mockService = Substitute.For<ITrackService>();
        mockService.GetTrackAsync("c_0").Returns(new Track { Id = "c_0" });

        var result = await query.GetTrack("c_0", mockService);

        result.Should().NotBeNull();
    }

    #endregion
}
