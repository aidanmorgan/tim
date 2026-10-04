using Godot;
using CuriousContraptions.Gpu;
using System;
using System.Linq;

namespace CuriousContraptions;

public partial class Workshop
{
    private MachinePart? _preview;
    private Window? _cameraFocusWindow;
    private static CanonicalRotation CaptureEditorRotation(Quaternion input)
    {
        var q = input.Normalized();
        var value = new CanonicalRotation((Half)q.X, (Half)q.Y, (Half)q.Z, (Half)q.W);
        value.Validate();
        return value;
    }

    private CanonicalRotation _previewOrientation = CanonicalRotation.Identity;
    private Vector3 _grabOffset;
    private Node3D _workGrid = null!;
    private OptionButton _layers = null!;
    private SpinBox _layerDepth = null!;
    private Label _buildHelp = null!;
    private bool _buildView, _updatingLayers, _lifting;
    private Metres _placementHeight = new((Half)3);
    private Vector2 _liftStartMouse, _liftScreenAxis;
    private Vector3 _liftStartPosition;
    private Control _partTools = null!;
    private PlacementShadows _placementShadows = null!;
    private RotationGizmo _rotationGizmo = null!;
    private bool _gizmoUndoPending;
    private Button _moveModeButton = null!, _rotateModeButton = null!, _resizeModeButton = null!;

    private void MakeGuidance()
    {
        _workGrid = new Node3D { Name = "ActiveLayerGrid" };
        AddChild(_workGrid);
        _placementShadows = new PlacementShadows();
        AddChild(_placementShadows);
        _rotationGizmo = new RotationGizmo();
        AddChild(_rotationGizmo);
        _optionsContents.AddChild(Text("Camera", 14));
        var views = new HBoxContainer();
        views.AddChild(Button("↶ View", () => OrbitQuarter(-1)));
        views.AddChild(Button("View ↷", () => OrbitQuarter(1)));
        _optionsContents.AddChild(views);
        var zoom = new HBoxContainer();
        zoom.AddChild(Button("Zoom +", () => { _zoom = Mathf.Max(8, _zoom - 1); UpdateCamera(); }));
        zoom.AddChild(Button("Zoom −", () => { _zoom = Mathf.Min(22, _zoom + 1); UpdateCamera(); }));
        _optionsContents.AddChild(zoom);
        _optionsContents.AddChild(Button("Reset camera", () => SetBuildView(false)));
        _buildHelp = Paragraph("Right-drag to look around. Scroll to zoom.", 13, Muted, new(330, 32));
        _optionsContents.AddChild(_buildHelp);
        var advanced = new VBoxContainer { Visible = false };
        _optionsContents.AddChild(Button("Fine rotate", () => advanced.Visible = !advanced.Visible));
        _optionsContents.AddChild(advanced);
        AddRotationRow(advanced, "Tip", "X", Vector3.Right);
        AddRotationRow(advanced, "Turn", "Y", Vector3.Up);
        AddRotationRow(advanced, "Tilt", "Z", Vector3.Back);
        var quick = new HBoxContainer();
        quick.AddChild(Button("↶ Tilt", () => Rotate(new(0, 0, 5))));
        quick.AddChild(Button("Tilt ↷", () => Rotate(new(0, 0, -5))));
        quick.AddChild(Button("Turn 90°", () => Rotate(new(0, -90, 0))));
        advanced.AddChild(quick);
        var height = new HBoxContainer();
        height.AddChild(Text("Height", 14));
        height.AddChild(Button("↑", () => LiftStep(.5f)));
        height.AddChild(Button("↓", () => LiftStep(-.5f)));
        advanced.AddChild(height);
        var modes = new HBoxContainer();
        modes.AddChild(Button("Front view", () => SetBuildView(true)));
        modes.AddChild(Button("3D view", () => SetBuildView(false)));
        advanced.AddChild(modes);
        _layers = new OptionButton { Name = "LayerPicker", CustomMinimumSize = new(190, 36) };
        _layers.ItemSelected += index => SetLayer((float)_layers.GetItemMetadata((int)index).AsDouble());
        advanced.AddChild(_layers);
        var row = new HBoxContainer();
        row.AddChild(Text("Depth", 14));
        _layerDepth = new SpinBox { Name = "LayerDepth", MinValue = -4, MaxValue = 4, Step = .1, CustomMinimumSize = new(100, 36) };
        _layerDepth.ValueChanged += value => { if (!_updatingLayers) SetLayer((float)value); };
        row.AddChild(_layerDepth);
        advanced.AddChild(row);
        advanced.AddChild(Button("Move selected here", MoveSelectedToLayer));

        var contextual = new VBoxContainer();
        _partDockContents.AddChild(contextual);
        _partTools = contextual;
        _partTools.Name = "PartTools";
        _partTools.Visible = false;
        var actions = new HBoxContainer();
        _moveModeButton = Button("Move mode", () => SetGizmoMode(true));
        _rotateModeButton = Button("Rotate mode", () => SetGizmoMode(false));
        _moveModeButton.ToggleMode = _rotateModeButton.ToggleMode = true;
        _rotateModeButton.ButtonPressed = true;
        actions.AddChild(_moveModeButton);
        actions.AddChild(_rotateModeButton);
        _resizeModeButton = Button("Resize mode", () =>
        {
            if (_selected is not IResizablePart || _selected.Locked) return;
            EndGizmo(true);
            _dragging = _lifting = false;
            _rotationGizmo.SetResizeMode();
        });
        _resizeModeButton.ToggleMode = true;
        _resizeModeButton.Visible = false;
        actions.AddChild(_resizeModeButton);
        var lift = Button("↕ Lift", () => { });
        lift.TooltipText = "";
        lift.GuiInput += input =>
        {
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
                BeginLift(GetViewport().GetMousePosition());
        };
        advanced.AddChild(lift);
        _removeButton = Button("Remove", DeleteSelected);
        actions.AddChild(_removeButton);
        contextual.AddChild(actions);
    }

