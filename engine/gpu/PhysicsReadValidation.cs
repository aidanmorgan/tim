using System;

namespace CuriousContraptions.Gpu;

public static class PhysicsReadValidation
{
    public static void ValidateScene(this PhysicsBodyReadSet bodies, IPhysicsSceneDeclaration scene, SimulationEpoch epoch, SimulationTick tick, PhysicalCommitRevision revision)
    {
        bodies.ValidateTime(epoch, tick);
        var index = 0;
        foreach (var declared in scene.Bodies)
        {
            if (declared.Motion != RigidMotionKind.Dynamic) continue;
            if (index >= bodies.Count || bodies[index].Body.Id != declared.Id)
                throw new ArgumentException("Complete body set differs from the installed scene.");
            var value = bodies[index++]; value.Validate();
            if (!F32Bits.Equal(value.LocalCentreOfMass, declared.MassProperties.LocalCentreOfMass))
                throw new ArgumentException("Committed centre of mass differs from declaration.");
            if (revision.IsAdmission && (value.Body.Cell != declared.Cell ||
                !F32Bits.Equal(value.Body.Local, declared.Local) ||
                !value.Body.Velocity.HasSameBits(declared.Velocity) ||
                !F32Bits.Equal(value.Rotation.X, declared.Rotation.X) ||
                !F32Bits.Equal(value.Rotation.Y, declared.Rotation.Y) ||
                !F32Bits.Equal(value.Rotation.Z, declared.Rotation.Z) ||
                !F32Bits.Equal(value.Rotation.W, declared.Rotation.W) ||
                !value.AngularVelocity.HasSameBits(declared.AngularVelocity)))
                throw new ArgumentException("Admission changed a canonical body.");
        }
        if (index != bodies.Count) throw new ArgumentException($"Read contains a foreign dynamic body: expected {index}, received {bodies.Count}, scene bodies {scene.Bodies.Length}, epoch {epoch.Value}, tick {tick.Value}.");
    }

    public static void ValidateAdvance(this PhysicsElectricalRead current, PhysicsElectricalRead previous)
    {
        if (current.Count != previous.Count) throw new ArgumentException("Electrical source population changed.");
        for (var i = 0; i < current.Count; i++)
        {
            var before = previous[i]; var after = current[i];
            if (before.Id != after.Id || before.Owner != after.Owner ||
                !AccountsFor(before.Balance, after.Balance, 0, after.Debit.Value, 0, after.Transactions))
                throw new ArgumentException("Electrical source debit changed identity or balance.");
        }
    }
    // Boundary validation only: two ULPs cover a rounded sum/subtraction; ordinary residuals do not fault valid ticks.
    public static float Residual(float balance) =>
        2f * (balance == float.MaxValue ? balance - MathF.BitDecrement(balance)
            : MathF.Max(float.Epsilon, MathF.BitIncrement(balance) - balance));

    public static void ValidateSummary(float before, float after, float accepted, uint operations)
    {
        if (after < before || (accepted == 0 || operations == 0) && (accepted != 0 || after != before))
            throw new ArgumentException("Cumulative energy observation differs from accepted work.");
        var scale = MathF.Max(after, accepted);
        if (scale == 0) return;
        var residual = EnergyReadInterval.Ratio(after, scale) - EnergyReadInterval.Ratio(before, scale) -
            EnergyReadInterval.Ratio(accepted, scale);
        var error = EnergyReadInterval.AddUpper(EnergyReadInterval.Error(after, operations, scale),
            EnergyReadInterval.Error(accepted, operations, scale));
        if (!residual.Overlaps(-error, error))
            throw new ArgumentException("Cumulative energy observation differs from accepted work.");
    }

    public static void ValidateAdvance(this PhysicsEnergyStoreRead current, PhysicsEnergyStoreRead previous)
    {
        if (current.Count != previous.Count) throw new ArgumentException("Energy store population changed.");
        for (var i = 0; i < current.Count; i++)
        {
            var a = previous[i]; var b = current[i];
            if (a.Id != b.Id || a.Owner != b.Owner || b.Supplied.Value < a.Supplied.Value ||
                b.Released.Value < a.Released.Value || b.Dissipated.Value < a.Dissipated.Value)
                throw new ArgumentException("Energy store identity or cumulative accounting changed illegally.");
            ValidateSummary(a.Supplied.Value, b.Supplied.Value, b.CommitSupplied.Value, b.Transactions);
            ValidateSummary(a.Released.Value, b.Released.Value, b.CommitReleased.Value, b.Transactions);
            ValidateSummary(a.Dissipated.Value, b.Dissipated.Value, b.CommitDissipated.Value, b.Transactions);
            var supplied = b.CommitSupplied.Value;
            var released = b.CommitReleased.Value;
            var dissipated = b.CommitDissipated.Value;
            if (!AccountsFor(a.Balance, b.Balance, supplied, released, dissipated, b.Transactions))
                throw new ArgumentException("Energy store balance does not account for accepted work.");
        }
    }

