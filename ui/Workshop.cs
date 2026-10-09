using Godot;
using CuriousContraptions.Gpu;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace CuriousContraptions;

public partial class Workshop : Node3D
{
    private static readonly Color Navy = new("#293954"), Muted = new("#48556a"), Cream = new("#25344b"), Mint = new("#326537");
    public MachineWorld World { get; private set; } = null!;
    private Camera3D _camera = null!;
    private CanvasLayer _canvas = null!;
    private VBoxContainer _palette = null!;
    private ScrollContainer _paletteScroll = null!;
    private Label _title = null!, _task = null!, _status = null!, _detail = null!, _state = null!, _precisionText = null!, _depthText = null!, _time = null!, _hint = null!;
    private Button _run = null!, _cancelButton = null!, _removeButton = null!;
    private Control _optionsPanel = null!, _objectivePanel = null!;
    private Button _hintButton = null!;
    private Vector3 _cameraPan;
    private readonly HashSet<Key> _cameraKeys = new();
    private VBoxContainer _optionsContents = null!, _partDockContents = null!;
    private Button _menuButton = null!, _goalButton = null!;
    private HBoxContainer _mainActions = null!;
    private OptionButton _picker = null!;
    private HSlider _precision = null!;
    private CheckButton _friction = null!;
    private MachinePart? _selected;
    private WorkshopPartKind? _tool;
    private bool _dragging, _dragMoved, _orbiting, _inRun;
    private WorkshopSimulationPhase _displayedWorkshopPhase;
    private WorkshopTransportState _displayedTransportState = WorkshopTransportState.Ready;
    private Vector2 _dragPressScreen;
    private const float PartDragThresholdPixels = 6;
    private Metres _depth;
    private float _azimuth, _elevation, _zoom = 13.8f;
    private const int FreeWorkshopIndex = 1;
    private const int DelayedSignalIndex = 2;
    private const int DominoEffectIndex = 3;
    private GpuBodyId _nextId = new(1);
    private IReadOnlyDictionary<WorkshopPartKind, PartAllowance> _inventory = new Dictionary<WorkshopPartKind, PartAllowance>();
    private readonly List<WorkshopConstruction> _undo = new();
    private bool _gpuPending = true;
    private bool _workshopUiRemoved;
    private bool CanEdit => !_gpuPending && !_inRun && World.WorkshopPhase == WorkshopSimulationPhase.Building;

    public override async void _Ready()
    {
        World = new MachineWorld { Name = "Machine" };
        AddChild(World);
        MakeStage();
        MakeInterface();
        InitializeUiAnimations();
        MakeGuidance();
        _picker.AddItem("First principles");
        _picker.AddItem("Free workshop");
        _picker.AddItem("Wait for it");
        _picker.AddItem("The domino effect");
        PresentMode();
        _picker.Select(FreeWorkshopIndex);
        try { await World.InitializeWorkshop(); if (_workshopUiRemoved) return; _gpuPending = false; SetBuildUi(); RefreshPalette(); }
        catch (Exception error) { if (_workshopUiRemoved) return; _status.Text = error.Message; _state.Text = "GPU UNAVAILABLE"; }
    }

