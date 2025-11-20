using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Catstronauts.GraphQL.GraphQL.Mutations;
using Catstronauts.GraphQL.Models;
using Catstronauts.GraphQL.Services;
using System.Net;
using Xunit;

namespace Catstronauts.Tests.GraphQL.Mutations;

/// <summary>
/// Unit tests for the GraphQL Mutation resolver class.
/// These tests verify that mutation resolvers correctly handle data modifications,
/// validate input, handle errors gracefully, and return appropriate responses.
/// </summary>
/// <remarks>
/// Mutation Testing Philosophy:
/// - Mutations should return structured responses (not throw exceptions)
/// - Test success scenarios (200 OK)
/// - Test validation failures (400 Bad Request)
/// - Test API failures (4xx, 5xx from upstream)
/// - Test unexpected errors (500 Internal Server Error)
/// - Verify response structure (code, success, message, data)
/// </remarks>
public class MutationTests
{
    #region IncrementTrackViews - Success Scenarios

    [Fact]
    public async Task IncrementTrackViews_WithValidId_ReturnsSuccessResponse()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var updatedTrack = new Track
        {
            Id = "c_0",
            Title = "Catstronauts",
            AuthorId = "cat-1",
            NumberOfViews = 101  // Incremented from 100
        };
        mockService.IncrementTrackViewsAsync("c_0").Returns(updatedTrack);
        var mutation = new Mutation();

