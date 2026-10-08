using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>One work coordinate composed from typed physical ports. The same
/// effort acts through every component; shared-body Jacobians sum before solve.</summary>
public sealed record CompositePowerPort : MechanicalPowerPort
{
    public IReadOnlyList<MechanicalPowerPort> Components { get; }
    public CompositePowerPort(IEnumerable<MechanicalPowerPort> components)
    {
        ArgumentNullException.ThrowIfNull(components);
        var flat=new List<MechanicalPowerPort>();
        foreach(var component in components)
        {
            ArgumentNullException.ThrowIfNull(component);
            if(component is CompositePowerPort composite)flat.AddRange(composite.Components);
            else flat.Add(component);
        }
        if(flat.Count==0)throw new ArgumentException("A composite port requires physical components.");
        Components=Array.AsReadOnly(flat.ToArray());
    }
    public override ConstraintGradient Bind(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,IReadOnlyList<PhysicsJoint> joints)=>
        new(Components.SelectMany(port=>port.Bind(bodies,joints).Terms.ToArray()).ToArray());
    public bool Equals(CompositePowerPort? other)=>other is not null&&Components.SequenceEqual(other.Components);
    public override int GetHashCode()
    {
        var hash=new HashCode();
        foreach(var component in Components)hash.Add(component);
        return hash.ToHashCode();
    }
}
