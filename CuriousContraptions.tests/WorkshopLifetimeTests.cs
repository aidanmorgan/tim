using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class WorkshopLifetimeTests(NativeSceneFixture godot)
{
    public enum CollectionMode { Natural, Forced }
    [Theory]
    [InlineData(CollectionMode.Natural)]
    [InlineData(CollectionMode.Forced)]
    public void RepeatedSelectionUndoRetainsNativeHandlesUnderCollectionPressure(CollectionMode mode)
    {
        if(!Enum.IsDefined(mode))throw new ArgumentOutOfRangeException(nameof(mode));
        const int iterations=48;
        const int pressureBytes=4*1024*1024;
        var interactions=new WorkshopInteractionTests(godot);
        for(var iteration=0;iteration<iterations;iteration++)
        {
            var pressure=new byte[pressureBytes];
            pressure[0]=(byte)iteration;
            if(mode==CollectionMode.Forced)
            {
                GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,true);
                GC.WaitForPendingFinalizers();
            }
            interactions.SelectionAndSmallPointerJitterDoNotMovePartOrConsumeUndo(iteration%3-1);
            GC.KeepAlive(pressure);
        }
        godot.Engine.Iteration();
    }

    private readonly record struct CatalogueId(string Value);
    private readonly record struct ResourcePath(string Value);
    private static readonly CatalogueId Ramp=new("ramp");
    private static readonly ResourcePath WorkshopScene=new("res://scenes/workshop.tscn");
    private const string PartKindMetadata="part_kind";
    private static IEnumerable<T> Descendants<T>(Node root) where T:Node
    {
        for(var i=0;i<root.GetChildCount();i++)
        {
            var child=root.GetChild(i);
            if(child is T match)yield return match;
            foreach(var nested in Descendants<T>(child))yield return nested;
        }
    }
    public enum CollectionBoundary { BeforeUndo, BeforeSceneFree }
    [Theory]
    [InlineData(CollectionBoundary.BeforeUndo)]
    [InlineData(CollectionBoundary.BeforeSceneFree)]
    public void CollectionAtNativeReplacementBoundaryPreservesSelectionUndo(CollectionBoundary boundary)
    {
        if(!Enum.IsDefined(boundary))throw new ArgumentOutOfRangeException(nameof(boundary));
        for(var iteration=0;iteration<24;iteration++)
        {
            var scene=GD.Load<PackedScene>(WorkshopScene.Value).Instantiate<Workshop>();
            godot.Tree.Root.AddChild(scene);
            try
            {
                Descendants<Button>(scene).Single(button=>button.HasMeta(PartKindMetadata)&&
                    button.GetMeta(PartKindMetadata).AsString()==Ramp.Value).EmitSignal(Button.SignalName.Pressed);
                var camera=Assert.Single(Descendants<Camera3D>(scene));
                var pointer=camera.UnprojectPosition(new(-2,3,0));
                scene._UnhandledInput(new InputEventMouseButton {ButtonIndex=MouseButton.Left,Pressed=true,Position=pointer});
                var ramp=Assert.Single(scene.World.Parts,part=>!part.Locked);
                var original=ramp.Transform;
                scene._UnhandledInput(new InputEventKey {Keycode=Key.Escape,Pressed=true});
                scene._UnhandledInput(new InputEventMouseButton {ButtonIndex=MouseButton.Left,Pressed=true,Position=pointer});
                scene._UnhandledInput(new InputEventMouseMotion {Position=pointer+new Vector2(1,1)});
                scene._Input(new InputEventMouseButton {ButtonIndex=MouseButton.Left,Pressed=false,Position=pointer});
                Assert.Equal(original,ramp.Transform);
                if(boundary==CollectionBoundary.BeforeUndo)Collect();
                scene._UnhandledInput(new InputEventKey {Keycode=Key.Z,CtrlPressed=true,Pressed=true});
                Assert.DoesNotContain(scene.World.Parts,part=>!part.Locked);
                if(boundary==CollectionBoundary.BeforeSceneFree)Collect();
            }
            finally{scene.Free();}
        }
        godot.Engine.Iteration();
    }
    private static void Collect()
    {
        GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,true);
        GC.WaitForPendingFinalizers();
    }
}
