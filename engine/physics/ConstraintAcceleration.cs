using System;

namespace CuriousContraptions.Physics;

public enum AccelerationRelation { Equal, Nonnegative, Nonpositive }

/// <summary>J a + bias satisfies Relation. ConvectiveAcceleration is the instantaneous
/// geometric J-dot v term for direct acceleration solves, or the finite-interval
/// geometric bias used by support prediction. External forces remain separate.</summary>
public sealed record ConstraintAcceleration
{
    public ConstraintGradient Gradient { get; }
    public double ConvectiveAcceleration { get; }
    public AccelerationRelation Relation { get; }
    public ConstraintAcceleration(ConstraintGradient gradient,double convectiveAcceleration,AccelerationRelation relation)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        if(!double.IsFinite(convectiveAcceleration)||!Enum.IsDefined(relation))
            throw new ArgumentException("Acceleration row requires a finite bias and defined relation.");
        Gradient=gradient; ConvectiveAcceleration=convectiveAcceleration; Relation=relation;
    }
}
