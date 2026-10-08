using System;

namespace CuriousContraptions;

public sealed class ScalarInputSlot;
public readonly record struct SceneScalarInputKey(MachinePart Owner,ScalarInputSlot Slot);
public readonly record struct SceneScalarInputDeclaration(SceneScalarInputKey Key,double Initial,double Minimum,double Maximum)
{
    public void Validate()
    {
        if(Key.Owner is null||Key.Slot is null||!double.IsFinite(Initial)||!double.IsFinite(Minimum)||
            !double.IsFinite(Maximum)||Minimum>Maximum||Initial<Minimum||Initial>Maximum)
            throw new ArgumentException("Scalar input needs an owned slot and finite ordered bounds containing its initial value.");
    }
}
