using Fintable.Features.Providers;
using Fintable.Persistence;

namespace Fintable.Tests.Features.Providers;

public class ProviderDtoValidatorTests
{
    private readonly ProviderDtoValidator _validator = new();

    [Fact]
    public void Validate_ValidDto_HasNoValidationErrors()
    {
        // Arrange
        var dto = new ProviderDto
        {
            Id = string.Empty,
            Type = ProviderType.Organizze,
            Name = "My Provider",
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_EmptyName_HasValidationError()
    {
        // Arrange
        var dto = new ProviderDto
        {
            Id = string.Empty,
            Type = ProviderType.Organizze,
            Name = string.Empty,
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ProviderDto.Name));
    }

    [Fact]
    public void Validate_EmptyType_HasValidationError()
    {
        // Arrange
        var dto = new ProviderDto
        {
            Id = string.Empty,
            Type = string.Empty,
            Name = "My Provider",
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ProviderDto.Type));
    }

    [Fact]
    public void Validate_UnknownType_HasValidationError()
    {
        // Arrange
        var dto = new ProviderDto
        {
            Id = string.Empty,
            Type = "unknown",
            Name = "My Provider",
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ProviderDto.Type));
    }
}