    private static bool AccountsFor(FiniteEnergyBalance before, FiniteEnergyBalance after,
        float supplied, float released, float dissipated, uint operations)
    {
        if (operations == 0)
            return supplied == 0 && released == 0 && dissipated == 0 && before == after;
        var scale = MathF.Max(MathF.Max(before.Bulk, after.Bulk),
            MathF.Max(MathF.Max(before.Remainder, after.Remainder), MathF.Max(supplied, MathF.Max(released, dissipated))));
        if (scale == 0) return true;
        var prior = EnergyReadInterval.Balance(before, scale);
        var credit = EnergyReadInterval.Ratio(supplied, scale);
        var release = EnergyReadInterval.Ratio(released, scale);
        var loss = EnergyReadInterval.Ratio(dissipated, scale);
        var ec = EnergyReadInterval.Error(supplied, operations, scale);
        var er = EnergyReadInterval.Error(released, operations, scale);
        var ed = EnergyReadInterval.Error(dissipated, operations, scale);
        var error = EnergyReadInterval.AddUpper(ec, EnergyReadInterval.AddUpper(er, ed));
        var peak = EnergyReadInterval.AddUpper(prior.Upper, EnergyReadInterval.AddUpper(credit.Upper, ec));
        var total = EnergyReadInterval.AddUpper(error,
            EnergyReadInterval.AddUpper(credit.Upper, EnergyReadInterval.AddUpper(release.Upper, loss.Upper)));
        var bound = EnergyReadInterval.Loss(peak, total, operations, scale);
        var residual = prior - EnergyReadInterval.Balance(after, scale) - release + credit - loss;
        return residual.Overlaps(-error, EnergyReadInterval.AddUpper(error, bound));
    }

    internal static void ValidateElectricalBudget(float debit, uint operations, FiniteEnergyBalance source,
        FiniteEnergyBalance phase, float quantum, uint windows)
    {
        var scale = MathF.Max(debit, MathF.Max(quantum,
            MathF.Max(MathF.Max(source.Bulk, source.Remainder), MathF.Max(phase.Bulk, phase.Remainder))));
        if (scale == 0) return;
        var budget = MathF.Min(EnergyReadInterval.Balance(source, scale).Upper,
            EnergyReadInterval.AddUpper(EnergyReadInterval.Balance(phase, scale).Upper,
                EnergyReadInterval.MultiplyUpper(EnergyReadInterval.Ratio(quantum, scale).Upper,
                    EnergyReadInterval.CountUpper(windows))));
        if ((operations == 0 || budget == 0) && debit != 0 ||
            EnergyReadInterval.Accepted(debit, operations, scale).Lower > budget)
            throw new ArgumentException("Electrical source exceeded its phase power allowance.");
    }

    internal static void ValidateConnectedCredits(float debit, uint operations, float maximum,
        float normalizedSum, ulong terms)
    {
        if (debit == 0 && terms != 0 || operations == 0 && (debit != 0 || terms != 0))
            throw new ArgumentException("Electrical debit does not fund its connected storage credits.");
        var scale = MathF.Max(debit, maximum);
        if (scale == 0) return;
        // Every positive child addition has one corresponding source payment.
        var folding = EnergyReadInterval.Error(normalizedSum, terms, 1f);
        var division = EnergyReadInterval.AddUpper(
            EnergyReadInterval.MultiplyUpper(2f * EnergyReadInterval.UnitRoundoff,
                EnergyReadInterval.AddUpper(normalizedSum, folding)),
            EnergyReadInterval.MultiplyUpper(EnergyReadInterval.CountUpper(terms), float.Epsilon));
        var error = EnergyReadInterval.AddUpper(EnergyReadInterval.Error(maximum, operations, scale),
            EnergyReadInterval.AddUpper(folding, division));
        var credits = EnergyReadInterval.Point(normalizedSum).Expand(error);
        var payment = EnergyReadInterval.Accepted(debit, operations, scale);
        if (!credits.Overlaps(payment.Lower, payment.Upper))
            throw new ArgumentException("Electrical debit does not fund its connected storage credits.");
    }

