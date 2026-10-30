using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    public const int TensileSpanBytes = 80;
    public const int TensileRouteOffset = 8;
    public const int TensileOrderOffset = 16;
    public const int TensileBodyAOffset = 20;
    public const int TensileBodyBOffset = 24;
    public const int TensileLocalAOffset = 28;
    public const int TensileLocalBOffset = 40;
    public const int TensileMaximumOffset = 52;
    public const int TensileCompleteOffset = 56;
    public const int TensileIntegrityOffset = 60;
    public const int TensileCutSpanOffset = 64;
    public const int TensileImpulseOffset = 72;
    public const int TensileTravelOffset = 76;

    private static void WriteTensileAdmission(Span<byte> data, PhysicsSceneDeclaration scene)
    {
        for (var i = 0; i < scene.TensileSpans.Length; i++)
        {
            var value = scene.TensileSpans[i];
            var row = data.Slice(scene.World.TensileSpanOffset(i), TensileSpanBytes);
            U64(row, 0, value.Id.Value); U64(row, TensileRouteOffset, value.Route.Value);
            U32(row, TensileOrderOffset, value.Order);
            U32(row, TensileBodyAOffset, (uint)scene.Layout.GetBodySlot(value.BodyA));
            U32(row, TensileBodyBOffset, (uint)scene.Layout.GetBodySlot(value.BodyB));
            F32Vector(row, TensileLocalAOffset, value.LocalA.X, value.LocalA.Y, value.LocalA.Z);
            F32Vector(row, TensileLocalBOffset, value.LocalB.X, value.LocalB.Y, value.LocalB.Z);
            F32(row, TensileMaximumOffset, value.MaximumLength);
            U32(row, TensileCompleteOffset, value.Complete ? 1u : 0u);
            U32(row, TensileIntegrityOffset, (uint)RopeIntegrity.Intact);
            F32(row, TensileTravelOffset, 0f);
        }
    }

    public static TensileSpanDeclaration ReadTensileSpan(ReadOnlySpan<byte> data, int index)
    {
        Header(data);
        var layout = Layout(data);
        var row = data.Slice(layout.TensileSpanOffset(index), TensileSpanBytes);
        var a = R32(row, TensileBodyAOffset); var b = R32(row, TensileBodyBOffset);
        var integrity = (RopeIntegrity)R32(row, TensileIntegrityOffset);
        var impulse = RF(row, TensileImpulseOffset);
        var travel = RF(row, TensileTravelOffset);
        var cut = R64(row, TensileCutSpanOffset);
        if (a >= layout.Bodies || b >= layout.Bodies || R32(row, TensileCompleteOffset) > 1 ||
            !Enum.IsDefined(integrity) || (integrity == RopeIntegrity.Intact) != (cut == 0) ||
            !float.IsFinite(impulse) || impulse < 0f || (integrity == RopeIntegrity.Cut && impulse != 0f) ||
            !float.IsFinite(travel))
            throw new ArgumentException("Invalid tensile span.");
        var value = new TensileSpanDeclaration(new(R64(row, 0)), new(R64(row, TensileRouteOffset)),
            R32(row, TensileOrderOffset), new(R64(data, layout.BodyOffset((int)a))),
            new(RF(row, TensileLocalAOffset), RF(row, TensileLocalAOffset + 4), RF(row, TensileLocalAOffset + 8)),
            new(R64(data, layout.BodyOffset((int)b))),
            new(RF(row, TensileLocalBOffset), RF(row, TensileLocalBOffset + 4), RF(row, TensileLocalBOffset + 8)),
            RF(row, TensileMaximumOffset), R32(row, TensileCompleteOffset) == 1);
        value.Validate();
        return value;
    }

    private static void ValidateTensileCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source)
    {
        var layout = Layout(source);
        for (var i = 0; i < layout.TensileSpans; i++)
        {
            _ = ReadTensileSpan(candidate, i);
            var offset = layout.TensileSpanOffset(i);
            if (!candidate.Slice(offset, TensileIntegrityOffset).SequenceEqual(source.Slice(offset, TensileIntegrityOffset)))
                throw new ArgumentException("Tensile declaration changed.");
            if (R32(source, offset + TensileIntegrityOffset) == (uint)RopeIntegrity.Cut &&
                (!candidate.Slice(offset + TensileIntegrityOffset, 12).SequenceEqual(source.Slice(offset + TensileIntegrityOffset, 12))))
                throw new ArgumentException("A committed cut cannot heal.");
        }
    }
}
