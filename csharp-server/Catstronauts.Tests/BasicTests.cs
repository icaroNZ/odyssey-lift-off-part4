using FluentAssertions;
using Xunit;

namespace Catstronauts.Tests;

/// <summary>
/// Basic sanity tests to verify test setup is working.
/// </summary>
public class BasicTests
{
    [Fact]
    public void SanityCheck_OnePlusOne_EqualsTwo()
    {
        // Arrange
        var a = 1;
        var b = 1;

        // Act
        var result = a + b;

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void String_Concatenation_Works()
    {
        // Arrange
        var firstName = "GraphQL";
        var lastName = "Rocks";

        // Act
        var fullName = $"{firstName} {lastName}";

        // Assert
        fullName.Should().Be("GraphQL Rocks");
    }
}
