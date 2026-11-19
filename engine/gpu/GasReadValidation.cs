using System;

namespace CuriousContraptions.Gpu;

internal static class GasReadValidation
{
    public static void ValidateBodies(WorkshopRead read)
    {
        for (var i = 0; i < read.GasStores.Count; i++)
        {
            var gas = read.GasStores[i];
            gas.Validate();
            if (!read.Bodies.TryGet(gas.Body, out _))
                throw new ArgumentException("Gas observation names a foreign dynamic body.");
        }
        for (var i = 0; i < read.IntegrityObservers.Count; i++)
        {
            var observation = read.IntegrityObservers[i];
            observation.Validate();
            if (observation.Phase == IntegrityPhase.Broken &&
                (read.Motion is not { } motion || observation.Ordinal > motion.LastOrdinal))
                throw new ArgumentException("Integrity occurrence is outside committed physical time.");
        }
    }

    public static void ValidateOwners(WorkshopRead read, ICompiledScene scene)
    {
        var stores = scene.Physics.GasStores;
        var observers = scene.Physics.IntegrityObservers;
        if (read.GasStores.Count != (read.Epoch.Value == 0 ? 0 : stores.Length) ||
            read.IntegrityObservers.Count != (read.Epoch.Value == 0 ? 0 : observers.Length))
            throw new ArgumentException("Gas observation population differs from installation.");
        for (var i = 0; i < read.GasStores.Count; i++)
        {
            var value = read.GasStores[i]; var declared = stores[i];
            IsobaricGas.ValidateRuntime(declared.Prepared.State, value.State);
            if (value.Id != declared.Id || value.Body != declared.Body ||
                value.PreparationWork != declared.Prepared.BoundaryWorkOut ||
                value.PreparationElastic != declared.Prepared.ElasticEnergyChange)
                throw new ArgumentException("Gas observation differs from its declared ownership or preparation.");
            if (read.PhysicalRevision.IsAdmission &&
                (value.State != declared.Prepared.State || value.Outlet != GasOutletPhase.Closed ||
                 value.EnthalpyOut != 0 || value.BoundaryWork != 0 || value.ElasticReleased != 0 ||
                 value.KineticOut != 0 || value.MomentumOut != System.Numerics.Vector3.Zero))
                throw new ArgumentException("Admission carries transient gas state.");
        }
        for (var i = 0; i < read.IntegrityObservers.Count; i++)
        {
            var value = read.IntegrityObservers[i]; var declared = observers[i];
            if (value.Id != declared.Id ||
                read.PhysicalRevision.IsAdmission && value.Phase != IntegrityPhase.Intact ||
                !read.GasStores.TryGet(declared.Store, out var gas) ||
                (value.Phase == IntegrityPhase.Broken) != (gas.Outlet == GasOutletPhase.Open))
                throw new ArgumentException("Integrity observation differs from its declared outlet or admission.");
        }
    }

    public static void ValidateAdvance(WorkshopRead current, WorkshopRead previous)
    {
        if (current.GasStores.Count != previous.GasStores.Count || current.IntegrityObservers.Count != previous.IntegrityObservers.Count)
            throw new ArgumentException("Gas observation population changed.");
        for (var i = 0; i < current.GasStores.Count; i++)
        {
            var a = current.GasStores[i]; var b = previous.GasStores[i];
            if (a.Id != b.Id || a.Body != b.Body || (uint)a.Outlet < (uint)b.Outlet ||
                a.State.Mass > b.State.Mass || a.State.Volume > b.State.Volume || a.State.Energy > b.State.Energy ||
                a.EnthalpyOut < b.EnthalpyOut || a.BoundaryWork < b.BoundaryWork ||
                a.ElasticReleased < b.ElasticReleased || a.KineticOut < b.KineticOut ||
                a.PreparationWork != b.PreparationWork || a.PreparationElastic != b.PreparationElastic)
                throw new ArgumentException("Gas inventory or ledger advanced unlawfully.");
        }
        for (var i = 0; i < current.IntegrityObservers.Count; i++)
        {
            var a = current.IntegrityObservers[i]; var b = previous.IntegrityObservers[i];
            if (a.Id != b.Id || b.Phase == IntegrityPhase.Broken && a != b ||
                a.Phase == IntegrityPhase.Broken && b.Phase == IntegrityPhase.Intact &&
                a.Ordinal <= (previous.Motion?.LastOrdinal ?? 0))
                throw new ArgumentException("Integrity occurrence changed or predates its committed transition.");
        }
    }
}
