using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CuriousContraptions;

/// <summary>
/// Fixed-step sphere/OBB simulation with quantized state and stable part order.
/// This is not TIM's integer solver; original and cross-platform fidelity need
/// measured regression evidence. All coordinates and collisions use real XYZ.
/// </summary>
public partial class MachineWorld : Node3D
{
    public const float Tick = 1f / 120;
    public const int Substeps = 4;
    public const float Quantum = 1f / 65536;
    public event Action? Solved;
    public PartRegistry Registry { get; } = new();
    public List<MachinePart> Parts { get; } = new();
    public List<MachinePart> Bodies { get; } = new();
    public IEnumerable<MachinePart> CollisionParts
    {
        get
        {
            foreach (var part in Parts)
            {
                yield return part;
                foreach (var body in part.InternalBodies) yield return body;
            }
        }
    }
    public List<ConnectionSpec> Connections { get; private set; } = new();
    public List<GoalSpec> Objectives { get; private set; } = new();
    public List<RopePath> Ropes { get; private set; } = new();
    public SortedDictionary<MachineEvent, int> Events { get; } = new();
    private List<PartSpec> _placementTargets = new();
    private List<PartAssistance.Correction> _corrections = new();
    private float _assistanceTime;
    public bool Running { get; set; }
    public bool Won { get; private set; }
    public int Ticks { get; private set; }
    public float Precision { get; set; } = .45f;
    public float Gravity { get; set; } = 9.81f;
    public float Pressure { get; set; } = 1;
    public bool Realistic { get; set; }
    public MachineData? Initial { get; private set; }
    private List<(string Id, Transform3D Transform, Vector3 Rotation, Vector3 Scale)> _initialTransforms = new();

    private OpticalPathVisual _opticalVisual=null!;
    public IReadOnlyList<OpticalSegment> OpticalPaths { get; private set; }=[];
    public void SetOpticalPaths(IReadOnlyList<OpticalSegment> paths)
    {
        OpticalPaths=OpticalPathVisual.Merge(paths);
    }
    public override void _Ready()
    {
        Registry.Discover();
        _opticalVisual=new OpticalPathVisual {Name="OpticalPaths"};
        AddChild(_opticalVisual);
    }
    public override void _Process(double delta)=>_opticalVisual.Refresh(OpticalPaths);

    public void LoadMachine(MachineData input)
    {
        var data = MachineCodec.Clone(input);
        ValidateMachine(data);
        Initial = null;
        _initialTransforms = new();
        Running = Won = false;
        OpticalPaths=[];
        _opticalVisual.Refresh([]);
        Ticks = 0;
        Events.Clear();
        _corrections.Clear();
        _surfaceDampingBudgets.Clear();
        _assistanceTime = 0;
        _placementTargets = data.PlacementTargets;
        foreach (var part in Parts) { RemoveChild(part); part.Free(); }
        Parts.Clear();
        Bodies.Clear();
        Connections = data.Connections;
        Ropes.Clear();
        Objectives = data.Goals;
        Gravity = data.Gravity;
        Pressure = data.Pressure;
        foreach (var entry in data.Parts) AddPart(entry);
    }

    public void ValidateMachine(MachineData data)
    {
        foreach (var goal in data.Goals)
            if (!float.IsFinite(goal.MinimumDelaySeconds) || goal.MinimumDelaySeconds < 0 || goal.MinimumDelaySeconds > 120)
                throw new ArgumentException("Goal delay must be finite and between zero and 120 seconds.");
        // Reject unsupported mechanical graphs before replacing the current machine.
        var candidates = new List<MachinePart>();
        try
        {
            foreach (var entry in data.Parts) candidates.Add(Registry.Create(entry));
            ValidateInstanceIds(candidates);
            foreach (var link in data.Connections)
            {
                var source = candidates.SingleOrDefault(p => p.Uid == link.From);
                var target = candidates.SingleOrDefault(p => p.Uid == link.To);
                if (source == null || target == null ||
                    !ConnectionRules.TryResolve(link, source.ConnectionPorts, target.ConnectionPorts, out _, out _))
                    throw new ArgumentException("Invalid connection endpoints or socket type.");
            }
            MechanicalNetwork.Validate(candidates, data.Connections);
            RopeNetwork.Build(candidates, data.Connections);
        }
        finally { foreach (var candidate in candidates) candidate.Free(); }
    }