    private void MakeStage()
    {
        var environment = new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new("#91cbed"),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new("#fff5dd"), AmbientLightEnergy = .65f,
                TonemapMode = Godot.Environment.ToneMapper.Linear
            }
        };
        AddChild(environment);
        AddChild(new DirectionalLight3D
        {
            RotationDegrees = new(-38, -28, 0), LightColor = new("#fff0d6"),
            LightEnergy = 1.3f, ShadowEnabled = true, DirectionalShadowMaxDistance = 45
        });
        AddChild(new DirectionalLight3D { RotationDegrees = new(-10, 145, 0), LightColor = new("#b7ddea"), LightEnergy = .45f });
        var stage = new Node3D { Name = "Workbench" };
        AddChild(stage);
        PartArt.Box(stage, Workbench.Base.Half * 2, new("#b77c42"), Workbench.Base.At);
        PartArt.Box(stage, Workbench.Deck.Half * 2, new("#ead39b"), Workbench.Deck.At);
        // A toy workbench, not a permanent drafting grid.
        foreach (var x in new[] { -8f, 8f })
            foreach (var z in new[] { -4.5f, 4.5f })
                PartArt.Sphere(stage, .09f, new("#9c743f"), new(x, -.43f, z));
        _camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Near = .1f, Far = 100 };
        AddChild(_camera);
        UpdateCamera();
    }
    private void UpdateCamera()
    {
        var focus = new Vector3(0, 3.6f, 0) + _cameraPan;
        _camera.Position = focus + new Vector3(Mathf.Sin(_azimuth) * Mathf.Cos(_elevation), Mathf.Sin(_elevation), Mathf.Cos(_azimuth) * Mathf.Cos(_elevation)) * 26;
        _camera.LookAt(focus);
        _camera.Size = _zoom;
    }
    private static StyleBoxFlat Style(Color color, Color border = default)
    {
        var style = new StyleBoxFlat
        {
            BgColor = color, BorderColor = border,
            ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 12, ContentMarginBottom = 12
        };
        style.SetCornerRadiusAll(10);
        style.BorderColor = border.A > 0 ? border : new("#867961");
        style.SetBorderWidthAll(1);
        style.ShadowColor = new("#3e342a55");
        style.ShadowSize = 0;
        style.ShadowOffset = new(2, 3);
        return style;
    }
    private static Label Text(string text, int size = 16, Color? color = null)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color ?? Cream);
        return label;
    }
    private static Button Button(string text, Action action, bool accent = false, Texture2D? icon = null)
    {
        var button = new WorkshopButton { Text = text, CustomMinimumSize = new(40, 40), MouseDefaultCursorShape = Control.CursorShape.PointingHand };
        button.AddThemeFontSizeOverride("font_size", 15);
        button.AddThemeColorOverride("font_color", Navy);
        button.AddThemeColorOverride("font_hover_color", Navy);
        button.AddThemeColorOverride("font_pressed_color", Navy);
        button.AddThemeStyleboxOverride("normal", Style(accent ? new("#f7cb52") : new("#ffffff00")));
        button.AddThemeStyleboxOverride("hover", Style(accent ? new("#ffe38a") : new("#fff4d8")));
        button.AddThemeStyleboxOverride("pressed", Style(accent ? new("#dbae38") : new("#cbbd9b")));
        foreach (var state in new[] { "normal", "hover", "pressed" })
        {
            var style = (StyleBoxFlat)button.GetThemeStylebox(state);
            style.ContentMarginLeft = style.ContentMarginRight = 10;
            style.SetBorderWidthAll(0);
        }
        if (icon == null) WorkshopIcons.Apply(button, text);
        else
        {
            button.SetMeta("action_label", text);
            button.Text = "";
            button.Icon = icon;
            button.ExpandIcon = false;
            button.TooltipText = "";
            button.AddThemeConstantOverride("icon_max_width", 20);
        }
        button.Pressed += action;
        return button;
    }
    private VBoxContainer Panel(Vector2 at, Vector2 size)
    {
        var panel = new PanelContainer { Position = at, Size = size };
        panel.AddThemeStyleboxOverride("panel", Style(new("#fff8e9eb"), new("#c5bca8")));
        _canvas.AddChild(panel);
        var contents = new VBoxContainer();
        contents.AddThemeConstantOverride("separation", 10);
        panel.AddChild(contents);
        return contents;
    }
    private static Label Paragraph(string text, int size, Color color, Vector2 minimum)
    {
        var label = Text(text, size, color);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = minimum;
        return label;
    }
    private void MakeInterface()
    {
        _canvas = new CanvasLayer();
        AddChild(_canvas);
        var heading = Text("CURIOUS CONTRAPTIONS", 18);
        heading.Position = new(28, 23);
        _canvas.AddChild(heading);
        // Pointer-only: a focused picker would consume Space (ui_accept) before the Run/Reset shortcut reaches _UnhandledInput.
        _picker = new OptionButton { Name = WorkshopUiControlBoundary.NodeName(WorkshopUiControl.LevelPicker),
            Position = new(470, 24), Size = new(350, 42), FocusMode = Control.FocusModeEnum.None };
        _picker.AddThemeFontSizeOverride("font_size", 16);
        _picker.AddThemeColorOverride("font_color", Navy);
        _picker.AddThemeColorOverride("font_hover_color", Navy);
        _picker.AddThemeStyleboxOverride("normal", Style(new("#eee5cc")));
        _picker.ItemSelected += index => SelectModeFromPicker((int)index);
        _canvas.AddChild(_picker);
        _state = Text("BUILD MODE", 13, Mint);
        _state.Position = new(1050, 36);
        _state.Visible = false;
        _canvas.AddChild(_state);
        _menuButton = Button("Menu", () =>
        {
            _optionsPanel.Visible = !_optionsPanel.Visible;
            _objectivePanel.Visible = false;
        });
        _menuButton.Name = WorkshopUiControlBoundary.NodeName(WorkshopUiControl.Menu);
        _menuButton.Size = new(44, 44);
        _canvas.AddChild(_menuButton);
        var left = Panel(new(24, 94), new(216, 0));
        _partDockContents = left;
        left.AddChild(Text("PARTS", 11, Muted));
        _paletteScroll = new ScrollContainer
        {
            Name = "PartsScroll", CustomMinimumSize = new(180, 40),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        left.AddChild(_paletteScroll);
        _palette = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _palette.AddThemeConstantOverride("separation", 7);
        _paletteScroll.AddChild(_palette);
        _detail = Paragraph("Choose a part, then click the workbench.", 14, Muted, new(180, 0));
        left.AddChild(_detail);
        _connectionChoices = new VBoxContainer(); left.AddChild(_connectionChoices);
        _cancelButton = Button("Cancel / deselect", CancelTool);
        _cancelButton.Visible = false;
        left.AddChild(_cancelButton);
        var objective = Panel(new(1150, 94), new(266, 0));
        _objectivePanel = objective.GetParent<Control>();
        _objectivePanel.Name = "PuzzleInfo";
        _title = Paragraph("", 16, Navy, new(230, 0));
        objective.AddChild(_title);
        _goalButton = Button("Goal", () =>
        {
            _objectivePanel.Visible = !_objectivePanel.Visible;
            _task.Visible = _hintButton.Visible = _objectivePanel.Visible;
            _optionsPanel.Visible = false;
            if (!_objectivePanel.Visible) HideHint();
            _objectivePanel.Size = new(266, 0);
        });
        _goalButton.Name = WorkshopUiControlBoundary.NodeName(WorkshopUiControl.Goal);
        _goalButton.Size = new(44, 44);
        _canvas.AddChild(_goalButton);
        _task = Paragraph("", 14, Muted, new(230, 0));
        _task.Name = "PuzzleIntroduction";
        objective.AddChild(_task);
        _hintButton = Button("Show hint", ShowHint);
        _hintButton.Name = WorkshopUiControlBoundary.NodeName(WorkshopUiControl.ShowHint);
        objective.AddChild(_hintButton);
        _hint = Paragraph("", 14, Mint, new(230, 0));
        _hint.Name = WorkshopUiControlBoundary.NodeName(WorkshopUiControl.Hint);
        _hint.Visible = false;
        objective.AddChild(_hint);
        _objectivePanel.Visible = false;

        _mainActions = new HBoxContainer();
        _mainActions.AddThemeConstantOverride("separation", 8);
        _canvas.AddChild(_mainActions);
        _run = Button("▶  Run machine", ToggleRun, true);
        _run.Name = WorkshopUiControlBoundary.NodeName(WorkshopUiControl.Run);
        _run.CustomMinimumSize = new(56, 48);
        _mainActions.AddChild(_run);
        _mainActions.AddChild(Button("↺ Undo", Undo));
        _time = Text("00.00 s", 13, Muted);
        _mainActions.AddChild(_time);
        _status = Paragraph("Choose a part to get started.", 12, Muted, new(480, 28));
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.MouseFilter = Control.MouseFilterEnum.Ignore;
        _canvas.AddChild(_status);
        _success = Text("SOLVED!", 26, Mint); _success.Name = WorkshopUiControlBoundary.NodeName(WorkshopUiControl.Solved);
        _success.Position = new(850, 30); _canvas.AddChild(_success);
        // Secondary actions live in one scrollable menu, closed by default.
        var options = Panel(new(1020, 90), new(396, 680));
        _optionsPanel = options.GetParent<Control>();
        _optionsPanel.Name = "WorkshopMenu";
        var optionsScroll = new ScrollContainer { CustomMinimumSize = new(360, 650) };
        options.AddChild(optionsScroll);
        _optionsContents = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _optionsContents.AddThemeConstantOverride("separation", 10);
        optionsScroll.AddChild(_optionsContents);
        _optionsContents.AddChild(Text("WORKSHOP", 16));
        var files = new HBoxContainer();
        files.AddChild(Button("Save", Save));
        files.AddChild(Button("Load", LoadSave));
        files.AddChild(Button("↶ Reset", ResetRun));
        _optionsContents.AddChild(files);
        AddPlaybackControls(_optionsContents);
        _optionsContents.AddChild(Text("Difficulty", 14));
        _precisionText = Text("Balanced");
        _optionsContents.AddChild(_precisionText);
        _precision = new HSlider { MinValue = 0, MaxValue = 100, Value = 45 };
        _precision.ValueChanged += ChangePrecision;
        _optionsContents.AddChild(_precision);
        _optionsContents.AddChild(Text("Forgiving                         Precise", 12, Muted));
        _friction = new CheckButton { Text = "More surface friction" };
        _friction.AddThemeColorOverride("font_color", Navy);
        _friction.Toggled += _ => _status.Text = "The current GPU Workshop uses each ball’s declared material.";
        _optionsContents.AddChild(_friction);
        _depthText = Text("", 13, Muted);
        _depthText.Visible = false;
        _optionsContents.AddChild(_depthText);
        _optionsPanel.Visible = false;
        GetViewport().SizeChanged += ResizeInterface;
        ResizeInterface();
    }
    private void ResizeInterface()
    {
        var size = GetViewport().GetVisibleRect().Size;
        var factor = Mathf.Min(size.X / 1440, size.Y / 900);
        _canvas.Transform = new Transform2D(0, Vector2.One * factor, 0, Vector2.Zero);
        var layout = size / factor;
        _picker.Position = new(layout.X / 2 - 175, 24);
        _menuButton.Position = new(layout.X - 68, 24);
        _goalButton.Position = new(layout.X - 122, 24);
        _optionsPanel.Position = new(layout.X - 420, 94);
        _objectivePanel.Position = new(layout.X - 290, 94);
        _mainActions.Position = new(layout.X / 2 - 60, layout.Y - 88);
        _status.Position = new(layout.X / 2 - 240, layout.Y - 32);
        ResizePartsToolbox();
    }

    private void ResizePartsToolbox()
    {
        if (_paletteScroll == null || _partDockContents == null) return;
        var panel = _partDockContents.GetParent<PanelContainer>();
        var layoutHeight = GetViewport().GetVisibleRect().Size.Y / _canvas.Transform.Scale.Y;
        var children = _partDockContents.GetChildren().OfType<Control>().Where(c => c.Visible).ToArray();
        var chromeHeight = panel.GetThemeStylebox("panel").GetMinimumSize().Y +
            children.Where(c => c != _paletteScroll).Sum(c => c.GetCombinedMinimumSize().Y) +
            Mathf.Max(0, children.Length - 1) * _partDockContents.GetThemeConstant("separation");
        var available = Mathf.Max(40, layoutHeight - panel.Position.Y - 24 - chromeHeight);
        var height = Mathf.Clamp(_palette.GetCombinedMinimumSize().Y, 40, available);
        _paletteScroll.CustomMinimumSize = new(180, height);
        // Containers grow automatically but do not otherwise shrink after changing
        // from a full inventory to a small puzzle or hiding contextual controls.
        var targetHeight = height + chromeHeight;
        if (Mathf.Abs(panel.Size.Y - targetHeight) > .1f)
            panel.Size = new(panel.Size.X, targetHeight);
    }

    private void RefreshPalette()
    {
        foreach (var child in _palette.GetChildren()) { _palette.RemoveChild(child); child.QueueFree(); }
        foreach (var key in _inventory.Keys)
        {
            var definition = World.Registry.Definitions[key];
            var remaining = Remaining(key);
            var countText = remaining.Kind == PartAllowanceKind.Unlimited ? "∞" : remaining.Count.ToString();
            var button = Button(definition.Title + "  × " + countText, () => ChooseTool(key));
            button.Disabled = remaining.Exhausted || !CanEdit;
            button.TooltipText = ""; // Description already appears in the drawer when chosen.
            button.Icon = WorkshopIcons.Pictogram(definition.Id);
            button.SetMeta("part_kind", definition.Id);
            button.CustomMinimumSize = new(180, 40);
            button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            button.IconAlignment = HorizontalAlignment.Left;
            // The single button owns the whole row. Decorative labels ignore
            // mouse input so the name/count activate the same placement preview.
            var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            button.AddChild(row);
            row.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            row.OffsetLeft = 40;
            row.OffsetRight = -6;
            var name = Text(definition.Title, 14, button.Disabled ? Muted : Navy);
            name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            name.ClipText = true;
            name.MouseFilter = Control.MouseFilterEnum.Ignore;
            row.AddChild(name);
            var countLabel = Text(countText, 14, Muted);
            countLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            row.AddChild(countLabel);
            _palette.AddChild(button);
        }
    }
    private PartAllowance Remaining(WorkshopPartKind kind) => _inventory.GetValueOrDefault(kind, PartAllowance.None)
        .Less(World.Parts.Count(p => !p.Locked && p.Definition.WorkshopKind == kind));
    private void ChooseTool(WorkshopPartKind kind)
    {
        if (!CanEdit) return;
        ClearPreview();
        _previewOrientation = CanonicalRotation.Identity;
        _placementHeight = new((Half)(3));
        _tool = kind;
        Select(null);
        _detail.Text = World.Registry.Definitions[kind].Title;
        CreatePreview(kind);
        _status.Text = "Click to place. Escape cancels.";
    }
    private void Select(MachinePart? part)
    {
        EndGizmo(false);
        if (GodotObject.IsInstanceValid(_selected)) _selected!.SetSelected(false);
        _selected = part;
        if (part is null) _linkSource = null;
        RefreshConnectionChoices();
        if (part == null) { _detail.Text = ""; return; }
        part.SetSelected(true);
        if (!_inRun) SetLayer(part.Position.Z);
        _detail.Text = part.Definition.Title + (part.Locked ? " · fixed" : "");
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventKey panKey)
        {
            var code = panKey.PhysicalKeycode != Key.None ? panKey.PhysicalKeycode : panKey.Keycode;
            if (code is Key.W or Key.A or Key.S or Key.D or Key.Q or Key.E)
            {
                if (panKey.Pressed && !panKey.CtrlPressed && !panKey.MetaPressed && !panKey.AltPressed &&
                    GetViewport().GuiGetFocusOwner() is not (LineEdit or TextEdit))
                    _cameraKeys.Add(code);
                else _cameraKeys.Remove(code);
                return;
            }
        }
        if (_rotationGizmo != null && _rotationGizmo.Dragging) return;
        if (input is InputEventMouseButton mouse)
        {
            if (mouse.ButtonIndex == MouseButton.Right) { _orbiting = mouse.Pressed; if (_orbiting) { _buildView = false; RefreshLayerAppearance(); } }
            if (mouse.Pressed && mouse.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            {
                _zoom = Mathf.Clamp(_zoom + (mouse.ButtonIndex == MouseButton.WheelUp ? -.6f : .6f), 8, 22);
                UpdateCamera();
            }
            if (mouse.ButtonIndex == MouseButton.Left)
            {
                if (!mouse.Pressed) _dragging = _lifting = false;
                else if (CanEdit) Click(mouse.Position);
            }
        }
        else if (input is InputEventMouseMotion motion)
        {
            if (_lifting) return; // Lift motion is handled globally, including over UI.
            if (_orbiting)
            {
                _azimuth -= motion.Relative.X * .006f;
                _elevation = Mathf.Clamp(_elevation + motion.Relative.Y * .004f, .2f, 1.2f);
                UpdateCamera();
            }
            else if (_dragging && _selected is { Locked: false } && CanEdit)
            {
                if (!_dragMoved)
                {
                    if (motion.Position.DistanceTo(_dragPressScreen) < PartDragThresholdPixels) return;
                    PushUndo();
                    _dragMoved = true;
                }
                var point = WorkPoint(motion.Position, _buildView ? _selected.Position.Z : _selected.Position.Y);
                if (point is { } at)
                {
                    _selected.Position = ClampPlacement(at + _grabOffset, _buildView ? _selected.Position.Z : _selected.Position.Y);
                    RefreshLayerAppearance();

                }
            }
        }
        else if (input is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (key.Keycode == Key.Space) ToggleRun();
            else if (key.Keycode == Key.Escape) { _optionsPanel.Visible = false; CancelTool(); }
            else if (CanEdit)
            {
                if (key.Keycode == Key.Z && (key.CtrlPressed || key.MetaPressed)) Undo();
                switch (key.Keycode)
                {

                    case Key.Delete: case Key.Backspace: DeleteSelected(); break;
                    case Key.Pageup: ChangeDepth(.5f); break;
                    case Key.Pagedown: ChangeDepth(-.5f); break;
                }
            }
        }
    }
    private Vector3? WorkPoint(Vector2 screen, float z) =>
        new Plane(_buildView ? Vector3.Back : Vector3.Up, z).IntersectsRay(_camera.ProjectRayOrigin(screen), _camera.ProjectRayNormal(screen));
    private MachinePart? Pick(Vector2 screen)
    {
        MachinePart? closest = null;
        var best = 65f;
        foreach (var part in World.Parts)
        {
            if (!part.Visible) continue;
            if (_buildView && Mathf.Abs(part.Position.Z - (float)_depth.Value) > .26f) continue;
            var distance = _camera.UnprojectPosition(part.Position).DistanceTo(screen);
            if (distance >= best) continue;
            best = distance;
            closest = part;
        }
        return closest;
    }
    private async void Click(Vector2 screen)
    {
        if (_linkSource is not null) { ChooseConnectionTarget(Pick(screen)); return; }
        if (_tool is null && _rotationGizmo.Begin(_camera, screen))
        {
            PushUndo();
            _gizmoUndoPending = true;
            _dragging = _lifting = _orbiting = false;
            _status.Text = _rotationGizmo.ResizeMode ? "Drag a square to stretch. Shift snaps; Escape cancels." : _rotationGizmo.MoveMode ? "Drag an arrow. Shift aligns to 0.1; Escape cancels." : "Drag to rotate. Shift snaps; Escape cancels.";
            return;
        }
        if (_tool is not null)
        {
            if (Remaining(_tool.Value).Exhausted || WorkPoint(screen, _buildView ? (float)_depth.Value : (float)_placementHeight.Value) is not { } at || !PlacementInside(at)) return;
            if (_nextId.Value == ulong.MaxValue) { _status.Text = "Part identity is exhausted."; return; }
            PushUndo();
            var position = at.Snapped(Vector3.One * .1f);
            var rotation = _preview?.Quaternion ?? Quaternion.Identity;
            var chosen = _tool.Value;
            var placedId = _nextId;
            var proposed = World.Construction.WithInstance(World.CaptureInstance(chosen, placedId, position, rotation,
                _preview is RampPart ramp ? ramp.CanonicalDimensions : null,
                _preview is WallPart wall ? wall.CanonicalDimensions : null));
            var accepted = await SubmitConstruction(proposed);
            if (_workshopUiRemoved) return;
            if (!accepted) { _undo.RemoveAt(_undo.Count - 1); return; }
            _nextId = new(_nextId.Value + 1);
            var part = World.Parts.Single(p => p.AuthoredId == placedId);
            Select(part);
            _tool = null;
            ClearPreview();
            RefreshPalette();
            RefreshLayers();
            _dragging = false;
            _status.Text = "Drag to slide. Choose arrows to move or rings to rotate.";
        }
        else
        {
            Select(Pick(screen));
            if (_selected is { Locked: false })
            {
                _dragging = true;
                _dragMoved = false;
                _dragPressScreen = screen;
                _grabOffset = WorkPoint(screen, _buildView ? _selected.Position.Z : _selected.Position.Y) is { } grabbed ? _selected.Position - grabbed : Vector3.Zero;
                _depth = new((Half)(_selected.Position.Z));
                ChangeDepth(0);
            }
        }
    }
    private void Rotate(Vector3 angles)
    {
        if (!CanEdit) return;
        if (_tool is not null)
        {
            if (_preview != null)
            {
                ApplyRotation(_preview, angles);
                _previewOrientation = CaptureEditorRotation(_preview.Quaternion);
            }
            return;
        }
        if (_selected is not { Locked: false }) { _status.Text = "Choose a part to place, or select a movable part first."; return; }
        PushUndo();
        ApplyRotation(_selected, angles);
        CommitSelected();

    }
    // Compose rotations around fixed workbench axes, not Euler components.
    // This keeps every axis usable even after a part has been tipped through 90°.
    private static void ApplyRotation(MachinePart part, Vector3 angles)
    {
        var rotation = new Quaternion(Vector3.Right, Mathf.DegToRad(angles.X)) *
                       new Quaternion(Vector3.Up, Mathf.DegToRad(angles.Y)) *
                       new Quaternion(Vector3.Back, Mathf.DegToRad(angles.Z));
        part.Quaternion = (rotation * part.Quaternion).Normalized();
    }

    private void ChangeDepth(float amount)
    {
        if (!CanEdit) return;
        _depth = new((Half)(Mathf.Clamp((float)_depth.Value + amount, -4, 4)));
        if (_selected is { Locked: false } && amount != 0)
        {
            PushUndo();
            _selected.Position = new(_selected.Position.X, _selected.Position.Y, (float)_depth.Value);
            CommitSelected();

        }
        SetLayer((float)_depth.Value);
    }
    private async void DeleteSelected()
    {
        if (!CanEdit || _selected is not { Locked: false }) return;
        var id = _selected.AuthoredId;
        PushUndo(); Select(null);
        var proposed = World.Construction.WithoutInstance(id);
        var accepted = await SubmitConstruction(proposed);
        if (_workshopUiRemoved) return;
        if (!accepted) _undo.RemoveAt(_undo.Count - 1);
        RefreshPalette();  RefreshLayers();
    }

    private void PushUndo()
    {
        _undo.Add(World.Construction);
        if (_undo.Count > 40) _undo.RemoveAt(0);
    }
    private async void Undo()
    {
        if (!CanEdit || _undo.Count == 0) return;
        Select(null);
        var accepted = await SubmitConstruction(_undo[^1]);
        if (_workshopUiRemoved) return;
        if (accepted) _undo.RemoveAt(_undo.Count - 1);
        RefreshPalette();  RefreshLayers();
    }



    private async void ToggleRun()
    {
        if (_gpuPending) return;
        if (IsRuntimePhase(World.WorkshopPhase)) { ResetRun(); return; }
        if (!CanEdit) return;
        EndGizmo(false);
        Select(null); _tool = null; ClearPreview(); _dragging = false;
        _gpuPending = true; _state.Text = "STARTING"; RefreshPalette();
        try
        {
            var result = await World.RunWorkshop();
            if (_workshopUiRemoved) return;
            if (!result.Applicable) return;
            if (result.Result.Outcome != WorkshopCommandOutcome.Applied) { _status.Text = $"Run rejected: {result.Result.Reason}"; return; }
            _inRun = true;
            WorkshopIcons.Apply(_run, "■  Back to building");
            _state.Text = "MACHINE RUNNING";
            _precision.Editable = false; _friction.Disabled = true;
            _status.Text = "Reset restores the starting arrangement.";
        }
        catch (Exception error) { if (!_workshopUiRemoved) _status.Text = error.Message; }
        finally { if (!_workshopUiRemoved) { _gpuPending = false; ReconcileCommittedUi(); RefreshPalette(); RefreshLayerAppearance(); } }
    }

    private bool _cancelPending;
    private async void ResetRun()
    {
        if (_gpuPending)
        {
            if (_cancelPending || World.TransportState != WorkshopTransportState.TimedOut) return;
            _cancelPending = true;
            try
            {
                var cancelled = await World.CancelPendingWorkshop();
                if (!_workshopUiRemoved && cancelled.Applicable) _status.Text = $"Cancel outcome: {cancelled.Result.Outcome}. The original request remains tracked.";
            }
            catch (Exception error) { if (!_workshopUiRemoved) _status.Text = error.Message; }
            finally { _cancelPending = false; }
            return;
        }
        _gpuPending = true; _state.Text = "RESETTING"; Select(null); RefreshPalette();
        try
        {
            var result = await World.ResetWorkshop();
            if (_workshopUiRemoved) return;
            if (!result.Applicable) return;
            if (result.Result.Outcome != WorkshopCommandOutcome.Applied) { _status.Text = $"Reset rejected: {result.Result.Reason}"; return; }
            ResetUiAnimations(); _inRun = false; SetBuildUi();
        }
        catch (Exception error) { if (!_workshopUiRemoved) _status.Text = error.Message; }
        finally { if (!_workshopUiRemoved) { _gpuPending = false; ReconcileCommittedUi(); RefreshPalette(); RefreshLayerAppearance();  } }
    }

    private static bool IsRuntimePhase(WorkshopSimulationPhase phase) =>
        phase is WorkshopSimulationPhase.Running or WorkshopSimulationPhase.Paused or WorkshopSimulationPhase.Completed or WorkshopSimulationPhase.Faulted;

    private void ReconcileCommittedUi()
    {
        var phase = World.WorkshopPhase;
        if (phase is not (WorkshopSimulationPhase.Building or WorkshopSimulationPhase.Running or WorkshopSimulationPhase.Paused or
            WorkshopSimulationPhase.Completed or WorkshopSimulationPhase.Faulted)) return;
        _displayedWorkshopPhase = phase;
        _inRun = IsRuntimePhase(phase);
        if (phase == WorkshopSimulationPhase.Building)
        {
            // Committed state owns goal visibility even when its Reset ACK is no longer applicable.
            // Do not reset autonomous hints during ordinary Building reconciliation.
            ClearGoalFeedback(); SetBuildUi(); return;
        }
        if (!_inRun) return;
        WorkshopIcons.Apply(_run, "■  Back to building");
        _precision.Editable = false; _friction.Disabled = true;
        _state.Text = phase switch
        {
            WorkshopSimulationPhase.Paused => "MACHINE PAUSED",
            WorkshopSimulationPhase.Completed => "TIME TO TINKER",
            WorkshopSimulationPhase.Faulted => "SIMULATION STOPPED",
            _ => "MACHINE RUNNING"
        };
        _status.Text = phase switch
        {
            WorkshopSimulationPhase.Paused => "Paused. Step advances one tick; Resume continues from here.",
            WorkshopSimulationPhase.Completed => "30 seconds elapsed. Reset to build again.",
            WorkshopSimulationPhase.Faulted => World.WorkshopFault ?? "The GPU simulation stopped. Reset to retry.",
            _ => "Reset restores the starting arrangement."
        };
    }

    private void SetBuildUi()
    {
        WorkshopIcons.Apply(_run, "▶  Run machine");
        _state.Text = _gpuPending ? "GPU PENDING" : "BUILD MODE";
        _precision.Editable = !_gpuPending;
        _friction.Disabled = true;
        _status.Text = "Choose a part. Arrows move; rings rotate.";
    }
    public override void _PhysicsProcess(double delta)
    {
        _time.Text = $"{World.Ticks / (double)World.Construction.Settings.SimulationRate.Numerator:00.00} s";
        var transport = World.TransportState;
        var transportChanged = transport != _displayedTransportState;
        _displayedTransportState = transport;
        if (transport is WorkshopTransportState.Indeterminate or WorkshopTransportState.RecoveryBlocked)
        {
            _state.Text = transport == WorkshopTransportState.Indeterminate ? "OPERATION INDETERMINATE" : "RECOVERY BLOCKED";
            _status.Text = "Worker session retired. Reload the page; unacknowledged commands were not assumed rolled back.";
            return;
        }
        if (transport == WorkshopTransportState.TimedOut)
        {
            _state.Text = "OPERATION TIMED OUT";
            _status.Text = World.HasPendingCommand
                ? "Reset requests Cancel for the original pending operation. Timeout does not imply rollback."
                : "The GPU candidate is still pending. Reset requests recovery; reload the page if it cannot complete.";
            return;
        }
        if (transport == WorkshopTransportState.Backpressure)
        {
            _state.Text = "WAITING FOR WORKER";
            _status.Text = "Reliable response exceeded 50 ms; simulation stops if unresolved for 500 ms.";
            return;
        }
        // A physical read may supersede the original Run ACK before its await resumes.
        // Reconcile on the committed transition, preserving later visible action rejections.
        if (!_gpuPending && (World.WorkshopPhase != _displayedWorkshopPhase || transportChanged))
        {
            ReconcileCommittedUi(); RefreshPalette(); RefreshLayerAppearance();
        }
    }

    private void ShowHint()
    {
        _hint.Text = World.Construction.Puzzle.Id == WorkshopPuzzleId.FirstPrinciples
            ? "Start with a gentle slope below the ball. Use the second ramp to continue the journey toward the receiver."
            : World.Construction.Puzzle.Id == WorkshopPuzzleId.DelayedSignal
                ? "Connect switch → delay → lamp. A trigger starts the one-second countdown; further triggers are ignored until Reset."
                : World.Construction.Puzzle.Id == WorkshopPuzzleId.DominoEffect
                    ? "Stand four tiles between the ball and the fixed end domino, one metre apart, so each falling tile strikes the next. Connect the end domino → lamp."
                    : "Place the Basketball at two different heights and compare its fall.";
        if (_hint.Visible) HideHint();
        else { _hint.Visible = true; RevealHint(); }
        _objectivePanel.Size = new(266, 0);
    }
    private async void Save()
    {
        if (!CanEdit) { _status.Text = "Return to building before saving."; return; }
        if (World.WorkshopRead.Revision.Value == ulong.MaxValue)
        { _status.Text = "The session cannot acknowledge another save."; return; }
        var stored = false;
        _gpuPending = true; RefreshPalette();
        try
        {
            var bytes = WorkshopSaveCodec.Encode(new(World.Construction, _nextId));
            await BrowserWorkshopSaveStore.Save(bytes);
            stored = true;
            if (_workshopUiRemoved) return;
            var result = await World.AcknowledgeConstructionSave();
            if (_workshopUiRemoved) return;
            _status.Text = result.Applicable && result.Result.Outcome == WorkshopCommandOutcome.Applied
                ? "Construction saved in this browser."
                : "Construction stored. Session acknowledgement is incomplete; do not retry automatically.";
        }
        catch (Exception error)
        {
            if (!_workshopUiRemoved) _status.Text = stored
                ? "Construction stored. Session acknowledgement is indeterminate: " + error.Message
                : "Save failed; the previous save is unchanged. " + error.Message;
        }
        finally { if (!_workshopUiRemoved) { _gpuPending = false; RefreshPalette(); } }
    }

    private async void LoadSave()
    {
        if (!CanEdit) { _status.Text = "Return to building before loading."; return; }
        var applied = false;
        var submitted = false;
        _gpuPending = true; RefreshPalette();
        try
        {
            var encoded = await BrowserWorkshopSaveStore.Load(WorkshopSaveCodec.ByteLength);
            if (_workshopUiRemoved) return;
            if (encoded.Length == 0) { _status.Text = "No construction has been saved in this browser."; return; }
            var saved = WorkshopSaveCodec.Decode(Convert.FromBase64String(encoded));
            if (saved.Construction.Settings != World.Construction.Settings)
                throw new ArgumentException("This save uses different simulation settings.");
            // SubmitConstruction owns the existing GPU admission and installation barrier.
            var priorRevision = World.Construction.Revision;
            _gpuPending = false;
            submitted = true;
            var accepted = await SubmitConstruction(saved.Construction);
            // Authority may have committed even if subsequent scene restoration failed.
            applied = World.Construction.Revision.Value > priorRevision.Value &&
                World.Construction.Instances.Equals(saved.Construction.Instances) &&
                World.Construction.Puzzle == saved.Construction.Puzzle &&
                World.Construction.Connections.Equals(saved.Construction.Connections);
            if (applied) _nextId = new(Math.Max(_nextId.Value, saved.NextBodyId.Value));
            if (_workshopUiRemoved) return;
            if (!accepted)
            {
                if (applied) _status.Text = "Construction loaded; scene restoration is incomplete.";
                return;
            }
            Select(null); _tool = null; ClearPreview(); _dragging = false; _undo.Clear();
            ResetUiAnimations(); PresentMode();
            _status.Text = "Construction loaded. Ready to run.";
        }
        catch (Exception error)
        {
            if (!_workshopUiRemoved) _status.Text = applied
                ? "Construction loaded; presentation could not finish. " + error.Message
                : submitted ? "Load acknowledgement is incomplete; recover the session. " + error.Message
                : "Load rejected; construction unchanged. " + error.Message;
        }
        finally { if (!_workshopUiRemoved) { _gpuPending = false; RefreshPalette(); RefreshLayers(); } }
    }


}
