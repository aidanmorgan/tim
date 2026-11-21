using System;
using System.Collections.Generic;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public readonly record struct EnergyStoreId(ulong Value);
public readonly record struct LocalEnergyStoreId(uint Value);

/// <summary>Work accepted in one physical commit from an exact finite store.</summary>
public readonly record struct EnergyStoreDebit(EnergyStoreId Store, Joules Work);

/// <summary>A reservoir independent of its charging route and discharge mechanisms. Quantities are joules and watts.</summary>
public readonly record struct EnergyStoreDeclaration(EnergyStoreId Id, GpuBodyId Owner,
    Joules Capacity, Joules InitialEnergy, Watts MaximumChargePower)
{
    public void Validate()
    {
        Capacity.Validate(float.MaxValue);
        InitialEnergy.Validate(Capacity.Value);
        if (Id.Value == 0 || Owner.Value == 0 || !float.IsFinite(MaximumChargePower.Value) || MaximumChargePower.Value < 0f)
            throw new ArgumentException("Invalid finite energy store.");
    }
}

/// <summary>Committed balance and cumulative work, independently identified from any actuator or contact.</summary>
public readonly record struct EnergyStoreRead(EnergyStoreId Id, GpuBodyId Owner, FiniteEnergyBalance Balance,
    Joules Supplied, Joules Released, Joules Dissipated, Joules CommitSupplied,
    Joules CommitReleased, Joules CommitDissipated, uint Transactions)
{
    public Joules Energy => new(Balance.Display);

    public void Validate()
    {
        if (Id.Value == 0 || Owner.Value == 0) throw new ArgumentException("Invalid energy store identity.");
        Balance.Validate(float.MaxValue);
        CommitSupplied.Validate(float.MaxValue);
        CommitReleased.Validate(float.MaxValue);
        CommitDissipated.Validate(float.MaxValue);
        Supplied.Validate(float.MaxValue);
        Released.Validate(float.MaxValue);
        Dissipated.Validate(float.MaxValue);
    }
}

/// <summary>A declared energy expression: either an energy value, or kinetic energy from explicit mass and speed.</summary>
public enum EnergyExpressionKind : byte { Direct, Kinetic }

public readonly record struct EnergyExpression(EnergyExpressionKind Kind, MechanicalValue Value, MechanicalValue Speed)
{
    public static EnergyExpression Direct(MechanicalValue joules) => new(EnergyExpressionKind.Direct, joules, MechanicalValue.Fixed(0f));
    public static EnergyExpression Kinetic(MechanicalValue mass, MechanicalValue speed) => new(EnergyExpressionKind.Kinetic, mass, speed);

    public float Evaluate(IReadOnlyDictionary<string, float> parameters)
    {
        if (!Enum.IsDefined(Kind)) throw new ArgumentException("Undefined energy expression.");
        var value = Value.Evaluate(parameters);
        if (value < 0f) throw new ArgumentException("Negative energy operand.");
        if (Kind == EnergyExpressionKind.Direct)
        {
            if (Speed != MechanicalValue.Fixed(0f)) throw new ArgumentException("Direct energy has no speed operand.");
            return value;
        }
        var speed = Speed.Evaluate(parameters);
        var energy = .5f * value * speed * speed;
        if (speed < 0f || !float.IsFinite(energy)) throw new ArgumentException("Invalid kinetic energy expression.");
        return energy;
    }
}

/// <summary>Package-owned capacity, initial charge and charging demand. The owner is an exact package body.</summary>
public readonly record struct EnergyStoreDescriptor(LocalEnergyStoreId Id, PackageBodyIndex Owner,
    EnergyExpression Capacity, EnergyExpression InitialEnergy, MechanicalValue MaximumChargePower,
    [property: System.Text.Json.Serialization.JsonRequired] ChoicePredicate? Selection = null);
