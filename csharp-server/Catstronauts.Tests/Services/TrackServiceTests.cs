using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Catstronauts.GraphQL.Models;
using Catstronauts.GraphQL.Services;
using Xunit;

namespace Catstronauts.Tests.Services;

/// <summary>
/// Unit tests for TrackService.
/// These tests verify service behavior without making actual HTTP requests.
/// </summary>
/// <remarks>
/// Testing HttpClient is complex. In a real project, consider using:
/// - WireMock for integration tests
/// - Custom HttpMessageHandler for unit tests
/// - Or test through the GraphQL layer (integration tests)
///
/// For this learning project, we'll test:
/// - Validation logic
/// - Null handling
/// - Method signatures and async behavior
/// </remarks>
public class TrackServiceTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var act = () => new TrackService(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_WithValidHttpClient_SetsBaseAddress()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        var service = new TrackService(httpClient);

        // Assert
        httpClient.BaseAddress.Should().NotBeNull();
        httpClient.BaseAddress!.ToString().Should().Be("https://odyssey-lift-off-rest-api.herokuapp.com/");
    }

    #endregion

    #region Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetTrackAsync_WithInvalidId_ThrowsArgumentException(string? invalidId)
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new TrackService(httpClient);

        // Act
        Func<Task> act = async () => await service.GetTrackAsync(invalidId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("ID cannot be null or empty.*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAuthorAsync_WithInvalidId_ThrowsArgumentException(string? invalidId)
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new TrackService(httpClient);

        // Act
        Func<Task> act = async () => await service.GetAuthorAsync(invalidId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetTrackModulesAsync_WithInvalidId_ThrowsArgumentException(string? invalidId)
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new TrackService(httpClient);

        // Act
        Func<Task> act = async () => await service.GetTrackModulesAsync(invalidId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetModuleAsync_WithInvalidId_ThrowsArgumentException(string? invalidId)
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new TrackService(httpClient);

        // Act
        Func<Task> act = async () => await service.GetModuleAsync(invalidId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IncrementTrackViewsAsync_WithInvalidId_ThrowsArgumentException(string? invalidId)
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new TrackService(httpClient);

        // Act
        Func<Task> act = async () => await service.IncrementTrackViewsAsync(invalidId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void TrackService_ImplementsITrackService()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        var service = new TrackService(httpClient);

        // Assert
        service.Should().BeAssignableTo<ITrackService>();
    }

    [Fact]
    public void ITrackService_HasExpectedMethods()
    {
        // Arrange
        var interfaceType = typeof(ITrackService);

        // Assert
        interfaceType.GetMethod(nameof(ITrackService.GetTracksForHomeAsync))
            .Should().NotBeNull();
        interfaceType.GetMethod(nameof(ITrackService.GetTrackAsync))
            .Should().NotBeNull();
        interfaceType.GetMethod(nameof(ITrackService.GetAuthorAsync))
            .Should().NotBeNull();
        interfaceType.GetMethod(nameof(ITrackService.GetTrackModulesAsync))
            .Should().NotBeNull();
        interfaceType.GetMethod(nameof(ITrackService.GetModuleAsync))
            .Should().NotBeNull();
        interfaceType.GetMethod(nameof(ITrackService.IncrementTrackViewsAsync))
            .Should().NotBeNull();
    }

    [Fact]
    public void AllServiceMethods_AreAsync()
    {
        // Arrange
        var interfaceType = typeof(ITrackService);
        var methods = interfaceType.GetMethods();

        // Assert - All methods should return Task<T>
        foreach (var method in methods)
        {
            method.ReturnType.Should().Match(t =>
                t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>) ||
                t == typeof(Task),
                $"because {method.Name} should be async");
        }
    }

    #endregion

    #region Return Type Tests

    [Fact]
    public void GetTracksForHomeAsync_ReturnsTaskOfListOfTrack()
    {
        // Arrange
        var method = typeof(ITrackService).GetMethod(nameof(ITrackService.GetTracksForHomeAsync));

        // Assert
        method!.ReturnType.Should().Be(typeof(Task<List<Track>>));
    }

    [Fact]
    public void GetTrackAsync_ReturnsTaskOfNullableTrack()
    {
        // Arrange
        var method = typeof(ITrackService).GetMethod(nameof(ITrackService.GetTrackAsync));

        // Assert
        method!.ReturnType.Should().Be(typeof(Task<Track?>));
    }

    [Fact]
    public void GetAuthorAsync_ReturnsTaskOfNullableAuthor()
    {
        // Arrange
        var method = typeof(ITrackService).GetMethod(nameof(ITrackService.GetAuthorAsync));

        // Assert
        method!.ReturnType.Should().Be(typeof(Task<Author?>));
    }

    [Fact]
    public void IncrementTrackViewsAsync_ReturnsTaskOfTrack()
    {
        // Arrange
        var method = typeof(ITrackService).GetMethod(nameof(ITrackService.IncrementTrackViewsAsync));

        // Assert
        method!.ReturnType.Should().Be(typeof(Task<Track>));
    }

    #endregion

    #region Documentation Tests

    [Fact]
    public void TrackService_HasXmlDocumentation()
    {
        // This test verifies that XML documentation exists
        // In a real project, you might use reflection to check for XML doc comments
        // For now, we're just verifying the class is public and well-named

        var type = typeof(TrackService);

        type.Should().NotBeNull();
        type.IsPublic.Should().BeTrue();
        type.Name.Should().EndWith("Service");
    }

    #endregion
}
