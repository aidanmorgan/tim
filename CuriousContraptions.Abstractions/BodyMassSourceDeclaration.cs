using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Text.Json.Serialization;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Abstractions.Mechanical;

public enum BodyMassSourceKind : byte { Declared = 1, CenteredInventory = 2 }

public readonly record struct ReferenceGasMass(uint BodyIndex, float Mass);

/// <summary>Exact local reference inventories for one declared body's mass resolution.</summary>
public readonly record struct MassSourceContext(uint BodyIndex,
    IReadOnlyDictionary<LocalGasStoreId, ReferenceGasMass> Stores);

public readonly record struct ResolvedBodyMass(float InitialMass, float DryMass, Vector3 PrincipalMoments);

/// <summary>One owner for initial mass and fixed carrier inertia; inventory adds translation only.</summary>
public readonly record struct BodyMassSourceDeclaration(BodyMassSourceKind Kind, MassScalar InitialMass,
    PrincipalMomentDeclaration DeclaredMoments, [property: JsonRequired] IReadOnlyList<LocalGasStoreId> Stores,
    Vector3 DryMomentPerMass)
{
    public static BodyMassSourceDeclaration Declared(MassScalar mass, PrincipalMomentDeclaration moments) =>
        new(BodyMassSourceKind.Declared, mass, moments, Array.Empty<LocalGasStoreId>(), Vector3.Zero);

    public ResolvedBodyMass Resolve(IReadOnlyDictionary<ParameterId, float> parameters, MassSourceContext context)
    {
        if (Stores is null || context.Stores is null)
            throw new ArgumentException("Mass source requires explicit inventory collections.");
        var total = InitialMass.Evaluate(parameters);
        if (Kind == BodyMassSourceKind.Declared)
        {
            if (Stores.Count != 0 || DryMomentPerMass != Vector3.Zero)
                throw new ArgumentException("Declared mass contains inactive inventory operands.");
            return new(total, total, DeclaredMoments.Evaluate(parameters));
        }
        ValidateInventoryOperands();
        var seen = new HashSet<LocalGasStoreId>();
        var gasMass = 0f;
        foreach (var id in Stores.OrderBy(id => id.Value))
        {
            if (id == default || !seen.Add(id) || !context.Stores.TryGetValue(id, out var inventory) ||
                inventory.BodyIndex != context.BodyIndex || !float.IsFinite(inventory.Mass) || inventory.Mass <= 0)
                throw new ArgumentException("Mass source requires unique exact inventories owned by its body.");
            gasMass += inventory.Mass;
        }
        var dry = total - gasMass;
        if (!float.IsFinite(gasMass) || !float.IsFinite(dry) || dry < 1f / 1024f || total > 1024f)
            throw new ArgumentException("Inventory mass leaves the admitted body mass domain.");
        var moments = DryMomentPerMass * dry;
        foreach (var value in new[] { moments.X, moments.Y, moments.Z })
            if (!float.IsFinite(value) || value < 1f / 4294967296f || value >= 524288f)
                throw new ArgumentException("Dry carrier moments leave the physical domain.");
        return new(total, dry, moments);
    }

    public void ValidateInventoryOperands()
    {
        if (Kind != BodyMassSourceKind.CenteredInventory || Stores is null || Stores.Count == 0 ||
            DeclaredMoments != default)
            throw new ArgumentException("Invalid centered-inventory mass operands.");
        var c = DryMomentPerMass;
        if (!float.IsFinite(c.X) || !float.IsFinite(c.Y) || !float.IsFinite(c.Z) ||
            c.X <= 0 || c.Y <= 0 || c.Z <= 0 ||
            c.X > c.Y + c.Z || c.Y > c.X + c.Z || c.Z > c.X + c.Y)
            throw new ArgumentException("Nonphysical dry carrier moment coefficients.");
    }
}
