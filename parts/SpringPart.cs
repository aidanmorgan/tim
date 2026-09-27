using Godot;
using System.Collections.Generic;
namespace CuriousContraptions;

public partial class SpringPart : MachinePart
{
    private readonly Dictionary<string, int> _cooldown = new();
    private readonly List<float> _recoilAges = new();
    private MeshInstance3D _plate = null!;
    private MeshInstance3D _coil = null!;
    private const float CoilHeight = .365f;
    private const float RecoilDuration = .84f;
    public float PlateOffset { get; private set; }
    public int HitCount { get; private set; }
    protected override void Build()
    {
        PickRadius = .7f;
        // Collision stays fixed; presentation cannot alter the solved trajectory.
        AddBox(new(0, .14f, 0), new(1.3f, .15f, 1.2f), Definition.Color, false);
        _plate = PartArt.Box(Visual, new(1.3f, .15f, 1.2f), Definition.Color, new(0, .14f, 0));
        _plate.Name = "SpringPlate";
        PartArt.Box(Visual, new(1.3f, .12f, 1.2f), new("#273446"), new(0, -.36f, 0));
        _coil = PartArt.Mesh(Visual, BuildCoil(), new("#ccd9df"), new(0, -.3f, 0));
        _coil.Name = "SpringCoil";
    }
    private static ImmediateMesh BuildCoil()
    {
        const int segments = 96, sides = 8;
        var mesh = new ImmediateMesh();
        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        void Vertex(int step, int side)
        {
            var t = (float)step / segments;
            var angle = t * Mathf.Tau * 3;
            var radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            var tangent = new Vector3(-Mathf.Sin(angle) * .27f * Mathf.Tau * 3, CoilHeight,
                Mathf.Cos(angle) * .27f * Mathf.Tau * 3).Normalized();
            var around = (float)side / sides * Mathf.Tau;
            var normal = radial * Mathf.Cos(around) + tangent.Cross(radial) * Mathf.Sin(around);
            mesh.SurfaceSetNormal(normal);
            mesh.SurfaceAddVertex(radial * .27f + Vector3.Up * (t * CoilHeight) + normal * .025f);
        }
        for (var step = 0; step < segments; step++)
        for (var side = 0; side < sides; side++)
        {
            Vertex(step, side); Vertex(step + 1, side); Vertex(step + 1, side + 1);
            Vertex(step, side); Vertex(step + 1, side + 1); Vertex(step, side + 1);
        }
        mesh.SurfaceEnd();
        return mesh;
    }
    public override void _Process(double delta)
    {
        var offset = 0f;
        for (var i = _recoilAges.Count - 1; i >= 0; i--)
        {
            _recoilAges[i] += (float)delta;
            var age = _recoilAges[i];
            if (age >= RecoilDuration) { _recoilAges.RemoveAt(i); continue; }
            var envelope = 1 - age / RecoilDuration;
            // Contact starts compression, then a decaying rebound. Zero onset
            // displacement and velocity keep overlapping impacts continuous.
            var onset = Mathf.Clamp(age / .035f, 0, 1);
            onset = onset * onset * (3 - 2 * onset);
            offset -= .22f * Mathf.Sin(age * Mathf.Tau / .28f) * envelope * envelope * onset;
        }
        // Smooth saturation keeps simultaneous hits above the base plate.
        PlateOffset = .25f * Mathf.Tanh(offset / .25f);
        _plate.Position = new(0, .14f + PlateOffset, 0);
        _coil.Scale = new(1, (CoilHeight + PlateOffset) / CoilHeight, 1);
    }
    public override void OnContact(MachinePart body, float speed, MachineWorld world)
    {
        if (speed < .05f || _cooldown.GetValueOrDefault(body.Uid, -1000) + 18 > world.Ticks) return;
        _cooldown[body.Uid] = world.Ticks;
        HitCount++;
        _recoilAges.Add(0);
        var direction = Basis.Y.Normalized();
        body.Velocity = direction * Parameter("strength", 8.5f);
        world.Events.TryAdd(new MachineEvent(MachineEventKind.Bounced, Uid), world.Ticks);
    }
}
