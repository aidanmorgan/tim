using System;
using System.Collections.Generic;
using System.Numerics;

namespace CuriousContraptions.Abstractions.Mechanical;

/// <summary>A degree-at-most-three f32 polynomial in one declared real parameter. Constants use the same Horner evaluator.</summary>
public readonly record struct MassScalar(ParameterId? Parameter, float C0, float C1, float C2, float C3)
{
    public static MassScalar Constant(float value) => new(null, value, 0f, 0f, 0f);

    public float Evaluate(IReadOnlyDictionary<ParameterId, float> parameters)
    {
        if (!float.IsFinite(C0) || !float.IsFinite(C1) || !float.IsFinite(C2) || !float.IsFinite(C3))
            throw new ArgumentException("Mass polynomial coefficients must be finite.");
        var x = 0f;
        if (Parameter is { } key)
        {
            if (key == default || !parameters.TryGetValue(key, out x) || !float.IsFinite(x))
                throw new ArgumentException("Mass polynomial requires its declared finite parameter.");
        }
        else if (C1 != 0f || C2 != 0f || C3 != 0f)
            throw new ArgumentException("A constant mass polynomial cannot have variable coefficients.");
        var value = ((C3 * x + C2) * x + C1) * x + C0;
        if (!float.IsFinite(value)) throw new ArgumentException("Mass polynomial overflow.");
        return value;
    }
}

public readonly record struct MassVector(MassScalar X, MassScalar Y, MassScalar Z)
{
    public static MassVector Constant(Vector3 value) => new(MassScalar.Constant(value.X), MassScalar.Constant(value.Y), MassScalar.Constant(value.Z));
    public Vector3 Evaluate(IReadOnlyDictionary<ParameterId, float> parameters) =>
        new(X.Evaluate(parameters), Y.Evaluate(parameters), Z.Evaluate(parameters));
}

/// <summary>Package-owned mass frame, independent of every collision child.</summary>
public readonly record struct MassFrameDeclaration(MassScalar Mass, MassVector CentreOfMass, MassVector PrincipalMoments,
    Quaternion PrincipalFrame, float LinearDrag, Vector3 Gravity)
{
    public static MassFrameDeclaration Constant(float mass, Vector3 centreOfMass, Vector3 principalMoments,
        Quaternion principalFrame, float linearDrag, Vector3 gravity) =>
        new(MassScalar.Constant(mass), MassVector.Constant(centreOfMass), MassVector.Constant(principalMoments),
            principalFrame, linearDrag, gravity);

    public RigidBodyMassProperties Evaluate(IReadOnlyDictionary<ParameterId, float> parameters) =>
        new(Mass.Evaluate(parameters), CentreOfMass.Evaluate(parameters), PrincipalMoments.Evaluate(parameters),
            PrincipalFrame, LinearDrag, Gravity);
}

public enum PayloadPolicy : byte { None, CaptureTarget }
