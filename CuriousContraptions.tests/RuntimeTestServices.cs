using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

internal static class RuntimeTestServices
{
    public static ICoreServices Core { get; } = new CuriousContraptions.Runtime.CoreServices();
    public static IBundleServices Bundle { get; } = new BundleServices();
    public static IAnimationServices Animation { get; } = new AnimationServices();
    public static PlacementPose Placement { get; } = new(Core.Geometry, Bundle);
    public static MachineWorld CreateWorld() => new() { Core = Core, Bundle = Bundle, Registry = new(Core.Presentation, Bundle), Placement = Placement, Diagnostics = Core.CreateDiagnostics() };
}
