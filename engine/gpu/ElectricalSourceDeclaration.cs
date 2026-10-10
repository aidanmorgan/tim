using System;

namespace CuriousContraptions.Gpu;

public readonly record struct ElectricalSourceId(ulong Value);
public readonly record struct Watts(float Value);
public readonly record struct ElectricalSourceDeclaration(ElectricalSourceId Id, GpuBodyId Owner,
    Joules Capacity, Watts MaximumPower, float InitialFraction, ElectricalEnable Enabled)
{
    public Joules InitialEnergy => new(Capacity.Value * InitialFraction);
    public void Validate()
    {
        new ElectricalSourceSettings(Capacity, MaximumPower, InitialFraction, Enabled).Validate();
        if (Id.Value == 0 || Owner.Value == 0)
            throw new ArgumentException("Invalid finite electrical source identity.");
    }
}

public readonly record struct ElectricalStorageBinding(ElectricalSourceId Source, GpuContactWorkId Storage);

/// <summary>Immutable direct electrical routes. Each storage input has one source; a source may fan out.</summary>
public sealed class ElectricalSupplyPlan
{
    public const int SourceCapacity = 8;
    private readonly ElectricalSourceDeclaration[] _sources;
    private readonly ElectricalStorageBinding[] _bindings;
    public ReadOnlySpan<ElectricalSourceDeclaration> Sources => _sources;
    public ReadOnlySpan<ElectricalStorageBinding> Bindings => _bindings;

    public ElectricalSupplyPlan(ReadOnlySpan<ElectricalSourceDeclaration> sources,
        ReadOnlySpan<ElectricalStorageBinding> bindings, ReadOnlySpan<RigidBodyDeclaration> bodies,
        ReadOnlySpan<ContactWorkDeclaration> storages)
    {
        if (sources.Length > SourceCapacity || bindings.Length > WorkshopConnections.Capacity)
            throw new ArgumentException("Electrical table capacity exceeded.");
        for (var i = 0; i < sources.Length; i++)
        {
            var source = sources[i];
            source.Validate();
            var ownsBody = false;
            foreach (var body in bodies) if (body.Id == source.Owner) ownsBody = true;
            if (!ownsBody || (i > 0 && sources[i - 1].Id.Value >= source.Id.Value))
                throw new ArgumentException("Electrical source requires an owned body and ordered unique identity.");
            for (var j = 0; j < i; j++)
                if (sources[j].Owner == source.Owner)
                    throw new ArgumentException("An electrical source owner was declared twice.");
        }
        for (var i = 0; i < bindings.Length; i++)
        {
            var binding = bindings[i];
            var sourceFound = false;
            var storageFound = false;
            foreach (var source in sources) if (source.Id == binding.Source) sourceFound = true;
            foreach (var storage in storages) if (storage.Id == binding.Storage) storageFound = true;
            if (!sourceFound || !storageFound)
                throw new ArgumentException("Electrical route names a foreign source or storage.");
            for (var j = 0; j < i; j++)
                if (bindings[j].Storage == binding.Storage)
                    throw new ArgumentException("A storage input already has an electrical supplier.");
        }
        _sources = sources.ToArray();
        _bindings = bindings.ToArray();
    }
}
