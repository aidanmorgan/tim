using Godot;
using System;

namespace CuriousContraptions;

// Visual editing handles only. Fixed world axes, no physics bodies or Euler-angle drag math.
public partial class RotationGizmo : Node3D
{
    private static readonly Vector3[] Axes = [Vector3.Right, Vector3.Up, Vector3.Back];
    private static readonly Vector3[] U = [Vector3.Up, Vector3.Back, Vector3.Right];
    private static readonly Vector3[] V = [Vector3.Back, Vector3.Right, Vector3.Up];
    private static readonly Color[] Colors = [new("#de7058"), new("#62aa78"), new("#5b9cdb")];
    private readonly MeshInstance3D[] _rings = new MeshInstance3D[3], _handles = new MeshInstance3D[3];
    private readonly float[] _angles = [.6f, 2.2f, 3.8f];
    private readonly MeshInstance3D[] _shafts = new MeshInstance3D[3], _arrows = new MeshInstance3D[3];
    private MachinePart? _target;
    private Vector3 _startPosition;
    private Vector2 _moveScreenAxis, _startMouse;
    public bool ResizeMode { get; private set; }
    private Vector3 _startDimensions;
    private Vector3 Direction(int axis) => ResizeMode && _target != null ? _target.GlobalBasis * Axes[axis] : Axes[axis];
    public void SetResizeMode()
    {
        End(true);
        MoveMode = false;
        ResizeMode = true;
    }
    public bool MoveMode { get; private set; }
    public void SetMoveMode(bool move)
    {
        End(true);
        MoveMode = move;
        ResizeMode = false;
    }
    private Quaternion _start;
    private Vector2 _lastMouse;
    private float _lastAngle, _totalAngle, _radius = 1;
    public bool Dragging => ActiveAxis >= 0;
    public int ActiveAxis { get; private set; } = -1;