    private void SetGizmoMode(bool move)
    {
        EndGizmo(true);
        _dragging = _lifting = false;
        _rotationGizmo.SetMoveMode(move);
        _moveModeButton.ButtonPressed = move;
        _rotateModeButton.ButtonPressed = !move;
        _rotationGizmo.Follow(_selected, !_inRun && _tool is null, _camera);
    }

    private void AddRotationRow(VBoxContainer parent, string title, string explanation, Vector3 axis)
    {
        var row = new HBoxContainer();
        var label = Text(title, 14);
        label.CustomMinimumSize = new(55, 0);

        row.AddChild(label);
        var decrease = Button(title + " −", () => Rotate(axis * -5));
        var increase = Button(title + " +", () => Rotate(axis * 5));
        decrease.TooltipText = "";
        increase.TooltipText = "";
        row.AddChild(decrease);
        row.AddChild(increase);
        parent.AddChild(row);
    }

    private static bool PlacementInside(Vector3 point) =>
        float.IsFinite(point.X) && float.IsFinite(point.Y) && float.IsFinite(point.Z) &&
        point.Z >= -4 && point.Z <= 4 &&
        point.X >= -7 && point.X <= 7 && point.Y >= 0 && point.Y <= 9;

    private Vector3 ClampPlacement(Vector3 point, float fixedAxis)
    {
        var result = new Vector3(Mathf.Clamp(point.X, -7, 7), Mathf.Clamp(point.Y, 0, 9),
            Mathf.Clamp(point.Z, -4, 4)).Snapped(Vector3.One * .1f);
        if (_buildView) result.Z = fixedAxis; else result.Y = fixedAxis;
        return result;
    }

    private void SetBuildView(bool build)
    {
        _buildView = build;
        _cameraPan = Vector3.Zero;
        ClearCameraMotion();
        _azimuth = build ? 0 : .6f;
        _elevation = build ? 0 : .48f;
        _zoom = 13.8f;
        _orbiting = _dragging = _lifting = false;
        UpdateCamera();
        RefreshLayerAppearance();
    }

