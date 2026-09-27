using Godot;
using System;
namespace CuriousContraptions;

public partial class FanPart : MachinePart
{
    private Node3D _rotor = null!;
    public override bool CanReceiveActivation => true;
    public override AirflowEmitter? AirflowSource => Active
        ? new(Vector3.Zero,Vector3.Right,Properties[FanParameters.Reach],Properties[FanParameters.Width],Properties[FanParameters.Force]) : null;
    public override void ValidateParameters()
    {
        foreach(var key in new[]{FanParameters.Powered,FanParameters.Force,FanParameters.Reach,FanParameters.Width})
            if(!float.IsFinite(Properties[key]))throw new ArgumentException("Fan parameters must be finite.");
        if(Properties[FanParameters.Powered] is not (0 or 1)||Properties[FanParameters.Force]<0||Properties[FanParameters.Force]>40||
            Properties[FanParameters.Reach]<=0||Properties[FanParameters.Reach]>12||Properties[FanParameters.Width]<=0||Properties[FanParameters.Width]>4)
            throw new ArgumentException("Fan parameters are outside their supported bounds.");
    }
    protected override void Build()
    {
        Active = Properties[FanParameters.Powered] > .5f;
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
    public override void BeforeStep(MachineWorld world, float delta)
    {
        if (!Active) return;
        _rotor.RotateX(delta * 18);
    }
}
