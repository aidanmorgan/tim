using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Bundle;

/// <summary>The one resolver of run-controllable lanes (engine-run-control-table). The validator, the declaration lookup and the
/// compiler all ask it which lanes a declared parameter binds; a RunControl parameter must bind exactly one.</summary>
public static class RunControlBindings
{
    /// <summary>Every run-controllable lane the declaration binds to the parameter <paramref name="key"/>: a Source store's enable
    /// state is the SourceEnable lane, and each hinge drive whose target speed is the parameter is one RevoluteDriveTarget lane.</summary>
    public static IReadOnlyList<RunControlLane> Lanes(BundleEntry entry, string key)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var lanes = new List<RunControlLane>(1);
        foreach (var source in (entry.Electrical?.SupplyGraph?.Nodes ?? []).OfType<CuriousContraptions.Abstractions.Electrical.SupplySourceDescriptor>())
            if (source.Store.Enable.Value == key) lanes.Add(RunControlLane.SourceEnable);
        if (entry.Mechanical is { } mechanical)
            foreach (var hinge in mechanical.Hinges)
                if (hinge.Drive is { TargetSpeed: { Source: MechanicalValueSource.Parameter } target } && target.Parameter == key)
                    lanes.Add(RunControlLane.RevoluteDriveTarget);
        return lanes;
    }

    private static bool Matches(MechanicalValue value, string key) =>
        (value.Source is MechanicalValueSource.Parameter or MechanicalValueSource.NegatedParameter && value.Parameter == key) ||
        (value.Product is { } product &&
            ((product.Left.Source == MechanicalValueSource.Parameter && product.Left.Parameter == key) ||
             (product.Right.Source == MechanicalValueSource.Parameter && product.Right.Parameter == key)));

    private static bool Matches(MassScalar scalar, string key) =>
        scalar.Parameter?.Value == key;

    private static bool Matches(MassVector vector, string key) =>
        Matches(vector.X, key) || Matches(vector.Y, key) || Matches(vector.Z, key);

    private static bool Matches(MassFrameDeclaration frame, string key) =>
        Matches(frame.Source.InitialMass, key) || Matches(frame.CentreOfMass, key) || Matches(frame.Source.DeclaredMoments.Polynomial, key) ||
        frame.Source.DeclaredMoments.Mass.Parameter?.Value == key || frame.Source.DeclaredMoments.Length.Parameter?.Value == key ||
        Matches(frame.LinearDrag, key) || frame.Gravity.Parameter?.Value == key;

    /// <summary>Whether the parameter <paramref name="key"/> is bound to any property that is not a run-control lane.
    /// RunControl parameters must bind strictly to their run-control lane.</summary>
    public static bool IsBoundToNonLane(BundleEntry entry, string key)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return IsBoundToDeclarations(entry, key) ||
               IsBoundToElectrical(entry.Electrical, key) ||
               IsBoundToMechanical(entry.Mechanical, key) ||
               IsBoundToSignal(entry.Signal, key);
    }

    private static bool IsBoundToDeclarations(BundleEntry entry, string key) =>
        entry.MaterialBindings.Any(b => b.Parameter.Value == key) ||
        entry.ColliderBindings.Any(b => b.Parameter == key) ||
        entry.TubeMouthBindings.Any(b => b.Parameter == key) ||
        entry.Presentation.Parameters.Any(p => p.Parameter == key) ||
        entry.Presentation.Sizing?.Parameters.Any(p => p.Parameter == key) == true ||
        entry.Ports.Any(p => p.PositionBinding?.Parameter.Value == key);

    private static bool IsBoundToElectrical(ElectricalPayload? electrical, string key)
    {
        if (electrical?.SupplyGraph is null) return false;
        foreach (var node in electrical.SupplyGraph.Nodes)
        {
            if (node is CuriousContraptions.Abstractions.Electrical.SupplySourceDescriptor source)
            {
                if (source.Store.Capacity.Value == key ||
                    source.Store.PowerLimit.Value == key ||
                    source.Store.InitialCharge.Value == key)
                    return true;
            }
        }
        return false;
    }

    private static bool IsBoundToMechanical(MechanicalPayload? mechanical, string key)
    {
        if (mechanical is null) return false;
        if (Matches(mechanical.MassFrame, key)) return true;
        if (mechanical.GasStores.Any(s => Matches(s.ReferenceVolume, key)) ||
            mechanical.AmbientLoads.Any(load => Matches(load.LinearDrag, key) || load.Acceleration.Parameter?.Value == key))
            return true;
        if (mechanical.Hinges.Any(hinge => IsBoundToHinge(hinge, key))) return true;
        if (mechanical.Sliders.Any(slider => IsBoundToSlider(slider, key))) return true;
        return IsBoundToMechanicalFeatures(mechanical, key);
    }

    private static bool IsBoundToHinge(RevoluteHingeDescriptor hinge, string key) =>
        Matches(hinge.MovingBody.MassFrame, key) ||
        Matches(hinge.Stiffness, key) ||
        Matches(hinge.Damping, key) ||
        Matches(hinge.FrictionTorque, key) ||
        Matches(hinge.InitialAngle, key) ||
        (hinge.Drive is { } drive && (Matches(drive.MaxTorque, key) ||
            (drive.TargetSpeed.Source != MechanicalValueSource.Parameter && Matches(drive.TargetSpeed, key))));

    private static bool IsBoundToSlider(PrismaticSliderDescriptor slider, string key)
    {
        if (Matches(slider.MovingBody.MassFrame, key) ||
            Matches(slider.LowerLimit, key) || Matches(slider.UpperLimit, key) ||
            Matches(slider.Stiffness, key) ||
            Matches(slider.Damping, key) ||
            Matches(slider.Compression, key))
            return true;

        if (slider.Drive is { } drive)
        {
            return Matches(drive.Target, key) ||
                   Matches(drive.MaxSpeed, key) ||
                   Matches(drive.StoppingAcceleration, key) ||
                   Matches(drive.MaxEffort, key) ||
                   Matches(drive.MaxPower, key);
        }
        return false;
    }

    private static bool IsBoundToMechanicalFeatures(MechanicalPayload mechanical, string key)
    {
        if (mechanical.Residence is { } residence &&
            (Matches(residence.SpeedLimit, key) ||
             Matches(residence.Dwell, key) ||
             residence.ParticipationParameter == key ||
             Matches(residence.UpperMargin, key)))
            return true;

        if (mechanical.PlanarGuide is { } guide &&
            (Matches(guide.SupportMargin, key) ||
             Matches(guide.MaximumAcceleration, key)))
            return true;

        if (mechanical.ContactWorks.Any(work => Matches(work.TargetSpeed, key)) || mechanical.EnergyStores.Any(store =>
            Matches(store.Capacity.Value, key) || Matches(store.Capacity.Speed, key) ||
            Matches(store.InitialEnergy.Value, key) || Matches(store.InitialEnergy.Speed, key) || Matches(store.MaximumChargePower, key)))
            return true;

        if (mechanical.CompliantSurface is { } surface &&
            (Matches(surface.Tension, key) ||
             Matches(surface.DampingRatio, key)))
            return true;

        if (mechanical.Transmissions is { } transmissions &&
            transmissions.Any(transmission => Matches(transmission.Ratio, key) ||
                (transmission.CloseSeconds is { } closeSeconds && Matches(closeSeconds, key))))
            return true;

        return mechanical.DrivenSurfaces.Any(driven => Matches(driven.TravelPerRadian, key));
    }

    private static bool IsBoundToSignal(SignalPayload? signal, string key)
    {
        if (signal is null) return false;
        foreach (var node in signal.Nodes)
        {
            switch (node)
            {
                case SignalTimerDescriptor timer when timer.DurationParameter.Value == key:
                case SignalContactDescriptor contact when contact.ThresholdParameter.Value == key:
                case SignalContactLoadDescriptor load when load.MinimumMassParameter.Value == key:
                case SignalOscillatorDescriptor osc when osc.IntervalParameter.Value == key:
                case SignalPulseCountDescriptor count when count.TargetParameter.Value == key:
                    return true;
            }
        }
        return false;
    }
}