    private static void ValidateInstanceIds(IEnumerable<MachinePart> parts)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var part in parts)
        {
            if (!ids.Add(part.Uid)) throw new ArgumentException("Duplicate instance ID: " + part.Uid);
            foreach (var role in part.InternalBodyRoles)
            {
                var id = part.InternalBodyId(role);
                if (!ids.Add(id)) throw new ArgumentException("Duplicate internal-body instance ID: " + id);
            }
        }
    }

    public MachinePart AddPart(PartSpec entry)
    {
        var part = Registry.Create(entry);
        try { ValidateInstanceIds(Parts.Append(part)); }
        catch { part.Free(); throw; }
        AddChild(part);
        Parts.Add(part);
        Parts.Sort((a, b) => string.CompareOrdinal(a.Uid, b.Uid));
        if (part.Dynamic) Bodies.Add(part);
        Bodies.AddRange(part.InternalBodies);
        Bodies.Sort((a, b) => string.CompareOrdinal(a.Uid, b.Uid));
        part.UpdateAssistance(Precision);
        return part;
    }
    public void RemovePart(MachinePart part)
    {
        Parts.Remove(part);
        Bodies.Remove(part);
        foreach (var body in part.InternalBodies) Bodies.Remove(body);
        Connections.RemoveAll(link => link.From == part.Uid || link.To == part.Uid);
        RemoveChild(part);
        part.Free();
    }
    public MachineData Snapshot() => MachineCodec.Clone(new()
    {
        Parts = Parts.Select(p => p.Serialize()).ToList(), PlacementTargets = _placementTargets,
        Connections = Connections, Goals = Objectives, Gravity = Gravity, Pressure = Pressure
    });
    public void Start()
    {
        ElectricalNetwork.Validate(this);
        MechanicalNetwork.Validate(Parts, Connections);
        Ropes = RopeNetwork.Build(Parts, Connections);
        Initial = Snapshot();
        // Preserve the edited basis exactly; Euler serialization is not a lossless Run/Reset snapshot.
        _initialTransforms = Parts.Select(part => (part.Uid, part.Transform, part.Rotation, part.Scale)).ToList();
        _assistanceTime = 0;
        _corrections = PartAssistance.Prepare(Parts, _placementTargets, Precision);
        foreach (var part in Parts) part.UpdateAssistance(Precision);
        Ticks = 0;
        Won = false;
        Events.Clear();
        Running = true;
    }
    public void Restore()
    {
        if (Initial == null) return;
        var transforms = _initialTransforms;
        LoadMachine(Initial);
        foreach (var entry in transforms)
        {
            var part = FindPart(entry.Id) ?? throw new InvalidOperationException("Restored construction is missing a part.");
            // Restore native radians/scale too: authored Euler nodes cache these independently
            // of the basis. Degrees-to-radians and matrix decomposition each introduce rounding.
            part.Rotation = entry.Rotation;
            part.Scale = entry.Scale;
            // Gizmo-edited bases must still be restored bit-for-bit.
            if (part.Transform != entry.Transform) part.Transform = entry.Transform;
        }
    }
    public MachinePart? FindPart(string id) => Parts.Find(p => p.Uid == id);
    public ConnectionSpec? SuggestedConnection(MachinePart source, MachinePart target)
    {
        var options = ConnectionOptions(source, target);
        return options.Count == 1 ? options[0] : null;
    }
    public List<ConnectionSpec> ConnectionOptions(MachinePart source, MachinePart target)
    {
        var options = new List<ConnectionSpec>();
        if (source == target || !Parts.Contains(source) || !Parts.Contains(target)) return options;
        foreach (var output in source.ConnectionPorts)
        foreach (var input in target.ConnectionPorts)
        {
            var link = new ConnectionSpec { From = source.Uid, To = target.Uid,
                FromPort = output.Id, ToPort = input.Id, Type = output.Domain,
                RopeLength = output.Domain == ConnectionDomain.Rope
                    ? (source.Transform * output.LocalPosition).DistanceTo(target.Transform * input.LocalPosition) : null };
            if (!ConnectionRules.TryResolve(link, source.ConnectionPorts, target.ConnectionPorts, out _, out _)) continue;
            if (link.Type == ConnectionDomain.Mechanical &&
                !MechanicalNetwork.CanConnect(Parts, Connections.Append(link))) continue;
            if (link.Type == ConnectionDomain.Rope &&
                !RopeNetwork.CanConnect(Parts, Connections.Append(link))) continue;
            options.Add(link);
        }
        return options;
    }
    public bool Connect(MachinePart source, MachinePart target)
    {
        var link = SuggestedConnection(source, target);
        if (link == null) return false;
        var output = source.ConnectionPorts.Single(p => p.Id == link.FromPort);
        return Connect(source, link.FromPort!.Value, target, link.ToPort!.Value, output.Domain);
    }

    public bool Connect(MachinePart source, SocketId fromPort, MachinePart target,
        SocketId toPort, ConnectionDomain domain)
    {
        if (Running || !Parts.Contains(source) || !Parts.Contains(target)) return false;
        var link = new ConnectionSpec
        {
            From = source.Uid, To = target.Uid, Type = domain,
            FromPort = fromPort, ToPort = toPort
        };
        if (domain == ConnectionDomain.Rope)
        {
            var output = source.ConnectionPorts.SingleOrDefault(p => p.Id == fromPort);
            var input = target.ConnectionPorts.SingleOrDefault(p => p.Id == toPort);
            link.RopeLength = (source.Transform * output.LocalPosition).DistanceTo(target.Transform * input.LocalPosition);
        }
        if (!IsValidConnection(link)) return false;
        if (Connections.Any(c => c.From == link.From && c.To == link.To
            && c.Type == link.Type && ConnectionRules.TryResolve(c, source.ConnectionPorts,
                target.ConnectionPorts, out var a, out var b)
            && a.Id == fromPort && b.Id == toPort)) return false;
        if (domain == ConnectionDomain.Mechanical &&
            !MechanicalNetwork.CanConnect(Parts, Connections.Append(link))) return false;
        if (domain == ConnectionDomain.Rope &&
            !RopeNetwork.CanConnect(Parts, Connections.Append(link))) return false;
        Connections.Add(link);
        return true;
    }

    public bool IsValidConnection(ConnectionSpec link)
    {
        var source = FindPart(link.From);
        var target = FindPart(link.To);
        return source != null && target != null
            && ConnectionRules.TryResolve(link, source.ConnectionPorts, target.ConnectionPorts, out _, out _);
    }
    private enum ActivationDelivery { Receive, Emit }
    public void Activate(MachinePart source) => DispatchActivation(source, ActivationDelivery.Receive);
    internal void EmitActivation(MachinePart source) => DispatchActivation(source, ActivationDelivery.Emit);
    private void DispatchActivation(MachinePart source, ActivationDelivery delivery)
    {
        if (!Parts.Contains(source)) return;
        var pending = new Queue<(string Id, ActivationDelivery Delivery, ActivationCommand Command)>();
        var seen = new HashSet<(string, ActivationCommand)>();
        pending.Enqueue((source.Uid, delivery, ActivationCommand.Trigger));
        while (pending.TryDequeue(out var entry))
        {
            var id = entry.Id;
            if (!seen.Add((id, entry.Command))) continue;
            var part = FindPart(id);
            if (part == null) continue;
            if (entry.Delivery == ActivationDelivery.Receive &&
                part.HandleActivation(this, entry.Command) == ActivationDisposition.Deferred) continue;
            part.Active = true;
            Events.TryAdd(new(MachineEventKind.Activated, id), Ticks);
            foreach (var link in Connections)
                if (link.From == id && link.Type == ConnectionDomain.Activation && IsValidConnection(link))
                    pending.Enqueue((link.To, ActivationDelivery.Receive,
                        FindPart(link.To)!.ConnectionPorts.Single(p => p.Id == link.ToPort).Command));
        }
    }

    public void Step()
    {
        if (!Running) return;
        MaximumFlightIterationsThisStep = 0;
        foreach (var part in Parts) part.BeforeNetworks(this);
        LightNetwork.Solve(this);
        OpticalNetwork.Solve(this);
        AcousticNetwork.Solve(this);
        ElectricalNetwork.Solve(this);
        Ropes = RopeNetwork.Build(Parts, Connections);
        const float delta = Tick / Substeps;
        for (var iteration = 0; iteration < Substeps; iteration++)
        {
            _assistanceTime += delta;
            foreach (var correction in _corrections) correction.Apply(_assistanceTime);
            foreach (var part in Parts) part.BeforeStep(this, delta);
            AirflowNetwork.Step(this,delta);
            MechanicalNetwork.Solve(this, delta);
            foreach (var part in Parts) part.MechanicalStep(this, delta);
            var guideDistances = Ropes.Select(r => r.GuideDistances()).ToArray();
            foreach (var body in Bodies)
            {
                if (!body.Visible || !body.FreeMotion) continue;
                var acceleration = Vector3.Down * Gravity + Vector3.Up * body.Buoyancy * Pressure;
                body.Velocity += body.InverseMassResponse(acceleration * body.Mass) * delta;
                body.Velocity *= Mathf.Max(0, 1 - body.Drag * Pressure * delta);
                body.Velocity = body.Velocity.LimitLength(40);
                body.ConstrainVelocity();
            }
            // Sample compliant forces before flight, so their impulses participate
            // in this substep's rigid contacts and rope velocity constraints.
            foreach (var body in Bodies)
            {
                if (!body.Visible) continue;
                foreach (var obstacle in Parts)
                {
                    if (obstacle == body || !obstacle.Visible) continue;
                    obstacle.ResolveCompliantContact(body, this, delta);
                }
                body.ConstrainVelocity();
            }
            foreach (var rope in Ropes) rope.SolveVelocity(delta);
            AdvanceBodies(delta);
            foreach (var body in Bodies)
            {
                if (!body.Visible) continue;
                if (body.PhysicsOwner == body && (body.Position.Y < -5 || body.Position.Y > 20 || Mathf.Abs(body.Position.X) > 18 || Mathf.Abs(body.Position.Z) > 12))
                {
                    body.Visible = false;
                    Events.TryAdd(new(MachineEventKind.Escaped, body.Uid), Ticks);
                }
            }
            // Swept flight already resolved rigid contacts; do not apply a second
            // discrete impulse/damping pass to the same movement.
            // Alternate rope projection and contacts so a tether does not pull a load through solids.
            // Only rope loads need this extra post-projection contact pass.
            var ropeLoads = Ropes.SelectMany(r => r.Sockets)
                .Where(s => s.Part.RopeAttachment == RopeAttachmentKind.Load).Select(s => s.Part).Distinct().ToArray();
            for (var pass = 0; pass < 4 && Ropes.Count > 0; pass++)
            {
                foreach (var rope in Ropes) rope.SolvePosition();
                foreach (var body in ropeLoads)
                {
                    if (!body.Visible) continue;
                    foreach (var obstacle in Parts)
                    {
                        if (obstacle == body || !obstacle.Visible) continue;
                        foreach (var box in obstacle.Boxes) CollideBox(body, obstacle.Transform, box, obstacle.SurfaceBounce, obstacle);
                        foreach (var sphere in obstacle.Spheres) CollideStaticSphere(body, obstacle, sphere);
                        foreach (var tube in obstacle.Tubes) CollideTube(body, obstacle, tube);
                        foreach (var bend in obstacle.Bends) CollideBend(body, obstacle, bend);
                        foreach (var frustum in obstacle.Frustums) CollideFrustum(body, obstacle, frustum);
                    }
                    CollideBox(body, Transform3D.Identity, Workbench.Deck, 1);
                    CollideBox(body, Transform3D.Identity, Workbench.Base, 1);
                }
                for (var i = 0; i < Bodies.Count; i++)
                    for (var j = i + 1; j < Bodies.Count; j++) CollideSpheres(Bodies[i], Bodies[j]);
                foreach (var rope in Ropes) rope.SolveVelocity(delta);
            }
            for (var i = 0; i < Ropes.Count; i++) Ropes[i].AnimateGuides(guideDistances[i]);
            foreach (var part in Parts) part.AfterStep(this, delta);
            foreach (var body in Bodies)
            {
                body.QuantizePhysics();
            }
        }
        Ticks++;
        if (!Won && Objectives.Count > 0 && GoalsMet())
        {
            Won = true;
            Running = false;
            Solved?.Invoke();
        }
    }

    private void CollideBox(MachinePart body, Transform3D transform, BoxProxy box, float surfaceBounce, MachinePart? obstacle = null)
    {
        var local = transform.AffineInverse() * body.Position - box.At;
        var closest = local.Clamp(-box.Half, box.Half);
        var difference = local - closest;
        var distance = difference.Length();
        if (distance >= body.Radius) return;
        Vector3 normal;
        if (distance < .00001f)
        {
            var clearance = box.Half - local.Abs();
            var axis = (int)clearance.MinAxisIndex();
            normal = Vector3.Zero;
            normal[axis] = local[axis] >= 0 ? 1 : -1;
            distance = -clearance[axis];
        }
        else normal = difference / distance;
        normal = transform.Basis * normal;
        body.Position += normal * (body.Radius - distance + .00001f);
        var speed = body.Velocity.Dot(normal);
        if (speed >= 0) return;
        body.Velocity -= normal * speed * (1 + body.Bounce * surfaceBounce);
        if (Mathf.Abs(speed) < .45f && normal.Y > .7f)
            body.Velocity -= normal * body.Velocity.Dot(normal);
        ApplySurfaceFriction(body, obstacle, normal, speed);
        obstacle?.OnContact(body, -speed, this);
    }

    private void CollideTube(MachinePart body, MachinePart obstacle, TubeProxy tube)
    {
        var transform = obstacle.Transform * tube.Pose;
        var (localNormal, distance) = tube.Surface(transform.AffineInverse() * body.Position);
        ResolveHollowContact(body, obstacle, transform.Basis * localNormal, distance);
    }

    private void CollideFrustum(MachinePart body, MachinePart obstacle, FrustumProxy frustum)
    {
        var transform = obstacle.Transform * frustum.Pose;
        var (localNormal, distance) = frustum.Surface(transform.AffineInverse() * body.Position);
        ResolveHollowContact(body, obstacle, transform.Basis * localNormal, distance);
    }

    private void CollideBend(MachinePart body, MachinePart obstacle, BendProxy bend)
    {
        var transform = obstacle.Transform * bend.Pose;
        var (localNormal, distance) = bend.Surface(transform.AffineInverse() * body.Position);
        ResolveHollowContact(body, obstacle, transform.Basis * localNormal, distance);
    }

    private void ResolveHollowContact(MachinePart body, MachinePart obstacle, Vector3 normal, float distance)
    {
        if (distance >= body.Radius) return;
        body.Position += normal * (body.Radius - distance + .00001f);
        var speed = body.Velocity.Dot(normal);
        if (speed >= 0) return;
        body.Velocity -= normal * speed * (1 + body.Bounce * obstacle.SurfaceBounce);
        ApplySurfaceFriction(body, obstacle, normal, speed);
        obstacle.OnContact(body, -speed, this);
    }

    private void CollideStaticSphere(MachinePart body, MachinePart obstacle, SphereProxy sphere)
    {
        var offset = body.Position - obstacle.Transform * sphere.At;
        var distance = offset.Length();
        var target = body.Radius + sphere.Radius;
        if (distance >= target) return;
        var normal = distance > .00001f ? offset / distance :
            body.Velocity.LengthSquared() > .00001f ? -body.Velocity.Normalized() : Vector3.Up;
        body.Position += normal * (target - distance + .00001f);
        var approach = body.Velocity.Dot(normal);
        if (approach >= 0) return;
        body.Velocity -= normal * approach * (1 + body.Bounce * obstacle.SurfaceBounce);
        ApplySurfaceFriction(body, obstacle, normal, approach);
        obstacle.OnContact(body, -approach, this);
    }

    private static void CollideSpheres(MachinePart a, MachinePart b)
    {
        if (!a.Visible || !b.Visible) return;
        var offset = b.Position - a.Position;
        var distance = offset.Length();
        var target = a.Radius + b.Radius;
        if (distance >= target) return;
        var normal = distance > .00001f ? offset / distance : Vector3.Right;
        if (!a.FreeMotion && !b.FreeMotion) return;
        BodyContact.Resolve(a, b, -normal, target - distance, Mathf.Min(a.Bounce, b.Bounce));
    }

    private bool GoalsMet() => Objectives.All(goal => goal.Type switch
    {
        GoalKind.Captured => Events.ContainsKey(new(MachineEventKind.Captured, goal.Target, goal.Body)),
        GoalKind.Activated => Events.ContainsKey(new(MachineEventKind.Activated, goal.Target)),
        GoalKind.Powered => Events.ContainsKey(new(MachineEventKind.Powered, goal.Target)),
        GoalKind.Turned => Events.ContainsKey(new(MachineEventKind.Turned, goal.Target)),
        GoalKind.ActivatedAfter => Events.TryGetValue(new(MachineEventKind.Activated, goal.Body), out var emittedTick)
            && Events.TryGetValue(new(MachineEventKind.Activated, goal.Target), out var receivedTick)
            && receivedTick - emittedTick >= Math.Ceiling(goal.MinimumDelaySeconds / Tick),
        GoalKind.PoweredAfter => Events.TryGetValue(new(MachineEventKind.Activated, goal.Body), out var triggerTick)
            && Events.TryGetValue(new(MachineEventKind.Powered, goal.Target), out var poweredTick)
            && poweredTick > triggerTick
            && poweredTick - triggerTick >= Math.Ceiling(goal.MinimumDelaySeconds / Tick),
        _ => false
    });

    public string StateSignature()
    {
        var state = new StringBuilder().Append(Ticks).Append(':').Append(Won);
        foreach (var part in Parts)
        {
            state.Append('|').Append(part.Uid).Append(':').Append(part.Active).Append(':').Append(part.Visible);
            foreach (var value in new[] { part.Position.X, part.Position.Y, part.Position.Z, part.Velocity.X, part.Velocity.Y, part.Velocity.Z })
                state.Append(':').Append(value.ToString("R", CultureInfo.InvariantCulture));
        }
        foreach (var part in Parts)
            foreach (var hinge in part.HingedBodies.OrderBy(h => h.Role))
                // Explicit invariant diagnostic/hash boundary, never a behaviour selector.
                state.Append('|').Append(part.Uid).Append(':').Append((int)hinge.Role)
                    .Append(':').Append(hinge.Joint.Angle.ToString("R", CultureInfo.InvariantCulture))
                    .Append(':').Append(hinge.Joint.AngularVelocity.ToString("R", CultureInfo.InvariantCulture));
        foreach (var pair in Events) state.Append('|').Append(pair.Key).Append(':').Append(pair.Value);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(state.ToString())));
    }
}
