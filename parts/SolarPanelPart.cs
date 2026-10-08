using Godot;
using System.Collections.Generic;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions;

/// <summary>Nine equal-area samples on the front face convert received game-light into binary supply.
/// No ambient-sky power, storage or voltage/current model.</summary>
public partial class SolarPanelPart : MachinePart
{
    public const float Threshold = 1;
    private readonly SimulationState<float> _irradiance = new(0);
    public float Irradiance => _irradiance.Value;
    public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState => [_irradiance];
    public static readonly ScalarObservationSlot IrradianceOutput=new(0);
    public override IReadOnlyList<SceneScalarObservation> ScalarObservations=>
        [new(IrradianceOutput,ScalarUnit.GameIrradiance,new(_irradiance))];
    private readonly List<SceneColourAnimation> _meter=new();
    public override IReadOnlyList<SceneColourAnimation> ColourAnimations=>_meter;
    private static readonly Color InactiveColour=new("#556573"),ActiveColour=new("#f7cb52");
    private static readonly AnimationDefinition MeterTransition=new(0,1,.1,
        AnimationCurve.SmoothStep,AnimationRepeat.Once,AnimationClock.Presentation);
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
        [new(SocketId.Supply, ConnectionDomain.Electrical, PortDirection.Output, new(.2f, -.5f, .6f))];
    public override IEnumerable<LightSample> LightSamples
    {
        get
        {
            foreach (var y in new[] { -.35f, 0f, .35f })
            foreach (var z in new[] { -.45f, 0f, .45f })
                yield return new(new(-.13f, y, z), Vector3.Left, 1f / 9);
        }
    }
    public override void ReceiveLight(float irradiance) => _irradiance.Value = irradiance;
    public override IEnumerable<ElectricalSourceDeclaration> ElectricalSources=>
        [new(SocketId.Supply,ElectricalSourceSignal.AtLeast(_irradiance,Threshold))];
    protected override void Build()
    {
        PickRadius = 1;
        AddBox(Vector3.Zero, new(.22f, 1.2f, 1.5f), new("#fff8e9"));
        foreach (var y in new[] { -.35f, 0f, .35f })
        foreach (var z in new[] { -.45f, 0f, .45f })
            PartArt.Box(Visual, new(.025f, .29f, .39f), Definition.Color, new(-.125f, y, z));
        AddBox(new(0, -.8f, 0), new(.9f, .16f, 1.2f), new("#293954"));
        PartArt.Cylinder(Visual, .1f, .3f, new("#f7cb52"), new(0, -.65f, 0));
        for (var i = 0; i < 4; i++)
        {
            var target=PartArt.Box(Visual,new(.025f,.08f,.17f),InactiveColour,new(-.145f,-.51f,-.3f+i*.2f));
            _meter.Add(new(target,MeterTransition,InactiveColour,ActiveColour,
                SceneAnimationSignal.ScalarAtLeast(new(this,IrradianceOutput),ScalarUnit.GameIrradiance,Threshold*(i+1)/4d),
                SceneAnimationDrive.Endpoint));
        }
        PartArt.Sphere(Visual, .085f, new("#f7cb52"), new(.2f, -.5f, .6f));
    }
    public override void PreparePhysics(MachineWorld world, float delta)
    {
        Active = Irradiance >= Threshold;
        if (Active) world.Events.TryAdd(new(MachineEventKind.Powered, Uid), world.Ticks);
    }
}
