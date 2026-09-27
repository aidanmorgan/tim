namespace CuriousContraptions.Tests;

public class PartParameterNameTests
{
    [Fact]
    public void TypedNamesMatchTheCurrentResourceBoundary()
    {
        Assert.Equal("speed", PartParameterName.Of(MotorParameter.Speed));
        Assert.Equal("torque", PartParameterName.Of(MotorParameter.Torque));
        Assert.Equal("length", PartParameterName.Of(ConveyorParameter.Length));
        Assert.Equal("width", PartParameterName.Of(ConveyorParameter.Width));
        Assert.Equal("surface_per_radian", PartParameterName.Of(ConveyorParameter.SurfacePerRadian));
        Assert.Equal("traction", PartParameterName.Of(ConveyorParameter.Traction));
        Assert.Equal("radians_per_force", PartParameterName.Of(WindmillParameter.RadiansPerForce));
    }
    [Fact]
    public void UndefinedParameterEnumsAreNotConvertedToNumericKeys()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PartParameterName.Of((MotorParameter)100));
        Assert.Throws<ArgumentOutOfRangeException>(() => PartParameterName.Of((ConveyorParameter)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PartParameterName.Of((WindmillParameter)100));
    }
}
