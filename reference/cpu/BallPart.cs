using Godot;
namespace CuriousContraptions;

public enum BallParameter { Radius, Mass, Bounce, Buoyancy, Drag }

public partial class BallPart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<BallParameter>(fields);
    public override BodyEnvelope CollisionEnvelope=>BodyEnvelope.Sphere;
    public override BodyDynamics InitialBodyDynamics=>BodyDynamics.SolidSphere(Mass,Radius,
        SceneGeometryAdapter.CaptureVector(InitialVelocity),default);
    protected override void ValidateParameters(PartParameterValues parameters)=>RequireParameters<BallParameter>(parameters);
    protected override void Build()
    {
        Dynamic = true;
        Radius = ReadParameter(BallParameter.Radius);
        Mass = ReadParameter(BallParameter.Mass);
        Bounce = ReadParameter(BallParameter.Bounce);
        Buoyancy = ReadParameter(BallParameter.Buoyancy);
        Drag = ReadParameter(BallParameter.Drag);
        PickRadius = Radius + .12f;
        PartArt.Sphere(Visual, Radius, Definition.Color);
        var stripe = PartArt.Ring(Visual, Radius * .99f, .014f, Definition.Color.Darkened(.4f));
        stripe.RotationDegrees = new(0, 0, 35);
        if (Buoyancy > 0)
            PartArt.Line(Visual, new(0, -Radius, 0), new(.06f, -Radius - .5f, 0), new("#e9dfcc"), .015f);
    }
}

