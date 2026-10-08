using Godot;
using System;
using System.Collections.Generic;
using CuriousContraptions.Physics;
using CuriousContraptions.Presentation;
namespace CuriousContraptions;

public partial class FanPart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<FanParameter>(fields);
    public const double SupplyEnergy=14_400;
    private SceneMechanicalSourceKey SupplyKey=>new(new(this,RootBody),AirflowNetwork.SourceSlot);
    public override IReadOnlyList<SceneMechanicalSourceKey> PhysicsTransferSources=>[SupplyKey];
    public override IReadOnlyList<SceneEnergyStoreDeclaration> PhysicsEnergyStores=>
        [new(new(this,RootBody),SupplyEnergy,SupplyEnergy)];
    private Node3D _rotor = null!;
    public override bool CanReceiveActivation => true;
    public override AirflowEmitter? CreateAirflowSource(MachineWorld world) => Active
        ? new(Vector3.Zero,Vector3.Right,ReadParameter(FanParameter.Reach),ReadParameter(FanParameter.Width),
            new(new StoredFlowSource(world.TransferBindings.Source(SupplyKey),world.PhysicsAssembly.Body(SupplyKey.Body).Id,
                AirflowNetwork.ReferenceFlowSpeed,new(ReadParameter(FanParameter.Force),ReadParameter(FanParameter.Force)*AirflowNetwork.ReferenceFlowSpeed))),
            new(ReadParameter(FanParameter.Force)/AirflowNetwork.ReferenceFlowSpeed,ReadParameter(FanParameter.Force))) : null;
    protected override void ValidateParameters(PartParameterValues parameters)
    {
        foreach(var key in Enum.GetValues<FanParameter>())
            if(!float.IsFinite(parameters.Read(key)))throw new ArgumentException("Fan parameters must be finite.");
        if(parameters.Read(FanParameter.Powered) is not (0 or 1)||parameters.Read(FanParameter.Force)<0||parameters.Read(FanParameter.Force)>40||
            parameters.Read(FanParameter.Reach)<=0||parameters.Read(FanParameter.Reach)>12||parameters.Read(FanParameter.Width)<=0||parameters.Read(FanParameter.Width)>4)
            throw new ArgumentException("Fan parameters are outside their supported bounds.");
    }
    protected override void Build()
    {
        Active = ReadParameter(FanParameter.Powered) > .5f;
        PickRadius = .75f;
        AddBox(new(0, -.55f, 0), new(.65f, .18f, 1), new("#263d4b"));
        PartArt.Box(Visual, new(.13f, .5f, .15f), new("#ccd8dc"), new(0, -.3f, 0));
        var housing = PartArt.Ring(Visual, .56f, .07f, Definition.Color);
        housing.RotationDegrees = new(0, 0, 90);
        _rotor = new Node3D();
        Visual.AddChild(_rotor);
        PartArt.Sphere(_rotor, .14f, new("#f4d089"));
        foreach (var angle in new[] { 0, 120, 240 })
        {
            var blade = PartArt.Box(_rotor, new(.1f, .6f, .16f), Definition.Color.Lightened(.15f));
            blade.RotationDegrees = new(angle, 0, 0);
        }
        PartArt.Line(Visual, new(.6f, 0, 0), new(1.15f, 0, 0), new("#a9e7e0"), .025f);
    }
    private static readonly AnimationDefinition RotorSpin = new(0, Math.Tau, Math.Tau / 18,
        AnimationCurve.Linear, AnimationRepeat.Loop, AnimationClock.Simulation);
    public override IReadOnlyList<SceneRotationAnimation> RotationAnimations =>
        [new(_rotor, RotorSpin, AnimationRotationAxis.X, SceneAnimationSignal.OwnerActive, SceneAnimationDrive.StartStop)];
}
