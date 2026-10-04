using System;

namespace CuriousContraptions.Gpu;

public sealed class PhysicsNumericalException : InvalidOperationException
{
    public PhysicsFailure Failure { get; }
    public PhysicsNumericalException(PhysicsFailure failure) : base("Generic GPU candidate failed before commit.")
    {
        if (!Enum.IsDefined(failure) || failure == PhysicsFailure.None) throw new ArgumentException("Invalid numerical failure.");
        Failure = failure;
    }
}
