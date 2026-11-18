using System.Collections.Generic;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

/// <summary>Reusable connected-component factors, rebuilt from each accepted or restored substep state.</summary>
internal sealed class GuideWorkspace
{
    private readonly Dictionary<SimBody, SimBody> _parents = new();
    private readonly Dictionary<SimBody, GuideComponent> _active = new();
    private readonly List<GuideComponent> _pool = new();

    public static void Detach(List<SimBody> bodies)
    {
        foreach (var body in bodies) body.Guide = null;
    }

    public void Prepare(List<SimBody> bodies, List<SimJoint> joints, bool useBias, bool clearReactions)
    {
        Detach(bodies); _parents.Clear(); _active.Clear();
        foreach (var joint in joints) Connect(joint.A, joint.B);
        foreach (var joint in joints)
        {
            var dynamicBody = joint.A.Motion == RigidMotionKind.Dynamic ? joint.A : joint.B;
            if (dynamicBody.Motion != RigidMotionKind.Dynamic) continue;
            var root = Root(dynamicBody);
            if (!_active.TryGetValue(root, out var component))
            {
                var index = _active.Count;
                if (index == _pool.Count) _pool.Add(new GuideComponent());
                component = _pool[index]; component.Clear();
                _active.Add(root, component);
            }
            component.AddBody(joint.A); component.AddBody(joint.B);
            for (var row = 0; row < 5; row++) component.Equations.Add(new(joint, row));
            if (joint.Holding) component.Equations.Add(new(joint, (int)PrismaticRowKind.Spring));
        }
        foreach (var component in _active.Values) component.Prepare(useBias, clearReactions);
    }

    public void FlushReactions()
    {
        foreach (var component in _active.Values) component.FlushReactions();
    }

    private SimBody Root(SimBody body)
    {
        if (!_parents.TryGetValue(body, out var parent)) { _parents.Add(body, body); return body; }
        if (parent != body) _parents[body] = parent = Root(parent);
        return parent;
    }

    private void Connect(SimBody a, SimBody b)
    {
        if (a.Motion != RigidMotionKind.Dynamic || b.Motion != RigidMotionKind.Dynamic) return;
        var ra = Root(a); var rb = Root(b);
        if (ra != rb) _parents[rb] = ra;
    }
}
