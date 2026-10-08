using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>One world-owned gas inventory coupled to one owned slider through permanent geometry.
/// Opposite chambers use separate node identities and signed areas.</summary>
public sealed record AxialGasLoad
{
    public PhysicsGasNodeId Node { get; }
    public PhysicsJointId Joint { get; }
    public AxialGasGeometry Geometry { get; }
    public double WorkTolerance { get; }

    public AxialGasLoad(PhysicsGasNodeId node, PhysicsJointId joint, AxialGasGeometry geometry, double workTolerance)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        if(!double.IsFinite(workTolerance)||workTolerance<=0||workTolerance/16==0)
            throw new ArgumentOutOfRangeException(nameof(workTolerance));
        WorkTolerance = workTolerance;
        Node = node;
        Joint = joint;
        Geometry = geometry;
    }

    public PhysicsFrameJoint Resolve(IEnumerable<PhysicsJoint> joints)
    {
        ArgumentNullException.ThrowIfNull(joints);
        if (joints.SingleOrDefault(joint => joint.Id == Joint) is not PhysicsFrameJoint frame ||
            frame.Kind != FrameJointKind.Slider)
            throw new ArgumentException("Gas chamber requires its current owned slider.");
        return frame;
    }

    public AxialGasPotential Bind(IReadOnlyList<PhysicsJoint> joints,
        IReadOnlyDictionary<PhysicsGasNodeId, PhysicsGasNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        var joint = Resolve(joints);
        if (!nodes.TryGetValue(Node, out var node) || node is null || node.Id != Node ||
            node.Owner != joint.A.Id && node.Owner != joint.B.Id)
            throw new ArgumentException("Gas chamber must reference an inventory owned by a slider participant.");
        return new(node.State, joint.Travel.Error, Geometry);
    }
}
