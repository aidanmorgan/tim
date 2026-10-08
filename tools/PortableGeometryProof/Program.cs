using CuriousContraptions.Physics;

namespace CuriousContraptions.PortableGeometryProof;

internal enum Criterion { DoubleAuthority, AffineCapture, AffineRejection, SupportFootprint, HollowBore, RotatingSweep, Rollback, QuerySnapshots }
internal static class Program
{
    private static void Require(bool condition, Criterion criterion)
    {
        if (!condition) throw new InvalidOperationException($"Criterion {criterion} failed.");
    }
    private static void Near(double actual, double expected, double tolerance, Criterion criterion) =>
        Require(double.IsFinite(actual) && Math.Abs(actual - expected) <= tolerance, criterion);
    private static void Reject(Action action, Criterion criterion)
    {
        try { action(); } catch (ArgumentException) { return; }
        throw new InvalidOperationException($"Criterion {criterion} accepted invalid input.");
    }
    private static int Main()
    {
        var large = Math.ScaleB(1, 40) + .25;
        var pose = RigidPose.At(new(large, 0, 0));
        Near(pose.TransformPoint(new(.25, 0, 0)).X, large + .25, 0, Criterion.DoubleAuthority);
        var basis = new AffineBasis(new(2, 0, 0), new(.5, 3, 0), new(0, 0, -4));
        var affine = new AffineTransform(basis, new(5, 6, 7));
        Require(affine.TransformPoint(new(1, 2, 3)) == new CollisionVector(8, 12, -5), Criterion.AffineCapture);
        Reject(() => new ConvexInstance(new ConvexSphere(1), affine), Criterion.AffineRejection);
        Reject(() => new AffineBasis(new(double.NaN, 0, 0), default, default), Criterion.AffineRejection);
        Reject(() => new ConvexInstance(new ConvexSphere(1), default), Criterion.AffineRejection);
        var sx = (double)(float)(1 + 2e-6);
        var sy = (double)(float)(1 - 2e-6);
        var sz = (double)(float)(1 + 3e-6);
        var distorted = new AffineTransform(new(new(sx, 0, 0), new(0, sy, 0), new(0, 0, sz)), new(.25, .5, -.75));
        var box = new ConvexInstance(new ConvexBox(new(1, 2, 3)), distorted);
        Require(box.Support(new(1, 1, 1)) == new CollisionVector(.25 + sx, .5 + 2 * sy, -.75 + 3 * sz), Criterion.AffineCapture);
        var footprint = SupportFootprint.Sample(new([box]), RigidPose.Identity, RigidPose.Identity);
        Near(footprint.MinimumX, .25 - sx, 0, Criterion.SupportFootprint);
        Near(footprint.MaximumX, .25 + sx, 0, Criterion.SupportFootprint);
        Near(footprint.LowestPoint.Y, .5 - 2 * sy, 1e-15, Criterion.SupportFootprint);
        // The discarded-basis model predicts -1.5, a known wrong value, not an oracle.
        Require(Math.Abs(footprint.LowestPoint.Y - (-1.5)) > 1e-6, Criterion.SupportFootprint);
        var hollow = HollowGeometry.Tube(1, .65, .7, new(.005));
        var bore = new PointTrace(new(-2, 0, 0), new(1, 0, 0), 4);
        bore.Test(hollow.Geometry, RigidPose.Identity);
        Near(bore.Closest, 4, 0, Criterion.HollowBore);
        var wall = new PointTrace(new(-2, .675, 0), new(1, 0, 0), 4);
        wall.Test(hollow.Geometry, RigidPose.Identity);
        Near(wall.Closest, 1, 2e-7, Criterion.HollowBore);
        var shape = new ConvexInstance(new ConvexBox(new(2, .05, .05)), AffineTransform.Identity);
        var body = new PhysicsBody(new(0), PhysicsMotionType.Kinematic, RigidPose.Identity, default, new(0, 0, 1));
        var targetPosition = new CollisionVector(1.5 * Math.Cos(.4), 1.5 * Math.Sin(.4), 0);
        var target = new PhysicsBody(new(1), PhysicsMotionType.Static, RigidPose.At(targetPosition), default, default);
        var hit = ConvexSweep.Cast(new(shape, body.CreateTrajectory(1, default)),
            new(new(new ConvexBox(new(.05, .05, .05)), AffineTransform.Identity), target.CreateTrajectory(1, default)),
            1, ConvexSweep.ContactDistance);
        double low = 0, high = .4;
        for (var i = 0; i < 80; i++)
        {
            var angle = (low + high) / 2;
            var gap = targetPosition.Y * Math.Cos(angle) - targetPosition.X * Math.Sin(angle) -
                .05 - .05 * (Math.Sin(angle) + Math.Cos(angle)) - ConvexSweep.ContactDistance;
            if (gap > 0) low = angle; else high = angle;
        }
        Require(hit.Status == ConvexSweepStatus.Contact, Criterion.RotatingSweep);
        Near(hit.Time, (low + high) / 2, 1e-6, Criterion.RotatingSweep);
        var snapshot = body.Snapshot();
        body.Advance(body.CreateTrajectory(.5, default), .5);
        Require(body.Snapshot() != snapshot, Criterion.Rollback);
        body.Restore(snapshot);
        Require(body.Snapshot() == snapshot, Criterion.Rollback);
        var queryGeometry = new BodyQueryGeometry(BodyQueryPolicy.Include,
            [new(new(new ConvexBox(new(.5, 1, 1)), AffineTransform.Identity), SweepSurfaceKind.Box, true, ColliderQuerySource.PartProxy)]);
        var surfaces = new[] { new PreparedColliderGroup(queryGeometry.All, RigidPose.At(new(3, 0, 0)), SweepObstacleKind.Part, new(7)) };
        var query = new WorldSweepSnapshot(surfaces);
        surfaces[0] = surfaces[0] with { Pose = RigidPose.At(new(30, 0, 0)) };
        var moving = new CompoundGeometry([new(new ConvexSphere(.25), AffineTransform.Identity)]);
        var result = query.Sweep(moving, RigidPose.Identity, new(5, 0, 0));
        Require(result.Body == new PhysicsBodyId(7) && result.Status == WorldSweepStatus.Contact, Criterion.QuerySnapshots);
        Near(result.Distance, 2.25, 2e-7, Criterion.QuerySnapshots);
        Require(query.Sweep(moving, RigidPose.At(new(0, 0, 3)), new(5, 0, 0)).Status == WorldSweepStatus.Clear, Criterion.QuerySnapshots);
        Reject(() => new BodyQueryGeometry((BodyQueryPolicy)999, queryGeometry.Children), Criterion.QuerySnapshots);
        Console.WriteLine("Portable geometry: 8 criterion groups passed; no scene or 2dog assembly reference.");
        return 0;
    }
}
