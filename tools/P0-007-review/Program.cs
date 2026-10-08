using Godot;
using CuriousContraptions;
using CuriousContraptions.Geometry;
using CuriousContraptions.Physics;
using System.Text.Json;
namespace P0007Review;
internal enum BoundaryCase { PreviouslyAccepted, PreviouslyRejected, Identity, AffineSupport }
internal static class Program
{
    private static void Main()
    {
        Check(BoundaryCase.PreviouslyAccepted, .9778739213943481f, .20917120575904846f);
        Check(BoundaryCase.PreviouslyRejected, -.7564643025398254f, .6540426015853882f);
        Check(BoundaryCase.Identity, 1f, 0f);
        var affine = new AffineTransform(new(new(1.000002,0,0),new(0,.999997,0),new(0,0,1.000001)), new(.25,.5,-.75));
        var shape = new ConvexInstance(new ConvexBox(new(1,2,3)),affine);
        var body = new RigidPose(new(2,3,4),RigidRotation.Identity);
        var frame = RigidPose.At(new(1,1,1));
        var actual = SupportFootprint.Sample(new([shape]), body, frame);
        if (Math.Abs(actual.MinimumX - .249998)>1e-14 || Math.Abs(actual.MaximumX - 2.250002)>1e-14 ||
            Math.Abs(actual.LowestPoint.Y - .500006)>1e-14 || Math.Abs(actual.MinimumZ - (-.750003))>1e-14 ||
            Math.Abs(actual.MaximumZ - 5.250003)>1e-14) throw new InvalidOperationException("Independent affine support golden failed.");
        Console.WriteLine(JsonSerializer.Serialize(new { Case=BoundaryCase.AffineSupport, actual }));
    }
    private static void Check(BoundaryCase scenario,float x,float y)
    {
        var basis=new Basis(new Vector3(x,y,0),new Vector3(-y,x,0),Vector3.Back);
        var oldAccepted = basis.X.IsFinite() && basis.Y.IsFinite() && basis.Z.IsFinite() &&
            Math.Abs(basis.X.LengthSquared()-1)<=.00001 && Math.Abs(basis.Y.LengthSquared()-1)<=.00001 &&
            Math.Abs(basis.Z.LengthSquared()-1)<=.00001 && Math.Abs(basis.X.Dot(basis.Y))<=.00001 &&
            Math.Abs(basis.X.Dot(basis.Z))<=.00001 && Math.Abs(basis.Y.Dot(basis.Z))<=.00001 &&
            Math.Abs(basis.Determinant()-1)<=.0001;
        var affine=SceneGeometryAdapter.CaptureAffine(new(basis,Vector3.Zero));
        var newAccepted=true;
        try { _=new ConvexInstance(new ConvexSphere(1),affine); } catch(ArgumentException) { newAccepted=false; }
        Console.WriteLine(JsonSerializer.Serialize(new { Case=scenario, x,y, FloatNorm=basis.X.LengthSquared(),
            DoubleNorm=affine.Basis.X.LengthSquared, oldAccepted,newAccepted }));
    }
}
