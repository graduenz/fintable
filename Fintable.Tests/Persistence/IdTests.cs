using Fintable.Persistence;

namespace Fintable.Tests.Persistence;

public class IdTests
{
    [Fact]
    public void New_ReturnsNonEmptyString()
    {
        // Arrange & Act
        var id = Id.New();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(id));
    }

    [Fact]
    public void New_ReturnsValidUlidFormat()
    {
        // Arrange & Act
        var id = Id.New();

        // Assert
        Assert.Equal(26, id.Length);
        Assert.True(Ulid.TryParse(id, out _));
    }

    [Fact]
    public void New_ReturnsUniqueValues()
    {
        // Arrange
        const int count = 100;

        // Act
        var ids = Enumerable.Range(0, count).Select(_ => Id.New()).ToList();

        // Assert
        Assert.Equal(count, ids.Distinct().Count());
    }
}
