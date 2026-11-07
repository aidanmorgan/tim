using System;
using System.Collections.Generic;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

/// <summary>A compiled run-control row's identity within one compiled scene.</summary>
public readonly record struct RunControlId(ulong Value);

/// <summary>Which field of a run-control value carries it (closed set; 0 is undefined).</summary>
public enum RunControlValueKind : byte { Enable = 1, Scalar = 2 }

/// <summary>A typed run-control value: an enable is an <see cref="ElectricalEnable"/> and a scalar an f32, never one encoded as the
/// other. The field its kind does not name is zero.</summary>
public readonly record struct RunControlValue
{
    private RunControlValue(RunControlValueKind kind, ElectricalEnable enable, float scalar)
    {
        Kind = kind; Enable = enable; Scalar = scalar;
    }

    public RunControlValueKind Kind { get; }
    public ElectricalEnable Enable { get; }
    public float Scalar { get; }

    public static RunControlValue Of(ElectricalEnable enable) => new(RunControlValueKind.Enable, enable, 0f);
    public static RunControlValue Of(float scalar) => new(RunControlValueKind.Scalar, ElectricalEnable.Disabled, scalar);

    /// <summary>The value decoded from its three wire fields; false when the kind or enable is undefined, the scalar is not finite, or
    /// the field the kind does not name is not zero.</summary>
    public static bool TryCreate(RunControlValueKind kind, ElectricalEnable enable, float scalar, out RunControlValue value)
    {
        value = new(kind, enable, scalar);
        return value.IsCanonical;
    }

    public bool IsCanonical => Kind switch
    {
        RunControlValueKind.Enable => Enum.IsDefined(Enable) && BitConverter.SingleToUInt32Bits(Scalar) == 0,
        RunControlValueKind.Scalar => float.IsFinite(Scalar) && Enable == ElectricalEnable.Disabled,
        _ => false
    };
}

/// <summary>One compiled run-control row: a declared RunControl parameter of one unlocked instance, the lane slot it drives and the
/// parameter's declared bounds. A SourceEnable row's lane index is its owner's source in compiled source order, and its bounds are zero;
/// a RevoluteDriveTarget row's lane index is its owner's drive in the compiled drive table, and a scalar outside its bounds is refused.</summary>
public readonly record struct RunControlRow(RunControlId Id, GpuBodyId Owner, RunControlLane Lane, int LaneIndex, float Minimum,
    float Maximum, ElectricalSourceId Source = default);

/// <summary>Explicit player input: one declared parameter edited during Run. The next committed step applies the value to the row's
/// lane; the control never edits the authored construction, so Reset and a save keep the authored value.</summary>
public readonly record struct WorkshopRunControl(GpuBodyId Owner, RunControlId Control, RunControlValue Value)
{
    public bool IsCanonical => Owner.Value != 0 && Control.Value != 0 && Value.IsCanonical;
}
