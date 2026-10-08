using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class JetTransferImpedanceTests
{
    [Theory]
    [InlineData(2,100,10,4,12,120,48,72)]
    [InlineData(2,5,10,4,5,50,20,30)]
    [InlineData(2,100,10,-4,28,280,-112,392)]
    [InlineData(2,100,0,-4,8,0,-32,32)]
    [InlineData(2,100,10,10,0,0,0,0)]
    [InlineData(2,100,10,14,0,0,0,0)]
    [InlineData(0,100,10,4,0,0,0,0)]
    [InlineData(2,0,10,4,0,0,0,0)]
    public void IndependentForceAndPowerExamples(double conductance,double ceiling,
        double source,double receiver,double force,double input,double output,double loss)
    {
        var result=new JetTransferImpedance(conductance,ceiling).Evaluate(source,receiver);
        Assert.Equal(new(force,input,output,loss),result);
        Assert.Equal(result.SourcePower-result.ReceiverPower,result.DissipatedPower);
        Assert.InRange(result.DissipatedPower,0,double.MaxValue);
    }

    [Fact]
    public void EveryBranchAddsPumpReactionRatherThanCopyingSupply()
    {
        // qdot=2 m/s, ratio=5, body speed=4 m/s; rotor arm=.5 m,
        // shaft speed=8 rad/s. Both independent ports see v=4 m/s.
        const double compression=2,ratio=5,arm=.5,shaftSpeed=8;
        var law=new JetTransferImpedance(2,100);
        var body=law.Evaluate(ratio*compression,4);
        var rotor=law.Evaluate(ratio*compression,arm*shaftSpeed);
        var pumpEffort=-ratio*(body.Force+rotor.Force);
        var bodyPower=body.Force*4;
        var rotorPower=arm*rotor.Force*shaftSpeed;
        Assert.Equal(-120,pumpEffort);
        Assert.Equal(96,bodyPower+rotorPower);
        Assert.Equal(-144,pumpEffort*compression+bodyPower+rotorPower);
        Assert.Equal(144,body.DissipatedPower+rotor.DissipatedPower);
    }

    [Fact]
    public void FiniteCapAvoidsOverflowOfUncappedForce()
    {
        var result=new JetTransferImpedance(double.MaxValue,1).Evaluate(2,0);
        Assert.Equal(new(1,2,0,2),result);
    }

    [Theory]
    [InlineData(-1,1)]
    [InlineData(1,-1)]
    [InlineData(double.NaN,1)]
    [InlineData(1,double.NaN)]
    [InlineData(double.PositiveInfinity,1)]
    [InlineData(1,double.PositiveInfinity)]
    public void InvalidMaterialParametersReject(double conductance,double ceiling)=>
        Assert.Throws<ArgumentOutOfRangeException>(()=>new JetTransferImpedance(conductance,ceiling));

    [Theory]
    [InlineData(-1,0)]
    [InlineData(double.NaN,0)]
    [InlineData(double.PositiveInfinity,0)]
    [InlineData(0,double.NaN)]
    [InlineData(0,double.PositiveInfinity)]
    [InlineData(0,double.NegativeInfinity)]
    public void InvalidSpeedRejectsEvenWhenDisabled(double source,double receiver)=>
        Assert.Throws<ArgumentOutOfRangeException>(()=>new JetTransferImpedance(0,0).Evaluate(source,receiver));

    [Fact]
    public void UnrepresentableSlipRejects()=>
        Assert.Throws<InvalidOperationException>(()=>
            new JetTransferImpedance(1,1).Evaluate(double.MaxValue,-double.MaxValue));

    [Fact]
    public void UnrepresentablePowerRejects()=>
        Assert.Throws<InvalidOperationException>(()=>
            new JetTransferImpedance(2,2).Evaluate(double.MaxValue,0));
}
