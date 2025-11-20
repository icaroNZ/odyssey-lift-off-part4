using FluentAssertions;
using Catstronauts.GraphQL.Models;
using Xunit;

namespace Catstronauts.Tests.Models;

/// <summary>
/// Tests for model classes (POCOs).
/// These tests verify that models can be instantiated and properties work correctly.
/// </summary>
public class ModelTests
{
    #region Track Model Tests

    [Fact]
    public void Track_CanBeInstantiated_WithDefaultValues()
    {
        // Arrange & Act
        var track = new Track();

        // Assert
        track.Should().NotBeNull();
        track.Id.Should().BeEmpty();
        track.Title.Should().BeEmpty();
        track.AuthorId.Should().BeEmpty();
        track.Thumbnail.Should().BeNull();
        track.Length.Should().BeNull();
        track.ModulesCount.Should().BeNull();
        track.Description.Should().BeNull();
        track.NumberOfViews.Should().BeNull();
    }

    [Fact]
    public void Track_CanBeInstantiated_WithAllProperties()
    {
        // Arrange & Act
        var track = new Track
        {
            Id = "c_0",
            Title = "Catstronauts",
            AuthorId = "cat-1",
            Thumbnail = "https://example.com/cat.jpg",
            Length = 120,
            ModulesCount = 5,
            Description = "Learn GraphQL with cats!",
            NumberOfViews = 42
        };

        // Assert
        track.Id.Should().Be("c_0");
        track.Title.Should().Be("Catstronauts");
        track.AuthorId.Should().Be("cat-1");
        track.Thumbnail.Should().Be("https://example.com/cat.jpg");
        track.Length.Should().Be(120);
        track.ModulesCount.Should().Be(5);
        track.Description.Should().Be("Learn GraphQL with cats!");
        track.NumberOfViews.Should().Be(42);
    }

    [Fact]
    public void Track_RequiredProperties_AreNotNull()
    {
        // Arrange & Act
        var track = new Track
        {
            Id = "t_01",
            Title = "Test Track",
            AuthorId = "a_01"
        };

        // Assert - Required properties should never be null
        track.Id.Should().NotBeNull();
        track.Title.Should().NotBeNull();
        track.AuthorId.Should().NotBeNull();
    }

    [Fact]
    public void Track_OptionalProperties_CanBeNull()
    {
        // Arrange & Act
        var track = new Track
        {
            Id = "t_01",
            Title = "Minimal Track",
            AuthorId = "a_01",
            // Leave optional properties as null
            Thumbnail = null,
            Length = null,
            ModulesCount = null,
            Description = null,
            NumberOfViews = null
        };

        // Assert - Optional properties can be null
        track.Thumbnail.Should().BeNull();
        track.Length.Should().BeNull();
        track.ModulesCount.Should().BeNull();
        track.Description.Should().BeNull();
        track.NumberOfViews.Should().BeNull();
    }

    #endregion

    #region Author Model Tests

    [Fact]
    public void Author_CanBeInstantiated_WithDefaultValues()
    {
        // Arrange & Act
        var author = new Author();

        // Assert
        author.Should().NotBeNull();
        author.Id.Should().BeEmpty();
        author.Name.Should().BeEmpty();
        author.Photo.Should().BeNull();
    }

    [Fact]
    public void Author_CanBeInstantiated_WithAllProperties()
    {
        // Arrange & Act
        var author = new Author
        {
            Id = "cat-1",
            Name = "Henri the Cat",
            Photo = "https://example.com/henri.jpg"
        };

        // Assert
        author.Id.Should().Be("cat-1");
        author.Name.Should().Be("Henri the Cat");
        author.Photo.Should().Be("https://example.com/henri.jpg");
    }

    [Fact]
    public void Author_Photo_CanBeNull()
    {
        // Arrange & Act
        var author = new Author
        {
            Id = "a_01",
            Name = "Anonymous Author",
            Photo = null  // Optional property
        };

        // Assert
        author.Photo.Should().BeNull();
    }

    #endregion

    #region Module Model Tests

    [Fact]
    public void Module_CanBeInstantiated_WithDefaultValues()
    {
        // Arrange & Act
        var module = new Module();

        // Assert
        module.Should().NotBeNull();
        module.Id.Should().BeEmpty();
        module.Title.Should().BeEmpty();
        module.Length.Should().BeNull();
        module.Content.Should().BeNull();
        module.VideoUrl.Should().BeNull();
    }

