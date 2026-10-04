using System;
using System.Runtime.CompilerServices;

namespace CuriousContraptions.Gpu;

[InlineArray(PhysicsSceneDeclaration.ContactWorkCapacity)]
internal struct ContactWorkReadStorage { private ContactWorkRead _first; }

public readonly struct PhysicsContactWorkRead
{
    private readonly ContactWorkReadStorage _values;
    public byte Count { get; }
    public PhysicsContactWorkRead(ReadOnlySpan<ContactWorkRead> values)
    {
        if (values.Length > PhysicsSceneDeclaration.ContactWorkCapacity) throw new ArgumentException("Contact-work read capacity exceeded.");
        Count = checked((byte)values.Length); _values = default;
        for (var i = 0; i < values.Length; i++)
        {
            values[i].Validate();
            if (i != 0 && values[i - 1].Id.Value >= values[i].Id.Value)
                throw new ArgumentException("Contact-work identities must be unique and ordered.");
            _values[i] = values[i];
        }
    }
    public ContactWorkRead this[int index] => index >= 0 && index < Count ? _values[index] :
        throw new ArgumentOutOfRangeException(nameof(index));
}