    // Selecting a layer never silently moves the selected part.
    private void SetLayer(float depth)
    {
        if (!CanEdit) return;
        _depth = new((Half)(Mathf.Clamp(depth, -4, 4)));
        _depthText.Text = "Drag to slide · Lift for height";
        _workGrid.Position = new(0, 0, (float)_depth.Value - .03f);
        _updatingLayers = true;
        _layerDepth.Value = (float)_depth.Value;
        _updatingLayers = false;
        RefreshLayers();
        RefreshLayerAppearance();
    }

    private void RefreshLayers()
    {
        if (_layers == null) return;
        _updatingLayers = true;
        _layers.Clear();
        var depths = World.Parts.Select(p => Mathf.Snapped(p.Position.Z, .1f)).Append(0).Append((float)_depth.Value)
            .Distinct().OrderBy(z => z).ToArray();
        for (var i = 0; i < depths.Length; i++)
        {
            var depth = depths[i];
            var count = World.Parts.Count(p => Mathf.Abs(p.Position.Z - depth) < .05f);
            var location = depth < -.05f ? "Back" : depth > .05f ? "Front" : "Middle";
            _layers.AddItem($"{location} {depth:+0.0;-0.0;0.0} m · {count} parts");
            _layers.SetItemMetadata(i, depth);
            if (Mathf.Abs(depth - (float)_depth.Value) < .05f) _layers.Select(i);
        }
        _updatingLayers = false;
    }

    private void MoveSelectedToLayer()
    {
        if (!CanEdit || _selected is not { Locked: false })
        { _status.Text = "Select a movable part, choose a layer, then move it here."; return; }
        PushUndo();
        _selected.Position = new(_selected.Position.X, _selected.Position.Y, (float)_depth.Value);
        CommitSelected();
        
        RefreshLayers();
        RefreshLayerAppearance();
        _status.Text = "Moved to this layer. Undo returns it to its previous depth.";
    }

