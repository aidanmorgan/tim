using Godot;
using CuriousContraptions.Physics;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Continuous electrical contact. Sums masses directly resting on the top face,
/// not impacts, stacked load transmission or a generated power supply.</summary>
public partial class PressurePlatePart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<PressurePlateParameter>(fields);
    public const float Top=.08f;
    public const float HalfSize=.9f;
    public float MinimumMass=>ReadParameter(PressurePlateParameter.MinimumMass);
    private PhysicsContactLoadReading? RuntimeReading=>GetParent() is MachineWorld {HasPhysicsState:true} world?
        world.Physics.ContactLoad(world.PhysicsAssembly.Body(new(this,RootBody)).Id):null;
    public double SupportedMass=>RuntimeReading?.Mass??0;
    public PhysicsContactLoadPhase State=>RuntimeReading?.Phase??PhysicsContactLoadPhase.Empty;
    public override IReadOnlyList<SceneContactLoadSensorDeclaration> PhysicsContactLoadSensors=>
        [new(new(this,RootBody),
            new(new(-HalfSize-ConvexSweep.ContactDistance,Top-ConvexSweep.ContactDistance,-HalfSize-1e-6),
                new(HalfSize+ConvexSweep.ContactDistance,Top+ConvexSweep.ContactDistance,HalfSize+1e-6)),
            new(0,1,0),1-1e-6,MinimumMass)];
    private readonly List<StandardMaterial3D> _indicators=new();
    private float _glow;
    public override Physics.ContactMaterial InitialContactMaterial => new(.1f,.1,.3);
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketId.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(-1.18f,-.08f,0)),
        new(SocketId.Supply,ConnectionDomain.Electrical,PortDirection.Output,new(1.18f,-.08f,0))
    ];
    public override IEnumerable<ElectricalRoute> ElectricalRoutes=>
        [new(SocketId.PowerIn,SocketId.Supply,ElectricalContactSignal.ContactLoaded(new(this,RootBody)))];
    protected override void ValidateParameters(PartParameterValues parameters)
    {
        var MinimumMass=parameters.Read(PressurePlateParameter.MinimumMass);
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
    public override void BeforeNetworks(MachineWorld world)=>Active=State==PhysicsContactLoadPhase.Loaded;
    public override void PreparePhysics(MachineWorld world,float delta)
    {
        _glow=Mathf.MoveToward(_glow,Active?1:0,delta*8);
        var resting=State==PhysicsContactLoadPhase.Underweight?new Color("#e8b764"):new Color("#556573");
        var color=resting.Lerp(new("#f7cb52"),_glow*_glow*(3-2*_glow));
        foreach(var indicator in _indicators)indicator.AlbedoColor=color;
    }
}