        // Act
        var response = await mutation.IncrementTrackViews("c_0", mockService);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Code.Should().Be(200);
        response.Message.Should().Contain("Successfully incremented views for track c_0");
        response.Track.Should().NotBeNull();
        response.Track!.Id.Should().Be("c_0");
        response.Track.NumberOfViews.Should().Be(101);
        await mockService.Received(1).IncrementTrackViewsAsync("c_0");
    }

    [Fact]
    public async Task IncrementTrackViews_Success_ReturnsTrackWithAllProperties()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var updatedTrack = new Track
        {
            Id = "c_0",
            Title = "Catstronauts",
            AuthorId = "cat-1",
            Description = "Learn GraphQL",
            Thumbnail = "https://example.com/image.jpg",
            Length = 120,
            ModulesCount = 6,
            NumberOfViews = 1338  // Incremented
        };
        mockService.IncrementTrackViewsAsync("c_0").Returns(updatedTrack);
        var mutation = new Mutation();

        // Act
        var response = await mutation.IncrementTrackViews("c_0", mockService);

        // Assert
        response.Track.Should().BeEquivalentTo(updatedTrack);
    }

    [Fact]
    public async Task IncrementTrackViews_WithDifferentIds_CallsServiceWithCorrectId()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        mockService.IncrementTrackViewsAsync(Arg.Any<string>())
            .Returns(new Track { Id = "test_id", NumberOfViews = 1 });
        var mutation = new Mutation();

        // Act
        await mutation.IncrementTrackViews("specific_track_123", mockService);

        // Assert
        await mockService.Received(1).IncrementTrackViewsAsync("specific_track_123");
    }

    #endregion

    #region IncrementTrackViews - Validation Errors (400)

    [Fact]
    public async Task IncrementTrackViews_WithNullId_Returns400Error()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var mutation = new Mutation();

        // Act
        var response = await mutation.IncrementTrackViews(null!, mockService);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeFalse();
        response.Code.Should().Be(400);
        response.Message.Should().Contain("Track ID cannot be null or empty");
        response.Track.Should().BeNull();
        // Service should NOT be called for invalid input
        await mockService.DidNotReceive().IncrementTrackViewsAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task IncrementTrackViews_WithEmptyId_Returns400Error()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var mutation = new Mutation();

        // Act
        var response = await mutation.IncrementTrackViews(string.Empty, mockService);

        // Assert
        response.Success.Should().BeFalse();
        response.Code.Should().Be(400);
        response.Message.Should().Contain("Track ID cannot be null or empty");
        response.Track.Should().BeNull();
        await mockService.DidNotReceive().IncrementTrackViewsAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task IncrementTrackViews_WithWhitespaceId_Returns400Error()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var mutation = new Mutation();

        // Act
        var response = await mutation.IncrementTrackViews("   ", mockService);

        // Assert
        response.Success.Should().BeFalse();
        response.Code.Should().Be(400);
        response.Message.Should().Contain("Track ID cannot be null or empty");
        response.Track.Should().BeNull();
        await mockService.DidNotReceive().IncrementTrackViewsAsync(Arg.Any<string>());
    }

    #endregion

    #region IncrementTrackViews - HTTP Errors (4xx/5xx)

    [Fact]
    public async Task IncrementTrackViews_WhenApiReturns404_Returns404Error()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var httpException = new HttpRequestException(
            "Track not found",
            null,
            HttpStatusCode.NotFound);
        mockService.IncrementTrackViewsAsync("nonexistent")
            .ThrowsAsync(httpException);
        var mutation = new Mutation();

        // Act
        var response = await mutation.IncrementTrackViews("nonexistent", mockService);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeFalse();
        response.Code.Should().Be(404);
        response.Message.Should().Contain("Failed to increment views");
        response.Message.Should().Contain("Track not found");
        response.Track.Should().BeNull();
    }

    [Fact]
    public async Task IncrementTrackViews_WhenApiReturns500_Returns500Error()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var httpException = new HttpRequestException(
            "Internal server error",
            null,
            HttpStatusCode.InternalServerError);
        mockService.IncrementTrackViewsAsync("c_0")
            .ThrowsAsync(httpException);
        var mutation = new Mutation();

        // Act
        var response = await mutation.IncrementTrackViews("c_0", mockService);

        // Assert
        response.Success.Should().BeFalse();
        response.Code.Should().Be(500);
        response.Message.Should().Contain("Failed to increment views");
        response.Track.Should().BeNull();
    }

    [Fact]
    public async Task IncrementTrackViews_WhenHttpExceptionHasNoStatusCode_Returns500()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        // HttpRequestException without status code
        var httpException = new HttpRequestException("Network error");
        mockService.IncrementTrackViewsAsync("c_0")
            .ThrowsAsync(httpException);
        var mutation = new Mutation();

        // Act
        var response = await mutation.IncrementTrackViews("c_0", mockService);

        // Assert
        response.Success.Should().BeFalse();
        response.Code.Should().Be(500);  // Default to 500 when no status code
        response.Message.Should().Contain("Failed to increment views");
        response.Message.Should().Contain("Network error");
        response.Track.Should().BeNull();
    }

    [Fact]
    public async Task IncrementTrackViews_WhenApiReturns503_Returns503Error()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        var httpException = new HttpRequestException(
            "Service unavailable",
            null,
            HttpStatusCode.ServiceUnavailable);
        mockService.IncrementTrackViewsAsync("c_0")
            .ThrowsAsync(httpException);
        var mutation = new Mutation();

        // Act
        var response = await mutation.IncrementTrackViews("c_0", mockService);

        // Assert
        response.Success.Should().BeFalse();
        response.Code.Should().Be(503);
        response.Message.Should().Contain("Service unavailable");
        response.Track.Should().BeNull();
    }

    #endregion

    #region IncrementTrackViews - Unexpected Errors

    [Fact]
    public async Task IncrementTrackViews_WhenUnexpectedExceptionOccurs_Returns500Error()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        mockService.IncrementTrackViewsAsync("c_0")
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));
        var mutation = new Mutation();

        // Act
        var response = await mutation.IncrementTrackViews("c_0", mockService);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeFalse();
        response.Code.Should().Be(500);
        response.Message.Should().Contain("An unexpected error occurred");
        response.Message.Should().Contain("Unexpected error");
        response.Track.Should().BeNull();
    }

    [Fact]
    public async Task IncrementTrackViews_WhenNullReferenceExceptionOccurs_Returns500Error()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        mockService.IncrementTrackViewsAsync("c_0")
            .ThrowsAsync(new NullReferenceException("Object reference not set"));
        var mutation = new Mutation();

        // Act
        var response = await mutation.IncrementTrackViews("c_0", mockService);

        // Assert
        response.Success.Should().BeFalse();
        response.Code.Should().Be(500);
        response.Message.Should().Contain("An unexpected error occurred");
        response.Track.Should().BeNull();
    }

    #endregion

    #region Response Structure Verification

    [Fact]
    public async Task IncrementTrackViews_SuccessResponse_HasAllRequiredFields()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        mockService.IncrementTrackViewsAsync("c_0")
            .Returns(new Track { Id = "c_0", NumberOfViews = 100 });
        var mutation = new Mutation();

        // Act
        var response = await mutation.IncrementTrackViews("c_0", mockService);

        // Assert - Verify response implements the expected pattern
        response.Should().NotBeNull();
        response.Code.Should().BeGreaterThan(0);  // Has HTTP status code
        response.Success.Should().NotBe(default);  // Has boolean success flag
        response.Message.Should().NotBeNullOrEmpty();  // Has descriptive message
        response.Track.Should().NotBeNull();  // Has data payload
    }

    [Fact]
    public async Task IncrementTrackViews_ErrorResponse_HasAllRequiredFields()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        mockService.IncrementTrackViewsAsync("c_0")
            .ThrowsAsync(new HttpRequestException("API error", null, HttpStatusCode.BadGateway));
        var mutation = new Mutation();

        // Act
        var response = await mutation.IncrementTrackViews("c_0", mockService);

        // Assert - Error responses also have complete structure
        response.Should().NotBeNull();
        response.Code.Should().Be(502);  // Has error code
        response.Success.Should().BeFalse();  // Indicates failure
        response.Message.Should().NotBeNullOrEmpty();  // Has error message
        response.Track.Should().BeNull();  // No data on error
    }

    #endregion

    #region Mutation Class Verification

    [Fact]
    public void IncrementTrackViews_IsAsync()
    {
        // Arrange
        var method = typeof(Mutation).GetMethod(nameof(Mutation.IncrementTrackViews));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IncrementTrackViewsResponse>));
    }

    [Fact]
    public void IncrementTrackViews_HasRequiredParameters()
    {
        // Arrange
        var method = typeof(Mutation).GetMethod(nameof(Mutation.IncrementTrackViews));

        // Assert
        method.Should().NotBeNull();
        var parameters = method!.GetParameters();
        parameters.Should().HaveCount(2);
        parameters[0].Name.Should().Be("id");
        parameters[0].ParameterType.Should().Be(typeof(string));
        parameters[1].Name.Should().Be("trackService");
        parameters[1].ParameterType.Should().Be(typeof(ITrackService));
    }

    [Fact]
    public void Mutation_AllMethods_AreAsync()
    {
        // Arrange
        var mutationType = typeof(Mutation);
        var methods = mutationType.GetMethods()
            .Where(m => m.DeclaringType == mutationType);

        // Assert - All mutation methods should return Task<T>
        foreach (var method in methods)
        {
            method.ReturnType.Should().Match(t =>
                t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>) ||
                t == typeof(Task),
                $"because {method.Name} should be async");
        }
    }

    #endregion

    #region Edge Cases and Business Logic

    [Fact]
    public async Task IncrementTrackViews_WithVeryLongId_StillProcesses()
    {
        // Arrange - Some APIs might have long IDs
        var mockService = Substitute.For<ITrackService>();
        var longId = new string('a', 1000);
        mockService.IncrementTrackViewsAsync(longId)
            .Returns(new Track { Id = longId, NumberOfViews = 1 });
        var mutation = new Mutation();

        // Act
        var response = await mutation.IncrementTrackViews(longId, mockService);

        // Assert
        response.Success.Should().BeTrue();
        response.Code.Should().Be(200);
        await mockService.Received(1).IncrementTrackViewsAsync(longId);
    }

    [Fact]
    public async Task IncrementTrackViews_SuccessMessage_IncludesTrackId()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        mockService.IncrementTrackViewsAsync("track_12345")
            .Returns(new Track { Id = "track_12345", NumberOfViews = 50 });
        var mutation = new Mutation();

        // Act
        var response = await mutation.IncrementTrackViews("track_12345", mockService);

        // Assert - Message should be descriptive and include the ID
        response.Message.Should().Contain("track_12345");
        response.Message.Should().Contain("Successfully");
    }

    [Fact]
    public async Task IncrementTrackViews_ErrorMessage_IncludesOriginalErrorDetails()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        mockService.IncrementTrackViewsAsync("c_0")
            .ThrowsAsync(new HttpRequestException(
                "Database connection timeout after 30 seconds",
                null,
                HttpStatusCode.GatewayTimeout));
        var mutation = new Mutation();

        // Act
        var response = await mutation.IncrementTrackViews("c_0", mockService);

        // Assert - Error message should include original error details
        response.Message.Should().Contain("Database connection timeout");
        response.Message.Should().Contain("30 seconds");
    }

    #endregion
}