    [Fact]
    public void Module_CanBeInstantiated_WithAllProperties()
    {
        // Arrange & Act
        var module = new Module
        {
            Id = "l_0",
            Title = "Introduction to GraphQL",
            Length = 15,
            Content = "GraphQL is a query language...",
            VideoUrl = "https://youtube.com/watch?v=example"
        };

        // Assert
        module.Id.Should().Be("l_0");
        module.Title.Should().Be("Introduction to GraphQL");
        module.Length.Should().Be(15);
        module.Content.Should().Be("GraphQL is a query language...");
        module.VideoUrl.Should().Be("https://youtube.com/watch?v=example");
    }

    [Fact]
    public void Module_VideoUrl_CanBeNull_ForTextOnlyModules()
    {
        // Arrange & Act - Text-only module
        var module = new Module
        {
            Id = "m_01",
            Title = "Text Lesson",
            Content = "Read this content...",
            VideoUrl = null  // No video for text lessons
        };

        // Assert
        module.VideoUrl.Should().BeNull();
        module.Content.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region IncrementTrackViewsResponse Model Tests

    [Fact]
    public void IncrementTrackViewsResponse_CanBeInstantiated_WithDefaultValues()
    {
        // Arrange & Act
        var response = new IncrementTrackViewsResponse();

        // Assert
        response.Should().NotBeNull();
        response.Code.Should().Be(0);  // int default
        response.Success.Should().BeFalse();  // bool default
        response.Message.Should().BeEmpty();
        response.Track.Should().BeNull();
    }

    [Fact]
    public void IncrementTrackViewsResponse_CanRepresent_SuccessfulMutation()
    {
        // Arrange
        var track = new Track
        {
            Id = "c_0",
            Title = "Catstronauts",
            AuthorId = "cat-1",
            NumberOfViews = 43
        };

        // Act
        var response = new IncrementTrackViewsResponse
        {
            Code = 200,
            Success = true,
            Message = "Successfully incremented views for track c_0",
            Track = track
        };

        // Assert
        response.Code.Should().Be(200);
        response.Success.Should().BeTrue();
        response.Message.Should().Contain("Successfully");
        response.Track.Should().NotBeNull();
        response.Track!.NumberOfViews.Should().Be(43);
    }

    [Fact]
    public void IncrementTrackViewsResponse_CanRepresent_FailedMutation()
    {
        // Arrange & Act
        var response = new IncrementTrackViewsResponse
        {
            Code = 404,
            Success = false,
            Message = "Track not found",
            Track = null
        };

        // Assert
        response.Code.Should().Be(404);
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Track not found");
        response.Track.Should().BeNull();
    }

    [Fact]
    public void IncrementTrackViewsResponse_CanRepresent_ServerError()
    {
        // Arrange & Act
        var response = new IncrementTrackViewsResponse
        {
            Code = 500,
            Success = false,
            Message = "Internal server error",
            Track = null
        };

        // Assert
        response.Code.Should().Be(500);
        response.Success.Should().BeFalse();
        response.Track.Should().BeNull();
    }

    #endregion

    #region Property Assignment Tests

    [Fact]
    public void Track_Properties_CanBeModified()
    {
        // Arrange
        var track = new Track
        {
            Id = "t_01",
            Title = "Original Title",
            AuthorId = "a_01"
        };

        // Act - Modify property
        track.Title = "Updated Title";
        track.NumberOfViews = 100;

        // Assert
        track.Title.Should().Be("Updated Title");
        track.NumberOfViews.Should().Be(100);
    }

    [Fact]
    public void Models_SupportObjectInitializerSyntax()
    {
        // Arrange & Act - Using object initializer
        var track = new Track
        {
            Id = "t_01",
            Title = "Test",
            AuthorId = "a_01"
        };

        var author = new Author
        {
            Id = "a_01",
            Name = "Test Author"
        };

        var module = new Module
        {
            Id = "m_01",
            Title = "Test Module"
        };

        // Assert - All initialized correctly
        track.Should().NotBeNull();
        author.Should().NotBeNull();
        module.Should().NotBeNull();
    }

    #endregion
}
