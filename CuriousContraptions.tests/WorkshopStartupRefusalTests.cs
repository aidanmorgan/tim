using Godot;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class WorkshopStartupRefusalTests(NativeSceneFixture godot)
{
    [Fact]
    public void ProviderRefusalDisablesInheritedCallbacksAndCanBeRemoved()
    {
        var scene = new RefusedWorkshop();
        godot.Tree.Root.AddChild(scene);
        var error = Assert.IsType<EngineProviderException>(scene.Refusal);
        Assert.Equal(EngineProviderRefusal.ProviderUnavailable, error.Reason);
        Assert.Equal(Node.ProcessModeEnum.Disabled, scene.ProcessMode);
        Assert.False(scene.CanProcess());
        var label = Assert.IsType<Label>(Assert.Single(scene.GetChildren()));
        Assert.Equal(error.Message, label.Text);
        Assert.True(label.IsVisibleInTree());
        godot.Errors.Expect(error.Message);
        for (var frame = 0; frame < 3; frame++) godot.Engine.Iteration();
        using (var key = new InputEventKey { Keycode = Key.Space, Pressed = true })
            scene.GetViewport().PushInput(key);
        godot.Engine.Iteration();
        Assert.Empty(godot.Errors.Drain());
        scene.Free();
        godot.Engine.Iteration();
        Assert.False(GodotObject.IsInstanceValid(scene));
        Assert.Empty(godot.Errors.Drain());
    }

    [Fact]
    public async Task DefaultObservationExplicitlyRefusesTheNativePlatform()
    {
        var scene = GD.Load<PackedScene>(NativeTestBoundary.WorkshopResource).Instantiate<Workshop>();
        godot.Tree.Root.AddChild(scene);
        try
        {
            await scene.Startup;
            var fault = Assert.IsType<WorkshopFaultDiagnostic>(Assert.Single(scene.Diagnostics.Snapshot()));
            Assert.Equal(FaultSite.Startup, fault.Site);
            Assert.Contains("Workshop observation requires a browser document.", fault.Detail, StringComparison.Ordinal);
            Assert.Contains(nameof(PlatformNotSupportedException), fault.Detail, StringComparison.Ordinal);
        }
        finally { scene.Free(); godot.Engine.Iteration(); }
        Assert.Empty(godot.Errors.Drain());
    }
}

// Exercise the production refusal operation with inherited processing/input/removal in a real scene tree.
public partial class RefusedWorkshop : Workshop
{
    public EngineProviderException? Refusal { get; private set; }
    public override void _Ready()
    {
        try { _ = RuntimeProviders.ConstructCore(null); }
        catch (EngineProviderException error) { Refusal = error; RefuseStartup(error); }
    }
}
