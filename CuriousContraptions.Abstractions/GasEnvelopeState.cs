using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public enum GasInventoryPhase : uint { Charged, Empty }
public enum GasOutletPhase : uint { Closed, Open }
public enum IntegrityPhase : uint { Intact, Broken }
public readonly record struct GasStoreId(ulong Value);
public readonly record struct LocalGasStoreId(uint Value);
public readonly record struct IntegrityObservationId(ulong Value);
public readonly record struct LocalIntegrityObservationId(uint Value);

/// <summary>Finite state; an empty store has canonical zero mass, volume and energy, never a hidden boundary refill.</summary>
public readonly record struct GasEnvelopeState(float Mass, float Volume, float Energy)
{
    public GasInventoryPhase Phase => Mass == 0f ? GasInventoryPhase.Empty : GasInventoryPhase.Charged;

    public void Validate(GasMaterial material)
    {
        material.Validate();
        if (Mass == 0f)
        {
            if (Volume != 0f || Energy != 0f) throw new ArgumentException("An empty gas store must be canonical zero.");
            return;
        }
        material.ValidateState(Mass, Volume, Energy);
    }
}

/// <summary>One accepted outlet allocation. BoundaryWork is positive for compression by ambient;
/// ElasticReleased is positive when the compliant envelope returns energy.</summary>
public readonly record struct GasDischarge(GasEnvelopeState After, float MassRemoved, float EnthalpyOut,
    float BoundaryWork, float ElasticReleased, Vector3 MomentumOut, float KineticEnergyOut);

/// <summary>Prepared sealed state and signed work performed on the unbounded boundary.</summary>
public readonly record struct GasPreparation(GasEnvelopeState State, float BoundaryWorkOut, float ElasticEnergyChange);

/// <summary>Cumulative outgoing thermal energy and its two pressure-work contributions, in joules.</summary>
public readonly record struct GasThermalAccounting(float EnthalpyOut, float BoundaryWork, float ElasticReleased);
