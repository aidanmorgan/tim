using System;

namespace CuriousContraptions;

public enum MachineEventKind { Activated, Escaped, Captured, Bounced, Bumped, Transported, Powered, Turned }

/// <summary>Typed runtime identity; target/body remain extensible instance IDs.</summary>
public readonly record struct MachineEvent(MachineEventKind Kind, string Target, string Body = "")
    : IComparable<MachineEvent>
{
    public int CompareTo(MachineEvent other)
    {
        var order = Kind.CompareTo(other.Kind);
        if (order != 0) return order;
        order = string.CompareOrdinal(Target, other.Target);
        return order != 0 ? order : string.CompareOrdinal(Body, other.Body);
    }
    public override string ToString() => Body.Length == 0 ? $"{Kind}:{Target}" : $"{Kind}:{Target}:{Body}";
}
