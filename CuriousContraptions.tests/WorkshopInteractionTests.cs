using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class WorkshopInteractionTests(HeadlessFixture godot)
{
    private Workshop Scene()
    {
        var scene = GD.Load<PackedScene>("res://scenes/workshop.tscn").Instantiate<Workshop>();
        godot.Tree.Root.AddChild(scene);
        return scene;
    }

    private static Button FindButton(Workshop scene, string text) =>
        scene.FindChildren("*", "Button", true, false).OfType<Button>().First(b => b.Text == text || (b.HasMeta("action_label") && b.GetMeta("action_label").AsString() == text));
    private static void Press(Workshop scene, string text) => FindButton(scene, text).EmitSignal(Button.SignalName.Pressed);
    private static Camera3D Camera(Workshop scene) => scene.FindChildren("*", "Camera3D", true, false).OfType<Camera3D>().Single();
    private static void Click(Workshop scene, Vector3 at) => scene._UnhandledInput(new InputEventMouseButton
    { ButtonIndex = MouseButton.Left, Pressed = true, Position = Camera(scene).UnprojectPosition(at) });
    private static void ChooseRamp(Workshop scene) =>
        scene.FindChildren("*", "Button", true, false).OfType<Button>()
            .First(b => b.HasMeta("action_label") && b.GetMeta("action_label").AsString().StartsWith("Ramp  ×")).EmitSignal(Button.SignalName.Pressed);
    private static MachinePart PlaceRamp(Workshop scene, Vector3 at)
    {
        ChooseRamp(scene);
        Click(scene, at);
        return scene.World.Parts.Single(p => !p.Locked);
    }

    [Fact]
    public void BeginnerControlsExistAndNoDifficultyNudgeIsExposed()
    {
        var scene = Scene();
        try
        {
            foreach (var text in new[] { "↶ View", "View ↷", "↕ Lift", "Menu", "Cancel / deselect", "Turn 90°" })
                Assert.NotNull(FindButton(scene, text));
            Assert.DoesNotContain(scene.FindChildren("*", "Button", true, false).OfType<Button>(),
                b => b.Text.Contains("nudge", StringComparison.OrdinalIgnoreCase));
            Assert.True(Camera(scene).Basis.Z.Y > .3f);
            Assert.False(((Control)scene.FindChild("LayerPicker", true, false)).IsVisibleInTree());
            Assert.NotNull(scene.FindChild("LayerPicker", true, false));
        }
        finally { scene.Free(); }
    }

    [Fact]
    public void PreviewDoesNotConsumeInventoryAndCanRotateBeforePlacement()
    {
        var scene = Scene();
        try
        {
            ChooseRamp(scene);
            Assert.Equal(2, scene.World.Parts.Count);
            Assert.NotNull(scene.FindChild("PlacementPreview", true, false));
            Press(scene, "Tilt ↷");
            Click(scene, new(-2, 3, 0));
            var ramp = scene.World.Parts.Single(p => !p.Locked);
            Assert.Equal(-5, ramp.RotationDegrees.Z, 3);
            Assert.Equal(new Vector3(-2, 3, 0), ramp.Position);
            Assert.Null(scene.FindChild("PlacementPreview", true, false));
        }
        finally { scene.Free(); }
    }

    [Fact]
    public void CancellingPreviewLeavesMachineAndInventoryUntouched()
    {
        var scene = Scene();
        try
        {
            ChooseRamp(scene);
            Press(scene, "Cancel / deselect");
            Assert.Equal(2, scene.World.Parts.Count);
            Assert.Null(scene.FindChild("PlacementPreview", true, false));
            Assert.Contains(scene.FindChildren("*", "Button", true, false).OfType<Button>(),
                b => b.HasMeta("action_label") && b.GetMeta("action_label").AsString() == "Ramp  × 2" && !b.Disabled);
        }
        finally { scene.Free(); }
    }

    [Fact]
    public void ChoosingLayerDoesNotMovePartUntilExplicitMoveAndUndoRestoresIt()
    {
        var scene = Scene();
        try
        {
            var ramp = PlaceRamp(scene, new(-2, 3, 0));
            var depth = (SpinBox)scene.FindChild("LayerDepth", true, false);
            depth.Value = 2;
            Assert.Equal(0, ramp.Position.Z);
            Press(scene, "Move selected here");
            Assert.Equal(2, ramp.Position.Z);
            Press(scene, "↺ Undo");
            Assert.Equal(0, scene.World.Parts.Single(p => !p.Locked).Position.Z);
        }
        finally { scene.Free(); }
    }

    [Fact]
    public void DragPreservesGrabOffsetAndMouseReleaseOverUiStopsIt()
    {
        var scene = Scene();
        try
        {
            var ramp = PlaceRamp(scene, new(-2, 3, 0));
            Click(scene, new(-1.7f, 3, 0));
            scene._UnhandledInput(new InputEventMouseMotion { Position = Camera(scene).UnprojectPosition(new(-1.2f, 3, 0)) });
            Assert.Equal(-1.5f, ramp.Position.X, 3);
            Assert.Equal(3, ramp.Position.Y, 3);
            scene._Input(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false });
            scene._UnhandledInput(new InputEventMouseMotion { Position = Camera(scene).UnprojectPosition(new(1, 3, 0)) });
            Assert.Equal(-1.5f, ramp.Position.X, 3);
        }
        finally { scene.Free(); }
    }

    [Fact]
    public void RunningDisablesLayerEditingAndResetRestoresIt()
    {
        var scene = Scene();
        try
        {
            Press(scene, "▶  Run machine");
            Assert.True(((OptionButton)scene.FindChild("LayerPicker", true, false)).Disabled);
            Assert.False(((SpinBox)scene.FindChild("LayerDepth", true, false)).Editable);
            Press(scene, "↶ Reset");
            Assert.False(((OptionButton)scene.FindChild("LayerPicker", true, false)).Disabled);
            Assert.True(((SpinBox)scene.FindChild("LayerDepth", true, false)).Editable);
        }
        finally { scene.Free(); }
    }
    [Fact]
    public void LiftChangesOnlyHeightAndUndoRestoresPlacement()
    {
        var scene = Scene();
        try
        {
            var ramp = PlaceRamp(scene, new(-2, 3, 1));
            Press(scene, "↑");
            Assert.Equal(new Vector3(-2, 3.5f, 1), ramp.Position);
            Press(scene, "↺ Undo");
            Assert.Equal(new Vector3(-2, 3, 1), scene.World.Parts.Single(p => !p.Locked).Position);
        }
        finally { scene.Free(); }
    }

    [Fact]
    public void LiftDragPreservesFootprintAndStopsOnReleaseOverUi()
    {
        var scene = Scene();
        try
        {
            var ramp = PlaceRamp(scene, new(-2, 3, 1));
            var start = scene.GetViewport().GetMousePosition();
            FindButton(scene, "↕ Lift").EmitSignal(Control.SignalName.GuiInput,
                new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true });
            var axis = Camera(scene).UnprojectPosition(ramp.Position + Vector3.Up) -
                       Camera(scene).UnprojectPosition(ramp.Position);
            scene._Input(new InputEventMouseMotion { Position = start + axis * 2 });
            Assert.True(ramp.Position.DistanceTo(new(-2, 5, 1)) < .001f);
            scene._Input(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false });
            scene._Input(new InputEventMouseMotion { Position = start + axis * 4 });
            Assert.Equal(5, ramp.Position.Y, 3);
        }
        finally { scene.Free(); }
    }

    [Fact]
    public void EveryFixtureHasThreeUnfilledProjectionsEvenWithoutASelection()
    {
        var scene = Scene();
        try
        {
            scene._Process(0);
            var shadows = (PlacementShadows)scene.FindChild("PlacementShadows", true, false);
            Assert.True(shadows.Visible);
            Assert.Equal(2, shadows.GetChildCount()); // Both locked fixtures, without selecting either.
            var ramp = PlaceRamp(scene, new(-2, 3, 1));
            ramp.RotationDegrees = new(0, 35, -20);
            scene._Process(0);
            var outlines = shadows.GetChildren().OfType<MeshInstance3D>().ToArray();
            Assert.Equal(3, outlines.Length);
            Assert.All(outlines, outline =>
            {
                Assert.IsType<ImmediateMesh>(outline.Mesh); // No wall boxes or silhouette meshes.
                Assert.Equal(3, outline.Mesh.GetSurfaceCount());
                Assert.Equal(GeometryInstance3D.ShadowCastingSetting.Off, outline.CastShadow);
            });
            var bounds = PlacementShadows.ArtworkBounds(ramp);
            Assert.True(bounds.Size.X > 0 && bounds.Size.Y > 0 && bounds.Size.Z > 0);
            Assert.Equal(-.42f, PlacementShadows.Project(bounds.Position, 0).Y);
            Assert.Equal(-4.65f, PlacementShadows.Project(bounds.Position, 1).Z);
            Assert.Equal(-7.65f, PlacementShadows.Project(bounds.Position, 2).X);
            Press(scene, "↑");
            scene._Process(0);
            var lifted = PlacementShadows.ArtworkBounds(ramp);
            Assert.Equal(bounds.Position.Y + .5f, lifted.Position.Y, 3);
            Assert.Equal(bounds.Position.X, lifted.Position.X, 3);
            Assert.Equal(bounds.Position.Z, lifted.Position.Z, 3);
            Press(scene, "Cancel / deselect");
            scene._Process(0);
            Assert.True(shadows.Visible);
            Assert.Equal(3, shadows.GetChildCount());
            Assert.Equal(3, scene.World.Parts.Count);
        }
        finally { scene.Free(); }
    }

    [Fact]
    public void ProjectionCleanupTracksPreviewCancellationAndPartRemoval()
    {
        var scene = Scene();
        try
        {
            ChooseRamp(scene);
            scene._Process(0);
            var shadows = (PlacementShadows)scene.FindChild("PlacementShadows", true, false);
            Assert.Equal(3, shadows.GetChildCount());
            Press(scene, "Cancel / deselect");
            scene._Process(0);
            Assert.Equal(2, shadows.GetChildCount());
            PlaceRamp(scene, new(-2, 3, 1));
            scene._Process(0);
            Assert.Equal(3, shadows.GetChildCount());
            Press(scene, "Remove");
            scene._Process(0);
            Assert.Equal(2, shadows.GetChildCount());
        }
        finally { scene.Free(); }
    }

    [Fact]
    public void PlacementShadowsHideDuringRunAndRebuildAfterUndo()
    {
        var scene = Scene();
        try
        {
            PlaceRamp(scene, new(-2, 3, 1));
            scene._Process(0);
            var shadows = (PlacementShadows)scene.FindChild("PlacementShadows", true, false);
            Press(scene, "▶  Run machine");
            scene._Process(0);
            Assert.False(shadows.Visible);
            Press(scene, "↶ Reset");
            Click(scene, new(-2, 3, 1));
            scene._Process(0);
            Assert.True(shadows.Visible);
            Press(scene, "↑");
            Press(scene, "↺ Undo");
            Click(scene, new(-2, 3, 1));
            scene._Process(0);
            Assert.True(shadows.Visible);
        }
        finally { scene.Free(); }
    }

    [Theory]
    [InlineData("Tip", 0)]
    [InlineData("Turn", 1)]
    [InlineData("Tilt", 2)]
    public void PlacedPartsRotateBothDirectionsOnEveryAxisAndUndo(string control, int axis)
    {
        var scene = Scene();
        try
        {
            var ramp = PlaceRamp(scene, new(-2, 3, 1));
            var direction = axis == 0 ? Vector3.Right : axis == 1 ? Vector3.Up : Vector3.Back;
            Press(scene, control + " +");
            var expected = new Quaternion(direction, Mathf.DegToRad(5));
            Assert.True(Mathf.Abs(ramp.Quaternion.Dot(expected)) > .99999f);
            Assert.Equal(new Vector3(-2, 3, 1), ramp.Position);
            Press(scene, control + " −");
            Assert.True(Mathf.Abs(ramp.Quaternion.Dot(Quaternion.Identity)) > .99999f);
            Press(scene, "↺ Undo");
            ramp = scene.World.Parts.Single(p => !p.Locked);
            Assert.True(Mathf.Abs(ramp.Quaternion.Dot(expected)) > .99999f);
        }
        finally { scene.Free(); }
    }

    [Fact]
    public void MixedAxisRotationsWorkThroughNinetyDegreesAndSurviveRunReset()
    {
        var scene = Scene();
        try
        {
            var ramp = PlaceRamp(scene, new(-2, 3, 1));
            for (var step = 0; step < 18; step++) Press(scene, "Tip +");
            Press(scene, "Turn +");
            Press(scene, "Tilt −");
            var expected = new Quaternion(Vector3.Back, Mathf.DegToRad(-5)) *
                           new Quaternion(Vector3.Up, Mathf.DegToRad(5)) *
                           new Quaternion(Vector3.Right, Mathf.Pi / 2);
            Assert.True(Mathf.Abs(ramp.Quaternion.Dot(expected)) > .99999f);
            Press(scene, "▶  Run machine");
            Press(scene, "Tip +");
            Press(scene, "↶ Reset");
            ramp = scene.World.Parts.Single(p => !p.Locked);
            Assert.True(Mathf.Abs(ramp.Quaternion.Dot(expected)) > .99999f);
        }
        finally { scene.Free(); }
    }

    [Fact]
    public void PreviewKeepsAllThreeRotationsWhenPlacedAndFixedPartsCannotRotate()
    {
        var scene = Scene();
        try
        {
            var fixture = scene.World.Parts.First();
            Click(scene, fixture.Position);
            var original = fixture.Quaternion;
            foreach (var control in new[] { "Tip +", "Turn +", "Tilt +" }) Press(scene, control);
            Assert.Equal(original, fixture.Quaternion);
            ChooseRamp(scene);
            foreach (var control in new[] { "Tip +", "Turn +", "Tilt +" }) Press(scene, control);
            var preview = (MachinePart)scene.FindChild("PlacementPreview", true, false);
            var rotation = preview.Quaternion;
            Click(scene, new(-2, 3, 1));
            Assert.True(Mathf.Abs(scene.World.Parts.Single(p => !p.Locked).Quaternion.Dot(rotation)) > .99999f);
        }
        finally { scene.Free(); }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void GizmoSphereDragsRotateEachAxisAndUndoAsOneGesture(int axis)
    {
        var scene = Scene();
        try
        {
            var ramp = PlaceRamp(scene, new(-2, 3, 1));
            scene._Process(0);
            var gizmo = (RotationGizmo)scene.FindChild("RotationGizmo", true, false);
            Assert.True(gizmo.Visible);
            var start = gizmo.HandlePosition(axis);
            Click(scene, start);
            Assert.True(gizmo.Dragging);
            Assert.Equal(axis, gizmo.ActiveAxis);
            var normal = axis == 0 ? Vector3.Right : axis == 1 ? Vector3.Up : Vector3.Back;
            for (var step = 1; step <= 4; step++)
            {
                var point = ramp.Position + new Quaternion(normal, Mathf.DegToRad(step * 10)) * (start - ramp.Position);
                scene._Input(new InputEventMouseMotion { Position = Camera(scene).UnprojectPosition(point) });
            }
            var expected = new Quaternion(normal, Mathf.DegToRad(40));
            Assert.True(Mathf.Abs(ramp.Quaternion.Dot(expected)) > .9999f);
            Assert.Equal(new Vector3(-2, 3, 1), ramp.Position);
            scene._Input(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false });
            Assert.False(gizmo.Dragging);
            Press(scene, "↺ Undo");
            Assert.True(Mathf.Abs(scene.World.Parts.Single(p => !p.Locked).Quaternion.Dot(Quaternion.Identity)) > .9999f);
        }
        finally { scene.Free(); }
    }

    [Fact]
    public void GizmoSnapsCancelsAndHandlesEdgeOnRingsWithoutInvalidRotations()
    {
        var scene = Scene();
        try
        {
            var ramp = PlaceRamp(scene, new(-2, 3, 1));
            scene._Process(0);
            var gizmo = (RotationGizmo)scene.FindChild("RotationGizmo", true, false);
            var start = gizmo.HandlePosition(2);
            Click(scene, start);
            var point = ramp.Position + new Quaternion(Vector3.Back, Mathf.DegToRad(22)) * (start - ramp.Position);
            scene._Input(new InputEventMouseMotion { Position = Camera(scene).UnprojectPosition(point), ShiftPressed = true });
            Assert.True(Mathf.Abs(ramp.Quaternion.Dot(new Quaternion(Vector3.Back, Mathf.DegToRad(15)))) > .9999f);
            scene._Input(new InputEventKey { Pressed = true, Keycode = Key.Escape });
            Assert.False(gizmo.Dragging);
            Assert.True(Mathf.Abs(ramp.Quaternion.Dot(Quaternion.Identity)) > .9999f);
            Press(scene, "Front view");
            scene._Process(0);
            start = gizmo.HandlePosition(0);
            Click(scene, start);
            Assert.Equal(0, gizmo.ActiveAxis);
            scene._Input(new InputEventMouseMotion { Position = Camera(scene).UnprojectPosition(start) + new Vector2(0, -30) });
            Assert.True(ramp.Quaternion.IsFinite());
            Assert.True(Mathf.Abs(ramp.Quaternion.Dot(Quaternion.Identity)) < .9999f);
            scene._Input(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false });
            var end = ramp.Quaternion;
            scene._Input(new InputEventMouseMotion { Position = Vector2.Zero });
            Assert.Equal(end, ramp.Quaternion);
            Press(scene, "▶  Run machine");
            scene._Process(0);
            Assert.False(gizmo.Visible);
        }
        finally { scene.Free(); }
    }

    [Fact]
    public void ToolbarUsesVisibleSvgTexturesAndTooltipsAreShortAndDelayed()
    {
        var scene = Scene();
        try
        {
            foreach (var action in new[] { "▶  Run machine", "↺ Undo", "↕ Lift", "↑", "↓", "Connect", "Save", "Load", "Remove", "Zoom +" })
            {
                var button = FindButton(scene, action);
                Assert.NotNull(button.Icon);
                Assert.True(button.Icon.GetWidth() > 0);
                Assert.False(button.Icon.GetImage().IsInvisible());
                Assert.Empty(button.Text);
            }
            Assert.True(ProjectSettings.GetSetting("gui/timers/tooltip_delay_sec").AsDouble() >= 1);
            Assert.All(scene.FindChildren("*", "Button", true, false).OfType<Button>(),
                button => Assert.True(button.TooltipText.Length < 80));
            var ramp = PlaceRamp(scene, new(-2, 3, 1));
            scene._Process(0);
            Assert.False(FindButton(scene, "Tip +").IsVisibleInTree());
            Press(scene, "Menu");
            Press(scene, "Fine rotate");
            Assert.True(FindButton(scene, "Tip +").IsVisibleInTree());
        }
        finally { scene.Free(); }
    }

    [Fact]
    public void DefaultInterfaceShowsOnlyPrimaryActionsAndMenuKeepsSecondaryTools()
    {
        var scene = Scene();
        try
        {
            scene._Process(0);
            var visible = scene.FindChildren("*", "Button", true, false).OfType<Button>()
                .Where(button => button.IsVisibleInTree()).ToArray();
            Assert.True(visible.Length <= 7, $"Too many default buttons: {visible.Length}");
            foreach (var action in new[] { "Save", "Load", "Zoom +", "↶ View", "Tip +", "Remove", "Connect" })
                Assert.False(FindButton(scene, action).IsVisibleInTree());
            Press(scene, "Menu");
            Assert.True(FindButton(scene, "Save").IsVisibleInTree());
            Press(scene, "Menu");
            PlaceRamp(scene, new(-2, 3, 1));
            scene._Process(0);
            Assert.True(FindButton(scene, "Move mode").IsVisibleInTree());
            Assert.True(FindButton(scene, "Rotate mode").IsVisibleInTree());
            Assert.True(FindButton(scene, "Remove").IsVisibleInTree());
            Assert.False(FindButton(scene, "Connect").IsVisibleInTree());
            Assert.False(FindButton(scene, "Tip +").IsVisibleInTree());
        }
        finally { scene.Free(); }
    }

    [Theory]
    [InlineData(Key.W)]
    [InlineData(Key.A)]
    [InlineData(Key.S)]
    [InlineData(Key.D)]
    public void WasdPansCameraWithoutRotatingPartsAndStopsOnRelease(Key key)
    {
        var scene = Scene();
        try
        {
            var ramp = PlaceRamp(scene, new(-2, 3, 1));
            scene._Process(0);
            var camera = Camera(scene);
            var before = camera.Position;
            var rotation = ramp.Quaternion;
            scene._UnhandledInput(new InputEventKey { Keycode = key, Pressed = true });
            scene._Process(.1);
            Assert.True(camera.Position.DistanceTo(before) > .1f);
            Assert.Equal(rotation, ramp.Quaternion);
            scene._Input(new InputEventKey { Keycode = key, Pressed = false });
            var stopped = camera.Position;
            scene._Process(.1);
            Assert.Equal(stopped, camera.Position);
            Press(scene, "Reset camera");
            Assert.True(camera.Position.DistanceTo(before) < .001f);
        }
        finally { scene.Free(); }
    }

    [Fact]
    public void PuzzleIntroductionAndHintAreCollapsedUntilRequested()
    {
        var scene = Scene();
        try
        {
            var introduction = (Label)scene.FindChild("PuzzleIntroduction", true, false);
            var hint = (Label)scene.FindChild("PuzzleHint", true, false);
            Assert.False(introduction.IsVisibleInTree());
            Assert.False(hint.IsVisibleInTree());
            Press(scene, "Goal");
            Assert.True(introduction.IsVisibleInTree());
            Assert.False(hint.IsVisibleInTree());
            Press(scene, "Show hint");
            Assert.True(hint.IsVisibleInTree());
            Press(scene, "Goal");
            Assert.False(introduction.IsVisibleInTree());
            Assert.False(hint.IsVisibleInTree());
        }
        finally { scene.Free(); }
    }


    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void MoveWidgetMovesOnlyChosenAxisAndSupportsCancelAndUndo(int axis)
    {
        var scene = Scene();
        try
        {
            var ramp = PlaceRamp(scene, new(-2, 3, 1));
            scene._Process(0);
            var gizmo = (RotationGizmo)scene.FindChild("RotationGizmo", true, false);
            Press(scene, "Move mode");
            scene._Process(0);
            Assert.True(gizmo.MoveMode);
            Assert.True(FindButton(scene, "Move mode").ButtonPressed);
            Assert.False(FindButton(scene, "Rotate mode").ButtonPressed);
            var start = ramp.Position;
            var rotation = ramp.Quaternion;
            var direction = axis == 0 ? Vector3.Right : axis == 1 ? Vector3.Up : Vector3.Back;
            var screen = Camera(scene).UnprojectPosition(gizmo.HandlePosition(axis));
            var step = Camera(scene).UnprojectPosition(start + direction) - Camera(scene).UnprojectPosition(start);
            scene._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = screen });
            Assert.Equal(axis, gizmo.ActiveAxis);
            scene._Input(new InputEventMouseMotion { Position = screen + step * 1.2f, ShiftPressed = true });
            Assert.True(ramp.Position.DistanceTo(start + direction * 1.2f) < .001f);
            Assert.Equal(rotation, ramp.Quaternion);
            scene._Input(new InputEventKey { Keycode = Key.Escape, Pressed = true });
            Assert.Equal(start, ramp.Position);
            scene._Process(0);
            screen = Camera(scene).UnprojectPosition(gizmo.HandlePosition(axis));
            scene._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = screen });
            scene._Input(new InputEventMouseMotion { Position = screen + step * 2 });
            scene._Input(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false });
            Assert.True(ramp.Position.DistanceTo(start + direction * 2) < .001f);
            Assert.False(gizmo.Dragging);
            Press(scene, "↺ Undo");
            Assert.Equal(start, scene.World.Parts.Single(p => !p.Locked).Position);
            Press(scene, "Rotate mode");
            Assert.False(gizmo.MoveMode);
        }
        finally { scene.Free(); }
    }


    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void MovementSnapAlignsOnlyChosenWorldCoordinateFromOffGridStart(int axis)
    {
        var scene = Scene();
        try
        {
            var ramp = PlaceRamp(scene, new(-2, 3, 1));
            ramp.Position += new Vector3(.031f, .037f, .043f);
            scene._Process(0);
            Press(scene, "Move mode");
            scene._Process(0);
            var gizmo = (RotationGizmo)scene.FindChild("RotationGizmo", true, false);
            var start = ramp.Position;
            var direction = axis == 0 ? Vector3.Right : axis == 1 ? Vector3.Up : Vector3.Back;
            var screen = Camera(scene).UnprojectPosition(gizmo.HandlePosition(axis));
            var step = Camera(scene).UnprojectPosition(start + direction) - Camera(scene).UnprojectPosition(start);
            scene._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = screen });
            scene._Input(new InputEventMouseMotion { Position = screen + step * .18f, ShiftPressed = true });
            var expected = start;
            expected[axis] = Mathf.Snapped(start[axis] + .18f, .1f);
            Assert.True(ramp.Position.DistanceTo(expected) < .0001f);
            for (var other = 0; other < 3; other++)
                if (other != axis) Assert.Equal(start[other], ramp.Position[other]);
            scene._Input(new InputEventKey { Keycode = Key.Escape, Pressed = true });
            Assert.Equal(start, ramp.Position);
        }
        finally { scene.Free(); }
    }

    [Fact]
    public void ActionButtonsAreIconOnlyAndEveryPartHasAVisiblePictogram()
    {
        var scene = Scene();
        try
        {
            Assert.All(scene.FindChildren("*", "Button", true, false).OfType<WorkshopButton>(), button =>
            {
                Assert.Empty(button.Text);
                Assert.Empty(button.TooltipText);
                Assert.NotNull(button.Icon);
                Assert.False(button.Icon.GetImage().IsInvisible());
                Assert.True(button.CustomMinimumSize.X >= 40);
            });
            foreach (var kind in scene.World.Registry.Definitions.Keys)
                Assert.False(WorkshopIcons.Pictogram(kind).GetImage().IsInvisible());
        }
        finally { scene.Free(); }
    }
}
