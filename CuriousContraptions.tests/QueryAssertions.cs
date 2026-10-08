using CuriousContraptions.Physics;
namespace CuriousContraptions.Tests;

internal static class QueryAssertions
{
    internal static void Owner(MachineWorld world, MachinePart expected, PhysicsBodyId? body)
    {
        Assert.NotNull(body);
        var capture = Assert.Single(WorldGeometry.CaptureBodies(world), candidate => candidate.Id == body.Value);
        Assert.Same(expected, capture.Owner);
    }
}
