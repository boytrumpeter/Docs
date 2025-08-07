using FluentAssertions;
using DocumentProcessing.Domain.ValueObjects;

namespace DocumentProcessing.Domain.Tests.ValueObjects;

public class ValidationResultTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateValidationResult()
    {
        // Arrange
        var isValid = true;
        var errors = new List<string> { "Error 1", "Error 2" };
        var schema = "TestSchema";

        // Act
        var result = new ValidationResult(isValid, errors, schema);

        // Assert
        result.IsValid.Should().Be(isValid);
        result.Errors.Should().BeEquivalentTo(errors);
        result.Schema.Should().Be(schema);
    }

    [Fact]
    public void Constructor_WithNullErrors_ShouldCreateEmptyErrorList()
    {
        // Arrange & Act
        var result = new ValidationResult(false, null, "TestSchema");

        // Assert
        result.Errors.Should().BeEmpty();
        result.Errors.Should().NotBeNull();
    }

    [Fact]
    public void Success_WithoutSchema_ShouldCreateValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Schema.Should().BeNull();
    }

    [Fact]
    public void Success_WithSchema_ShouldCreateValidResultWithSchema()
    {
        // Arrange
        var schema = "TestSchema";

        // Act
        var result = ValidationResult.Success(schema);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Schema.Should().Be(schema);
    }

    [Fact]
    public void Failure_WithErrorsCollection_ShouldCreateInvalidResult()
    {
        // Arrange
        var errors = new List<string> { "Error 1", "Error 2" };
        var schema = "TestSchema";

        // Act
        var result = ValidationResult.Failure(errors, schema);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().BeEquivalentTo(errors);
        result.Schema.Should().Be(schema);
    }

    [Fact]
    public void Failure_WithSingleError_ShouldCreateInvalidResult()
    {
        // Arrange
        var error = "Single error";
        var schema = "TestSchema";

        // Act
        var result = ValidationResult.Failure(error, schema);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be(error);
        result.Schema.Should().Be(schema);
    }

    [Fact]
    public void Failure_WithoutSchema_ShouldCreateInvalidResultWithNullSchema()
    {
        // Arrange
        var error = "Error message";

        // Act
        var result = ValidationResult.Failure(error);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be(error);
        result.Schema.Should().BeNull();
    }

    [Fact]
    public void Errors_ShouldBeReadOnly()
    {
        // Arrange
        var errors = new List<string> { "Error 1", "Error 2" };
        var result = new ValidationResult(false, errors);

        // Act & Assert
        result.Errors.Should().BeAssignableTo<IReadOnlyList<string>>();
        
        // Verify we can't modify the original collection through the result
        errors.Add("Error 3");
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void ValidationResult_WithEqualValues_ShouldBeEqual()
    {
        // Arrange
        var errors = new List<string> { "Error 1" };
        var result1 = new ValidationResult(false, errors, "Schema");
        var result2 = new ValidationResult(false, errors, "Schema");

        // Act & Assert
        result1.Should().Be(result2);
        (result1 == result2).Should().BeTrue();
        result1.GetHashCode().Should().Be(result2.GetHashCode());
    }

    [Fact]
    public void ValidationResult_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var result1 = ValidationResult.Success("Schema1");
        var result2 = ValidationResult.Success("Schema2");

        // Act & Assert
        result1.Should().NotBe(result2);
        (result1 != result2).Should().BeTrue();
    }
}