using System;
using System.Linq;

namespace CuriousContraptions;

public sealed class OscillatorSlot;
public readonly record struct SceneOscillatorKey(MachinePart Owner,OscillatorSlot Slot);
public readonly record struct SceneOscillatorDeclaration(SceneOscillatorKey Key,double IntervalSeconds,SocketId PowerInput)
{
    internal int IntervalTicks(double tickSeconds)
    {
        var input=PowerInput;
        if(Key.Owner is null||Key.Slot is null||!Enum.IsDefined(PowerInput)||
            !double.IsFinite(IntervalSeconds)||IntervalSeconds<=0||!double.IsFinite(tickSeconds)||tickSeconds<=0||
            !Key.Owner.ConnectionPorts.Any(p=>p.Id==input&&p.Domain==ConnectionDomain.Electrical&&p.Direction==PortDirection.Input))
            throw new ArgumentException("Oscillator requires a declared electrical input and finite positive interval.");
        return checked((int)Math.Ceiling(IntervalSeconds/tickSeconds));
    }
}
