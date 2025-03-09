using Arcor2.ClientSdk.ClientServices.Extensions;
using Arcor2.ClientSdk.ClientServices.Models;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.UnitTests;

public class ParameterMetaExtensionsTests {
    [Fact]
    public void ToParameter_NoConstraintsDoubleWithDefaultValue_ValidRepresentation() {
        // Arrange
        var meta = new ParameterMeta("TestParameter", "double", false, null!, "A test parameter", "0.5", null!);

        // Act
        var parameter = meta.ToParameter();

        // Assert
        Assert.Equal("TestParameter", parameter.Name);
        Assert.Equal("double", parameter.Type);
        Assert.Equal("0.5", parameter.Value);
    }

    [Fact]
    public void ToParameter_NoConstraintsIntegerWithDefaultValue_ValidRepresentation() {
        // Arrange
        var meta = new ParameterMeta("TestParameter", "integer", false, null!, "A test parameter", "0", null!);

        // Act
        var parameter = meta.ToParameter();

        // Assert
        Assert.Equal("TestParameter", parameter.Name);
        Assert.Equal("integer", parameter.Type);
        Assert.Equal("0", parameter.Value);
    }

    [Fact]
    public void ToParameter_NoConstraintsBooleanWithDefaultValue_ValidRepresentation() {
        // Arrange
        var meta = new ParameterMeta("TestParameter", "boolean", false, null!, "A test parameter", "true", null!);

        // Act
        var parameter = meta.ToParameter();

        // Assert
        Assert.Equal("TestParameter", parameter.Name);
        Assert.Equal("boolean", parameter.Type);
        Assert.Equal("true", parameter.Value);
    }

    [Fact]
    public void ToParameter_NoConstraintsStringWithDefaultValue_ValidRepresentation() {
        // Arrange
        var meta = new ParameterMeta("TestParameter", "string", false, null!, "A test parameter", "\"Hello\"", null!);

        // Act
        var parameter = meta.ToParameter();

        // Assert
        Assert.Equal("TestParameter", parameter.Name);
        Assert.Equal("string", parameter.Type);
        Assert.Equal("\"Hello\"", parameter.Value);
    }

    [Fact]
    public void Validate_ValueConstraintsStringWithDefaultValue_Valid() {
        // Arrange
        var meta = new ParameterMeta("TestParameter", "string", false, null!, "A test parameter", "\"Hello\"", "{\"allowed_values\":[\"left\",\"right\"]}");

        // Act
        var validator = (meta.GetValidator() as ValuesParameterValidator)!;

        // Assert
        Assert.NotNull(validator);
        Assert.True(validator.Validate("left"));
        Assert.True(validator.Validate("right"));

        Assert.False(validator.Validate("lEft"));
        Assert.False(validator.Validate(""));
        Assert.False(validator.Validate("Right"));

        Assert.True(validator.Validate("lEft", StringComparison.InvariantCultureIgnoreCase));
        Assert.False(validator.Validate("", StringComparison.InvariantCultureIgnoreCase));
        Assert.True(validator.Validate("Right",StringComparison.InvariantCultureIgnoreCase));

        Assert.Contains("left", validator.AllowedValues);
        Assert.Contains("right", validator.AllowedValues);
    }

    [Fact]
    public void Validate_RangeConstraintsDoubleWithDefaultValue_Valid() {
        // Arrange
        var meta = new ParameterMeta("TestParameter", "double", false, null!, "A test parameter", "0.5", "{\"minimum\":0.0,\"maximum\":10000000.0}");

        // Act
        var validator = (meta.GetValidator() as RangeParameterValidator)!;

        // Assert
        Assert.NotNull(validator);
        Assert.True(validator.Validate("0.5"));
        Assert.True(validator.Validate("100.0"));
        Assert.True(validator.Validate(0.5));
        Assert.True(validator.Validate(100.0));

        Assert.False(validator.Validate("-4.1"));
        Assert.False(validator.Validate("1000000000.0"));
        Assert.False(validator.Validate(-4.1));
        Assert.False(validator.Validate(1000000000.0));

        Assert.Equal((decimal) 10000000.0, validator.Maximum);
        Assert.Equal((decimal) 0.0, validator.Minimum);
    }

    [Fact]
    public void Validate_RangeConstraintsIntegerWithDefaultValue_Valid() {
        // Arrange
        var meta = new ParameterMeta("TestParameter", "integer", false, null!, "A test parameter", "5", "{\"minimum\":0,\"maximum\":10000000}");

        // Act
        var validator = (meta.GetValidator() as RangeParameterValidator)!;

        // Assert
        Assert.NotNull(validator);
        Assert.True(validator.Validate("5"));
        Assert.True(validator.Validate("100"));
        Assert.True(validator.Validate(5));
        Assert.True(validator.Validate(100));

        Assert.False(validator.Validate("-4"));
        Assert.False(validator.Validate("1000000000"));
        Assert.False(validator.Validate(-4));
        Assert.False(validator.Validate(1000000000));

        Assert.Equal(10000000, validator.Maximum);
        Assert.Equal(0, validator.Minimum);
    }
}