    public static void ValidateAdvance(this PhysicsContactWorkRead current, PhysicsContactWorkRead previous)
    {
        if (current.Count != previous.Count) throw new ArgumentException("Work population changed within an epoch.");
        for (var owner=0; owner<current.Count; owner++)
        {
            var before=previous[owner]; var after=current[owner];
            if (before.Id != after.Id || before.Owner != after.Owner || before.Store != after.Store || after.OccurrenceCount < before.OccurrenceCount)
                throw new ArgumentException("Work owner state reversed or changed identity.");
        }
    }

    public static void ValidateScene(this PhysicsContactWorkRead current, IPhysicsSceneDeclaration scene)
    {
        if (current.Count != scene.ContactWorks.Length) throw new ArgumentException("Work-owner population differs from scene.");
        for (var i=0; i<current.Count; i++)
        {
            var declared=scene.ContactWorks[i];
            if (current[i].Id != declared.Id || current[i].Owner != declared.Owner ||
                current[i].Store != declared.Store)
                throw new ArgumentException("Work owner or reservoir differs from declaration.");
        }
    }

    public static void ValidateAdvance(this PhysicsCaptureRead captures, PhysicsCaptureRead previous)
    {
        for(var i=0;i<previous.Count;i++)
        {
            var prior=previous[i];
            if(prior.Phase==CaptureLatchPhase.Latched && (!captures.TryGet(new(prior.Sensor,prior.Target),out var current) || current!=prior))
                throw new ArgumentException("Earned capture state was lost or changed.");
        }
    }
}

 // Read-boundary enclosure only; these values never authorize settlement or alter physical work.
internal readonly record struct EnergyReadInterval(float Lower, float Upper)
{
    internal const float UnitRoundoff = 1f / 16777216f;
    internal static EnergyReadInterval Point(float value) => new(value, value);
    internal static float CountUpper(ulong count)
    {
        var value = (float)count;
        return (ulong)value < count ? MathF.BitIncrement(value) : value;
    }
    internal static EnergyReadInterval Ratio(float value, float scale)
    {
        if (value == 0) return default;
        var ratio = value / scale;
        return new(MathF.Max(0, MathF.BitDecrement(ratio)), MathF.BitIncrement(ratio));
    }
    internal static EnergyReadInterval Balance(FiniteEnergyBalance value, float scale) =>
        Ratio(value.Bulk, scale) + Ratio(value.Remainder, scale);
    internal static EnergyReadInterval Accepted(float value, uint count, float scale) =>
        Ratio(value, scale).Expand(Error(value, count, scale));
    internal EnergyReadInterval Expand(float error) =>
        this + new EnergyReadInterval(-error, error);
    internal bool Overlaps(float lower, float upper) => Lower <= upper && Upper >= lower;
    internal static float Error(float summary, ulong count, float scale) =>
        summary == 0 || count == 0 ? 0 : MultiplyUpper(CountUpper(count),
            AddUpper(MultiplyUpper(2f * UnitRoundoff, Ratio(summary, scale).Upper), Ratio(float.Epsilon, scale).Upper));
    internal static float Loss(float peak, float total, uint count, float scale) =>
        AddUpper(MultiplyUpper(MultiplyUpper(64f * UnitRoundoff * UnitRoundoff, CountUpper(count)), peak),
            AddUpper(MultiplyUpper(8f * UnitRoundoff, total),
                MultiplyUpper(MultiplyUpper(64f, CountUpper(count)), Ratio(float.Epsilon, scale).Upper)));
    internal static float AddUpper(float left, float right) =>
        left == 0 ? right : right == 0 ? left : MathF.BitIncrement(left + right);
    private static float AddLower(float left, float right) =>
        left == 0 ? right : right == 0 ? left : left == -right ? 0 : MathF.BitDecrement(left + right);
    internal static float MultiplyUpper(float left, float right) =>
        left == 0 || right == 0 ? 0 : MathF.BitIncrement(left * right);
    public static EnergyReadInterval operator +(EnergyReadInterval left, EnergyReadInterval right) =>
        new(AddLower(left.Lower, right.Lower), AddUpper(left.Upper, right.Upper));
    public static EnergyReadInterval operator -(EnergyReadInterval left, EnergyReadInterval right) =>
        left + new EnergyReadInterval(-right.Upper, -right.Lower);
}