    public override void _Ready()
    {
        Name = "RotationGizmo";
        Visible = false;
        for (var axis = 0; axis < 3; axis++)
        {
            _rings[axis] = PartArt.Ring(this, 1, .025f, Colors[axis]);
            _rings[axis].Name = "Ring" + axis;
            _rings[axis].Quaternion = new Quaternion(Vector3.Up, Axes[axis]);
            _handles[axis] = PartArt.Sphere(this, .115f, Colors[axis]);
            _handles[axis].Name = "Handle" + axis;
            _shafts[axis] = new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = .025f, BottomRadius = .025f, Height = 1 },
                MaterialOverride = new StandardMaterial3D { AlbedoColor = Colors[axis] },
                Quaternion = new Quaternion(Vector3.Up, Axes[axis]), Visible = false
            };
            _arrows[axis] = new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0, BottomRadius = .13f, Height = .3f },
                MaterialOverride = new StandardMaterial3D { AlbedoColor = Colors[axis] },
                Quaternion = new Quaternion(Vector3.Up, Axes[axis]), Visible = false
            };
            AddChild(_shafts[axis]);
            AddChild(_arrows[axis]);
            foreach (var mesh in new[] { _rings[axis], _handles[axis], _shafts[axis], _arrows[axis] })
            {
                mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
                var material = (StandardMaterial3D)mesh.MaterialOverride;
                material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
                material.NoDepthTest = true;
                material.RenderPriority = 10;
            }
        }
    }

    public void Follow(MachinePart? target, bool enabled, Camera3D camera)
    {
        if (target != _target || !enabled) End(false);
        _target = target;
        if (ResizeMode && target is not WallPart) SetMoveMode(true);
        Visible = enabled && GodotObject.IsInstanceValid(target) && target is { Locked: false, Visible: true };
        if (!Visible) return;
        GlobalPosition = target!.GlobalPosition;
        if (!Dragging)
        {
            var bounds = PlacementShadows.ArtworkBounds(target);
            _radius = Mathf.Clamp(bounds.Size.Length() * .55f, .85f, 2.5f);
            // At least 65 screen pixels, so small parts remain easy to grab.
            var pixels = camera.UnprojectPosition(GlobalPosition + camera.GlobalBasis.X)
                .DistanceTo(camera.UnprojectPosition(GlobalPosition));
            _radius = Mathf.Max(_radius, 65 / Mathf.Max(1, pixels));
        }
        for (var axis = 0; axis < 3; axis++)
        {
            _rings[axis].Scale = Vector3.One * _radius;
            _rings[axis].Visible = _handles[axis].Visible = !MoveMode && !ResizeMode;
            _shafts[axis].Visible = _arrows[axis].Visible = MoveMode || ResizeMode;
            var direction = Direction(axis);
            _shafts[axis].Quaternion = _arrows[axis].Quaternion = new Quaternion(Vector3.Up, direction);
            _shafts[axis].Position = direction * _radius * .5f;
            _shafts[axis].Scale = new(1, _radius, 1);
            _arrows[axis].Position = direction * _radius;
            if (ResizeMode && _arrows[axis].Mesh is not BoxMesh)
                _arrows[axis].Mesh = new BoxMesh { Size = Vector3.One * .22f };
            else if (!ResizeMode && _arrows[axis].Mesh is BoxMesh)
                _arrows[axis].Mesh = new CylinderMesh { TopRadius = 0, BottomRadius = .13f, Height = .3f };
            _handles[axis].Position = Circle(axis, _angles[axis]) * _radius;
            var highlighted = ActiveAxis == axis;
            ((StandardMaterial3D)_rings[axis].MaterialOverride).AlbedoColor =
                highlighted ? Colors[axis].Lightened(.35f) : Colors[axis];
            _handles[axis].Scale = _arrows[axis].Scale = Vector3.One * (highlighted ? 1.4f : 1);
        }
    }

    private static Vector3 Circle(int axis, float angle) => U[axis] * Mathf.Cos(angle) + V[axis] * Mathf.Sin(angle);
    public Vector3 HandlePosition(int axis) => GlobalPosition + (MoveMode || ResizeMode ? Direction(axis) : Circle(axis, _angles[axis])) * _radius;

    public bool Begin(Camera3D camera, Vector2 screen)
    {
        if (!Visible || _target is not { Locked: false }) return false;
        var best = 18f;
        var picked = -1;
        var angle = 0f;
        // Spherical handles win over crossing rings.
        for (var axis = 0; axis < 3; axis++)
        {
            var distance = camera.UnprojectPosition(HandlePosition(axis)).DistanceTo(screen);
            if (distance >= best) continue;
            best = distance; picked = axis; angle = _angles[axis];
        }
        if (picked < 0 && !MoveMode && !ResizeMode)
        {
            best = 9;
            for (var axis = 0; axis < 3; axis++)
                for (var sample = 0; sample < 180; sample++)
                {
                    var at = sample * Mathf.Tau / 180;
                    var distance = camera.UnprojectPosition(GlobalPosition + Circle(axis, at) * _radius).DistanceTo(screen);
                    if (distance >= best) continue;
                    best = distance; picked = axis; angle = at;
                }
        }
        if (picked < 0) return false;
        ActiveAxis = picked;
        _start = _target.Quaternion;
        _startPosition = _target.Position;
        if (_target is WallPart wall) _startDimensions = wall.Dimensions;
        _startMouse = screen;
        _moveScreenAxis = camera.UnprojectPosition(GlobalPosition + Direction(picked)) - camera.UnprojectPosition(GlobalPosition);
        // An axis pointing directly into the camera has no meaningful projected direction.
        // A vertical drag then moves along that axis, at the same scale as the other handles.
        if (_moveScreenAxis.LengthSquared() < 16)
            _moveScreenAxis = Vector2.Up * camera.UnprojectPosition(GlobalPosition + camera.GlobalBasis.Y).DistanceTo(camera.UnprojectPosition(GlobalPosition));
        _lastMouse = screen;
        _lastAngle = PlaneAngle(camera, screen) ?? angle;
        _totalAngle = 0;
        return true;
    }

    private float? PlaneAngle(Camera3D camera, Vector2 screen)
    {
        var normal = Axes[ActiveAxis];
        var ray = camera.ProjectRayNormal(screen);
        if (Mathf.Abs(ray.Dot(normal)) < .15f) return null;
        var hit = new Plane(normal, normal.Dot(GlobalPosition))
            .IntersectsRay(camera.ProjectRayOrigin(screen), ray);
        if (hit is not { } point || point.DistanceSquaredTo(GlobalPosition) < .0001f) return null;
        var relative = point - GlobalPosition;
        return Mathf.Atan2(relative.Dot(V[ActiveAxis]), relative.Dot(U[ActiveAxis]));
    }

    public bool Drag(Camera3D camera, Vector2 screen, bool snap)
    {
        if (!Dragging || !GodotObject.IsInstanceValid(_target)) return false;
        if (ResizeMode && _target is WallPart wall)
        {
            var amount = (screen - _startMouse).Dot(_moveScreenAxis) / Mathf.Max(1, _moveScreenAxis.LengthSquared());
            var size = _startDimensions;
            size[ActiveAxis] += amount * 2; // Both faces expand around the unchanged centre.
            if (snap) size[ActiveAxis] = Mathf.Snapped(size[ActiveAxis], .1f);
            wall.SetDimensions(size);
            return true;
        }
        if (MoveMode)
        {
            var amount = (screen - _startMouse).Dot(_moveScreenAxis) / Mathf.Max(1, _moveScreenAxis.LengthSquared());
            // Snap the chosen world coordinate, not the delta: an off-grid part can align too.
            if (snap) amount = Mathf.Snapped(_startPosition[ActiveAxis] + amount, .1f) - _startPosition[ActiveAxis];
            _target!.Position = (_startPosition + Axes[ActiveAxis] * amount).Clamp(new Vector3(-7, 0, -4), new Vector3(7, 9, 4));
            return true;
        }
        float delta;
        if (PlaneAngle(camera, screen) is { } angle)
        {
            delta = Mathf.Wrap(angle - _lastAngle, -Mathf.Pi, Mathf.Pi);
            _lastAngle = angle;
        }
        else
        {
            // Edge-on rings: use their visible screen tangent instead of an unstable ray intersection.
            var tangent = (camera.UnprojectPosition(GlobalPosition + Circle(ActiveAxis, _lastAngle + .02f) * _radius) -
                           camera.UnprojectPosition(GlobalPosition + Circle(ActiveAxis, _lastAngle - .02f) * _radius)) / .04f;
            if (tangent.LengthSquared() < 4) tangent = new Vector2(60, 0);
            delta = Mathf.Clamp((screen - _lastMouse).Dot(tangent) / tangent.LengthSquared(), -.5f, .5f);
            _lastAngle += delta;
        }
        _totalAngle += delta;
        _lastMouse = screen;
        var rotation = snap ? Mathf.Snapped(_totalAngle, Mathf.DegToRad(15)) : _totalAngle;
        _target!.Quaternion = (new Quaternion(Axes[ActiveAxis], rotation) * _start).Normalized();
        _angles[ActiveAxis] = _lastAngle;
        return true;
    }

    public void End(bool cancel)
    {
        if (cancel && Dragging && GodotObject.IsInstanceValid(_target))
        {
            _target!.Quaternion = _start;
            _target.Position = _startPosition;
            if (ResizeMode && _target is WallPart wall) wall.SetDimensions(_startDimensions);
        }
        ActiveAxis = -1;
    }
}
