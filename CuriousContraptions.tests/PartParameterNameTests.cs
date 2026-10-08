namespace CuriousContraptions.Tests;

public class PartParameterNameTests
{
    [Theory]
    [InlineData(BallParameter.Radius,"radius")]
    [InlineData(BallParameter.Mass,"mass")]
    [InlineData(BallParameter.Bounce,"bounce")]
    [InlineData(BallParameter.Buoyancy,"buoyancy")]
    [InlineData(BallParameter.Drag,"drag")]
    public void BallParameterNamesAreCanonical(BallParameter parameter,string serialized)
        =>Assert.Equal(serialized,PartParameterName.Of(parameter));

    [Theory]
    [InlineData(RampParameter.Length,"length")]
    [InlineData(RampParameter.Width,"width")]
    public void RampParameterNamesAreCanonical(RampParameter parameter,string serialized)
        =>Assert.Equal(serialized,PartParameterName.Of(parameter));

    [Theory]
    [InlineData(WallParameter.Width,"width")]
    [InlineData(WallParameter.Height,"height")]
    [InlineData(WallParameter.Thickness,"thickness")]
    public void WallParameterNamesAreCanonical(WallParameter parameter,string serialized)
        =>Assert.Equal(serialized,PartParameterName.Of(parameter));

    [Theory]
    [InlineData(BatteryParameter.Enabled,"enabled")]
    public void BatteryParameterNamesAreCanonical(BatteryParameter parameter,string serialized)
        =>Assert.Equal(serialized,PartParameterName.Of(parameter));

    [Fact]
    public void TypedNamesMatchTheCurrentResourceBoundary()
    {
        Assert.Equal("close_seconds", PartParameterName.Of(ClutchParameter.CloseSeconds));
        Assert.Equal("speed", PartParameterName.Of(MotorParameter.Speed));
        Assert.Equal("torque", PartParameterName.Of(MotorParameter.Torque));
        Assert.Equal("length", PartParameterName.Of(ConveyorParameter.Length));
        Assert.Equal("width", PartParameterName.Of(ConveyorParameter.Width));
        Assert.Equal("surface_per_radian", PartParameterName.Of(ConveyorParameter.SurfacePerRadian));
        Assert.Equal("radians_per_force", PartParameterName.Of(WindmillParameter.RadiansPerForce));
    }
    [Fact]
    public void UndefinedParameterEnumsAreNotConvertedToNumericKeys()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((BallParameter)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((RampParameter)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((WallParameter)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((BatteryParameter)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => PartParameterName.Of((ClutchParameter)100));
        Assert.Throws<ArgumentOutOfRangeException>(() => PartParameterName.Of((MotorParameter)100));
        Assert.Throws<ArgumentOutOfRangeException>(() => PartParameterName.Of((ConveyorParameter)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PartParameterName.Of((WindmillParameter)100));
    }
}
