namespace CuriousContraptions.Tests;

/// <summary>Native query-only nodes, deliberately not attached to the scene
/// tree or built from catalogue visuals. Free unmanaged nodes as well as wrappers.</summary>
internal sealed class GeometryQueryScene : IDisposable
{
    internal MachineWorld World { get; }=new();
    internal MachinePart Part { get; }=new();
    internal GeometryQueryScene()=>FixtureParts.Attach(World,Part,FixturePartId.First);
    public void Dispose() { World.Free(); }
}