    private void RefreshLayerAppearance()
    {
        if (_workGrid == null) return;
        _workGrid.Visible = false;
        _layers.Disabled = _inRun;
        _layerDepth.Editable = !_inRun;
        _buildHelp.Text = _inRun ? "Watch your machine work" :
            _buildView ? "Front view · drag up/down or sideways" : "WASD moves · Q/E turns the camera · drag rings to rotate parts";
        foreach (var part in World.Parts)
        {
            var fade = !_inRun && _buildView && Mathf.Abs(part.Position.Z - (float)_depth.Value) > .26f;
            foreach (var mesh in part.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
                mesh.Transparency = fade ? .75f : 0;
            part.SetSelected(part == _selected);
        }
    }

    private void CreatePreview(WorkshopPartKind kind)
    {
        _preview = World.Registry.Create(kind);
        _preview.Position = new(0, 2, (float)_depth.Value);
        AddChild(_preview); // Deliberately not a World part: no inventory or physics effects.
        foreach (var mesh in _preview.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
            mesh.Transparency = .5f;
        _preview.Visible = false;
    }

    private void ClearPreview()
    {
        if (_preview == null) return;
        RemoveChild(_preview);
        _preview.Free();
        _preview = null;
    }

    private void CancelTool()
    {
        _linkSource = null;
        _tool = null;
        _dragging = _orbiting = _lifting = false;
        ClearPreview();
        Select(null);
        RefreshLayerAppearance();
        if (_status != null) _status.Text = "Choose a part, or click a placed part to adjust it.";
    }

    public override void _Process(double delta)
    {
        PresentUiAnimations(delta);
        PanCamera((float)delta);
        var target = _preview ?? _selected;
        _cancelButton.Visible = !_inRun && (_tool is not null || _linkSource is not null);
        _connectionChoices.Visible = CanEdit && (_selected is not null || _linkSource is not null);
        _removeButton.Visible = _preview == null && _selected is { Locked: false };
        _partTools.Visible = !_inRun && target is { Locked: false } && !_rotationGizmo.Dragging && !_optionsPanel.Visible;
        _rotationGizmo.Follow(_selected, !_inRun && _tool is null, _camera);
        _placementShadows.Follow(World.Parts, _preview, _selected, !_inRun);
        _detail.Visible = target != null;
        _time.Visible = _inRun;
        _resizeModeButton.Visible = _preview == null && _selected is IResizablePart && !_selected.Locked;
        _resizeModeButton.ButtonPressed = _rotationGizmo.ResizeMode;
        _moveModeButton.ButtonPressed = _rotationGizmo.MoveMode;
        _rotateModeButton.ButtonPressed = !_rotationGizmo.MoveMode && !_rotationGizmo.ResizeMode;
        ResizePartsToolbox();
        if (_preview == null || _lifting || GetViewport().GuiGetHoveredControl() != null) return;
        var point = WorkPoint(GetViewport().GetMousePosition(), _buildView ? (float)_depth.Value : (float)_placementHeight.Value);
        var valid = !_inRun && GetViewport().GuiGetHoveredControl() == null &&
                    point is { } at && PlacementInside(at);
        _preview.Visible = valid;
        if (valid && point is { } position)
        {
            _preview.Quaternion = new((float)_previewOrientation.X, (float)_previewOrientation.Y, (float)_previewOrientation.Z, (float)_previewOrientation.W);
            _preview.Position = ClampPlacement(position, _buildView ? (float)_depth.Value : (float)_placementHeight.Value);
        }
    }

    private void OrbitQuarter(int direction)
    {
        _buildView = false;
        _dragging = _lifting = false;
        _azimuth += direction * Mathf.Pi / 4;
        _elevation = Mathf.Max(.35f, _elevation);
        UpdateCamera();
        RefreshLayerAppearance();
    }

    private void LiftStep(float amount)
    {
        if (!CanEdit) return;
        var target = _preview ?? _selected;
        if (target is not { Locked: false }) return;
        if (_preview == null) PushUndo();
        target.Position = new(target.Position.X, Mathf.Clamp(target.Position.Y + amount, 0, 9), target.Position.Z);
        _placementHeight = new((Half)(target.Position.Y));
        if (_preview == null) CommitSelected();
        
    }

    private void BeginLift(Vector2 screen)
    {
        if (!CanEdit) return;
        var target = _preview ?? _selected;
        if (target is not { Locked: false }) return;
        if (_preview == null) PushUndo();
        _lifting = true;
        _dragging = _orbiting = false;
        _liftStartMouse = screen;
        _liftStartPosition = target.Position;
        _liftScreenAxis = _camera.UnprojectPosition(target.Position + Vector3.Up) - _camera.UnprojectPosition(target.Position);
    }

    private void ClearCameraMotion() => _cameraKeys.Clear();

    public override void _EnterTree()
    {
        _cameraFocusWindow = GetWindow();
        _cameraFocusWindow.FocusExited += ClearCameraMotion;
    }

    public override void _ExitTree()
    {
        _workshopUiRemoved = true;
        _gpuPending = true;
        RemoveUiAnimations();
        ClearCameraMotion();
        if (_cameraFocusWindow is { } window)
        {
            _cameraFocusWindow = null;
            if (GodotObject.IsInstanceValid(window)) window.FocusExited -= ClearCameraMotion;
        }
    }

    private void PanCamera(float delta)
    {
        if (_cameraKeys.Count == 0 || _rotationGizmo.Dragging || _dragging || _lifting ||
            GetViewport().GuiGetFocusOwner() is LineEdit or TextEdit) return;
        var step = Mathf.Clamp(delta, 0, .1f);
        var turn = (_cameraKeys.Contains(Key.Q) ? 1 : 0) - (_cameraKeys.Contains(Key.E) ? 1 : 0);
        var direction = new Vector2(
            (_cameraKeys.Contains(Key.D) ? 1 : 0) - (_cameraKeys.Contains(Key.A) ? 1 : 0),
            (_cameraKeys.Contains(Key.W) ? 1 : 0) - (_cameraKeys.Contains(Key.S) ? 1 : 0));
        if (turn == 0 && direction == Vector2.Zero) return;
        if (turn != 0)
        {
            // Match the existing orbit controls; Q looks left and E looks right.
            _azimuth = Mathf.Wrap(_azimuth + turn * 1.2f * step, -Mathf.Pi, Mathf.Pi);
            if (_buildView)
            {
                _buildView = false;
                _elevation = Mathf.Max(.35f, _elevation);
                RefreshLayerAppearance();
            }
        }
        // FPS-style movement stays on the ground plane, relative to the current
        // heading. W goes forward rather than raising the camera along screen-up.
        var right = new Vector3(Mathf.Cos(_azimuth), 0, -Mathf.Sin(_azimuth));
        var forward = new Vector3(-Mathf.Sin(_azimuth), 0, -Mathf.Cos(_azimuth));
        direction = direction.Normalized(); // Diagonals must not move faster.
        var offset = (right * direction.X + forward * direction.Y) * (_zoom * .4f * step);
        _cameraPan = (_cameraPan + offset).Clamp(Vector3.One * -12, Vector3.One * 12);
        UpdateCamera();
    }



    private void EndGizmo(bool cancel)
    {
        if (_rotationGizmo == null) return;
        var wasDragging = _rotationGizmo.Dragging;
        var snapMove = _rotationGizmo.Dragging && _rotationGizmo.MoveMode && !cancel;
        _rotationGizmo.End(cancel);
        if (wasDragging && !cancel) CommitSelected();
        if (cancel && _gizmoUndoPending && _undo.Count > 0)
        {
            _undo.RemoveAt(_undo.Count - 1);
            
        }
        _gizmoUndoPending = false;
        RefreshLayers();
        
    }

    // Release must be seen even when the pointer ends a drag over a UI panel.
    public override void _Input(InputEvent input)
    {
        if (input is InputEventKey { Pressed: false } released)
            _cameraKeys.Remove(released.PhysicalKeycode != Key.None ? released.PhysicalKeycode : released.Keycode);
        if (_rotationGizmo != null && _rotationGizmo.Dragging)
        {
            if (input is InputEventKey { Pressed: true, Keycode: Key.Escape })
            {
                EndGizmo(true);
                GetViewport().SetInputAsHandled();
                return;
            }
            if (input is InputEventMouseMotion rotate)
            {
                try
                {
                    if (_rotationGizmo.Drag(_camera, rotate.Position, rotate.ShiftPressed))
                    { GetViewport().SetInputAsHandled(); return; }
                }
                catch (ArgumentException error)
                {
                    EndGizmo(true);
                    _status.Text = "Placement unchanged: " + error.Message;
                    GetViewport().SetInputAsHandled(); return;
                }
            }
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
            {
                EndGizmo(false);
                GetViewport().SetInputAsHandled();
            }
        }
        if (input is InputEventMouseMotion motion && _lifting && CanEdit)
        {
            var target = _preview ?? _selected;
            if (target is { Locked: false } && _liftScreenAxis.LengthSquared() > 1)
            {
                var rise = (motion.Position - _liftStartMouse).Dot(_liftScreenAxis) / _liftScreenAxis.LengthSquared();
                target.Position = new(_liftStartPosition.X, Mathf.Clamp(_liftStartPosition.Y + rise, 0, 9), _liftStartPosition.Z);
                _placementHeight = new((Half)(target.Position.Y));
                
            }
        }
        if (input is InputEventMouseButton { Pressed: false } mouse)
        {
            if (mouse.ButtonIndex == MouseButton.Left)
            {
                if ((_dragging && _dragMoved) || _lifting)
                {
                    if (_preview == null) CommitSelected();
                }
                _dragging = _lifting = false;
            }
            if (mouse.ButtonIndex == MouseButton.Right) _orbiting = false;
        }
    }
}
