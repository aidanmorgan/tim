using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum PressurePlateState { Empty, Underweight, Pressed }

/// <summary>Continuous electrical contact. Sums masses directly resting on the top face,
/// not impacts, stacked load transmission or a generated power supply.</summary>
public partial class PressurePlatePart : MachinePart
{
    public const float Top=.08f;
    public const float HalfSize=.9f;
    private const float ContactSkin=.025f;
    public float MinimumMass=>Properties[PressurePlateParameters.MinimumMass];
    public float SupportedMass { get; private set; }
    public PressurePlateState State { get; private set; }
    private readonly List<StandardMaterial3D> _indicators=new();
    private float _glow;
    public override float SurfaceBounce=>.1f;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketIds.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(-1.18f,-.08f,0)),
        new(SocketIds.Supply,ConnectionDomain.Electrical,PortDirection.Output,new(1.18f,-.08f,0))
    ];
    public override IEnumerable<ElectricalRoute> ElectricalRoutes=>
        State==PressurePlateState.Pressed?[new(SocketIds.PowerIn,SocketIds.Supply)]:[];
    public override void ValidateParameters()
    {
        if(!float.IsFinite(MinimumMass)||MinimumMass<.1f||MinimumMass>16)
            throw new ArgumentException("Pressure plate minimum mass must be finite and between 0.1 and 16.");
    }
    protected override void Build()
    {
        PickRadius=1.5f;
        AddBox(new(0,-.12f,0),new(2.2f,.2f,2.2f),new("#293954"));
        AddBox(new(0,.03f,0),new(1.8f,.1f,1.8f),new("#fff8e9"));
        foreach(var x in new[]{-1f,1f})
        foreach(var z in new[]{-1f,1f})
            _indicators.Add((StandardMaterial3D)PartArt.Sphere(Visual,.055f,new("#556573"),new(x,.015f,z)).MaterialOverride);
        foreach(var x in new[]{-1.18f,1.18f})
            PartArt.Sphere(Visual,.075f,new("#e8b764"),new(x,-.08f,0));
        // A quiet central load target, not a control overlay.
        PartArt.Ring(Visual,.3f,.018f,new("#e8b764"),new(0,.084f,0));
    }
    public override void BeforeNetworks(MachineWorld world)
    {
        SupportedMass=0;
        var inverse=Transform.AffineInverse();
        foreach(var body in world.Bodies)
        {
            if(!body.Visible)continue;
            var local=inverse*body.Position;
            if(Mathf.Abs(local.X)>HalfSize||Mathf.Abs(local.Z)>HalfSize)continue;
            if(Mathf.Abs(local.Y-Top-body.Radius)>ContactSkin)continue;
            SupportedMass+=body.Mass;
        }
        State=SupportedMass<=0?PressurePlateState.Empty:
            SupportedMass<MinimumMass?PressurePlateState.Underweight:PressurePlateState.Pressed;
        Active=State==PressurePlateState.Pressed;
    }
    public override void BeforeStep(MachineWorld world,float delta)
    {
        _glow=Mathf.MoveToward(_glow,Active?1:0,delta*8);
        var resting=State==PressurePlateState.Underweight?new Color("#e8b764"):new Color("#556573");
        var color=resting.Lerp(new("#f7cb52"),_glow*_glow*(3-2*_glow));
        foreach(var indicator in _indicators)indicator.AlbedoColor=color;
    }
}
