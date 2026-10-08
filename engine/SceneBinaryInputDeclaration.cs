namespace CuriousContraptions;

public sealed class BinaryInputSlot;
public readonly record struct SceneBinaryInputKey(MachinePart Owner,BinaryInputSlot Slot);
public readonly record struct SceneBinaryInputDeclaration(SceneBinaryInputKey Key,Bridge.BinaryInputState Initial);
