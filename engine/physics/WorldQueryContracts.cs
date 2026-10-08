using CuriousContraptions.Physics;
namespace CuriousContraptions;

public enum TraceMedium { Light, Sound, Air }
public enum SweepBodyMode { IncludeBodies, ExcludeBodies, ExcludeDynamicBodies }
public enum SweepSurfaceKind { None, Box, Sphere, Tube, Bend, Frustum, Body, Convex }
public enum SweepObstacleKind { None, Part, Workbench }
public enum WorldSweepStatus { Clear, Contact, Overlapping }
public readonly record struct WorldSweepResult(WorldSweepStatus Status, double Distance, CollisionVector Normal,
    SweepObstacleKind Kind, PhysicsBodyId? Body, SweepSurfaceKind Surface, double Penetration);

public readonly record struct BodySpatialState(RigidPose Pose,CollisionParticipation Participation)
{
    public bool Enabled=>Participation==CollisionParticipation.Enabled;
}

public readonly record struct WorldOverlapResult(SweepObstacleKind Kind,PhysicsBodyId? Body,
    ColliderChildId MovingChild,ColliderChildId ObstacleChild,SweepSurfaceKind Surface,ConvexSeparationResult Separation);

public enum BodyQueryPolicy { Include, ExcludeFromStaticQueries }
