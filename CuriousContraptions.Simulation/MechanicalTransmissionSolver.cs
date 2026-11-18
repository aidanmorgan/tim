using System;
using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;
using static CuriousContraptions.Simulation.ConstraintBodyImpulse;

namespace CuriousContraptions.Simulation;

internal sealed class SimMechanicalTransmission
{
    public readonly SimConstraintCoordinate Driving = new();
    public readonly SimConstraintCoordinate Driven = new();
    public TransmissionConstraintKind ConstraintKind;
    public float Ratio, Scale;
    public readonly SimBody[] Bodies = new SimBody[4];
    public readonly Vector3[] Linear = new Vector3[4], Angular = new Vector3[4];
    public int Count;
    public float InverseMass;
    public bool Engaged;
    public float Impulse;
    /// <summary>The row impulse applied in the open accepted-work window; credited once when the window closes.</summary>
    public float WindowImpulse;
    /// <summary>This substep's signed interface work and its per-window positive transfer, committed after the substep.</summary>
    public float StepWork, StepPositive, Work, PositiveWork;
    public SimPrismaticRetention? Retention;
    public readonly SimPrismaticRetention RetentionSlot = new();
    public int Offset;
}

internal static class MechanicalTransmissionSolver
{
    public static void Prepare(SimMechanicalTransmission transmission)
    {
        transmission.Count = 0;
        if (!transmission.Engaged)
        {
            transmission.InverseMass = 0f;
            transmission.Impulse = 0f;
            return;
        }
        transmission.Driving.Prepare();
        transmission.Driven.Prepare();
        // Positive normalization preserves both bilateral and unilateral domains.
        transmission.Scale = Normalization(transmission);
        AddCoordinate(transmission, transmission.Driving, -transmission.Ratio / transmission.Scale);
        AddCoordinate(transmission, transmission.Driven, 1f / transmission.Scale);
        transmission.InverseMass = Row(transmission).Mass();
        if (transmission.InverseMass <= 0f) transmission.Impulse = 0f;
        if (transmission.ConstraintKind == TransmissionConstraintKind.NonPositiveVelocity)
            transmission.Impulse = MathF.Min(0f, transmission.Impulse);
    }

    /// <summary>Positive row normalization; it preserves both bilateral and unilateral domains.</summary>
    private static float Normalization(SimMechanicalTransmission transmission) => MathF.Max(1f, MathF.Abs(transmission.Ratio));

    private static void AddCoordinate(SimMechanicalTransmission transmission, SimConstraintCoordinate coordinate, float factor)
    {
        var row = coordinate.Row;
        Add(transmission, coordinate.A, row.La * factor, row.Aa * factor);
        Add(transmission, coordinate.B, row.Lb * factor, row.Ab * factor);
    }

    private static void Add(SimMechanicalTransmission transmission, SimBody body, Vector3 linear, Vector3 angular)
    {
        for (var i = 0; i < transmission.Count; i++)
        {
            if (!ReferenceEquals(transmission.Bodies[i], body)) continue;
            transmission.Linear[i] += linear;
            transmission.Angular[i] += angular;
            return;
        }
        var slot = transmission.Count++;
        transmission.Bodies[slot] = body;
        transmission.Linear[slot] = linear;
        transmission.Angular[slot] = angular;
    }

    public static void Solve(SimMechanicalTransmission transmission)
    {
        if (transmission.InverseMass <= 0f) return;
        var velocity = 0f;
        for (var i = 0; i < transmission.Count; i++)
            velocity += ConstraintVelocity(transmission.Bodies[i], transmission.Linear[i], transmission.Angular[i]);
        var previous = transmission.Impulse;
        var next = previous - velocity / transmission.InverseMass;
        if (transmission.ConstraintKind == TransmissionConstraintKind.NonPositiveVelocity) next = MathF.Min(0f, next);
        transmission.Impulse = next;
        Apply(transmission, next - previous);
    }

    private static ImpulseJacobian Row(SimMechanicalTransmission transmission) =>
        new(transmission.Bodies.AsSpan(0, transmission.Count), transmission.Linear, transmission.Angular);

    public static void Apply(SimMechanicalTransmission transmission, float impulse)
    {
        if (!transmission.Engaged || impulse == 0f) return;
        Row(transmission).Apply(impulse);
        transmission.WindowImpulse += impulse;
    }

    /// <summary>Closes the accepted-work window: the driven coordinate's contribution of the window's whole row impulse at the
    /// window's start and end rates. Positive transfer takes only the complete window's positive part.</summary>
    public static void CloseWorkWindow(SimMechanicalTransmission transmission)
    {
        if (transmission.WindowImpulse == 0f) return;
        // The window's own coordinate row at its fixed pose, also for a carried window whose relation has since disengaged.
        transmission.Driven.Prepare();
        // A disengaged relation is not prepared this substep, so the row normalization is evaluated here as well.
        var work = transmission.WindowImpulse / Normalization(transmission) *
            (.5f * transmission.Driven.WindowStartRate + .5f * transmission.Driven.Rate);
        transmission.WindowImpulse = 0f;
        transmission.StepWork += work;
        transmission.StepPositive += MathF.Max(0f, work);
    }
}
