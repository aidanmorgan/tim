using CuriousContraptions.Abstractions;
using System;
using System.Buffers.Binary;
using System.Numerics;

namespace CuriousContraptions.Gpu;

/// <summary>Immutable GPU-authored render trajectory for exactly one complete physical commit. Every lane is a named offset and every
/// game value an IEEE-754 f32 (Story 9A.44); the physical frequency is a U32.</summary>
public sealed class PhysicsMotionRead : IPhysicsMotionRead
{
    /// <summary>One piece per substep: room for the largest admitted substep count per tick (structural, not a population).</summary>
    public const int PiecesPerBody = 8;
    public const int PieceBytes = 80;
    public const int HeaderBytes = 16;
    // Header: piece count, substeps per commit, and the commit's first and last physical-step ordinals (U32 each).
    public const int CountOffset = 0;
    public const int SubstepsOffset = 4;
    public const int FirstOrdinalOffset = 8;
    public const int LastOrdinalOffset = 12;
    // One whole-substep integrated interval, without selectable laws or unused phase/acceleration lanes.
    public const int OwnerOffset = 0;
    public const int StartOrdinalOffset = 8;
    public const int EndOrdinalOffset = 12;
    public const int CellOffset = 16;
    public const int CentreRemainderOffset = 28;
    public const int RotationOffset = 40;
    public const int VelocityOffset = 56;
    public const int AngularVelocityOffset = 68;
    /// <summary>The payload for a world with this many dynamic bodies (Story 9R.5b: no fixed body capacity).</summary>
    public static int ByteLength(int dynamicBodies)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dynamicBodies);
        var bytes = HeaderBytes + (long)dynamicBodies * PiecesPerBody * PieceBytes;
        return bytes <= Array.MaxLength ? (int)bytes
            : throw new WorkshopStorageException("Motion storage is not representable.");
    }
    private readonly byte[] _bytes;
    private readonly SimulationTick _tick;
    private readonly GpuBodyId[] _bodyIds;
    private readonly MetreVector[] _localCentresOfMass;
    public ReadOnlySpan<byte> Bytes => _bytes;
    public int Count => checked((int)U32(_bytes, CountOffset));
    public uint Substeps => U32(_bytes, SubstepsOffset);
    public uint FirstOrdinal => U32(_bytes, FirstOrdinalOffset);
    public uint LastOrdinal => U32(_bytes, LastOrdinalOffset);
    private PhysicsMotionRead(ReadOnlySpan<byte> bytes, PhysicsBodyReadSet bodies, SimulationTick tick)
    {
        _bytes = bytes.ToArray();
        _tick = tick;
        _bodyIds = new GpuBodyId[bodies.Count];
        _localCentresOfMass = new MetreVector[bodies.Count];
        for (var i = 0; i < bodies.Count; i++)
        { _bodyIds[i] = bodies[i].Body.Id; _localCentresOfMass[i] = bodies[i].LocalCentreOfMass; }
    }

    public static PhysicsMotionRead Decode(ReadOnlySpan<byte> bytes, PhysicsBodyReadSet bodies, SimulationTick tick)
    {
        var motion = new PhysicsMotionRead(bytes, bodies, tick);
        Validate(motion._bytes, bodies, tick);
        return motion;
    }

    /// <summary>Rebinds an already validated immutable payload without scanning its bytes again.</summary>
    public void ValidateBinding(PhysicsBodyReadSet bodies, SimulationTick tick)
    {
        // Admission's canonical zero payload has no preceding body trajectory.
        if (tick != _tick || bodies.Count != _bodyIds.Length)
            throw new ArgumentException("Motion does not match its owning body set and commit.");
        for (var i = 0; i < bodies.Count; i++)
            if (bodies[i].Body.Id != _bodyIds[i] || !F32Bits.Equal(bodies[i].LocalCentreOfMass, _localCentresOfMass[i]))
                throw new ArgumentException("Motion body identity or centre of mass changed.");
    }

    public static void Validate(ReadOnlySpan<byte> bytes, PhysicsBodyReadSet bodies, SimulationTick tick)
    {
        if (bytes.Length != ByteLength(bodies.Count)) throw new ArgumentException("Invalid motion payload width.");
        var count = U32(bytes, CountOffset);
        if (count > (uint)(bodies.Count * PiecesPerBody) || !Zero(bytes[(HeaderBytes + checked((int)count) * PieceBytes)..]))
            throw new ArgumentException("Invalid motion capacity or unused slots.");
        if (tick.Value == 0)
        {
            if (!Zero(bytes)) throw new ArgumentException("Admission has no preceding motion interval.");
            return;
        }
        var steps = U32(bytes, SubstepsOffset);
        var first = U32(bytes, FirstOrdinalOffset); var last = U32(bytes, LastOrdinalOffset);
        // A tick beyond the u32 ordinal range cannot be covered: an input rejection, never an arithmetic fault (decoded wire input).
        if (steps is not (2 or 4 or 8) || tick.Value > uint.MaxValue || last != tick.Value * steps ||
            (ulong)first + steps != last || count != (ulong)steps * (ulong)bodies.Count)
            throw new ArgumentException("Motion does not cover its owning commit.");
        var previous = new PhysicalTime(first);
        for (var i = 0; i < count; i++)
        {
            var owner = bodies[checked((int)(i / steps))].Body;
            if (i % steps == 0) previous = new(first);
            var piece = bytes.Slice(HeaderBytes + checked((int)i) * PieceBytes, PieceBytes);
            var end = ValidatePiece(piece, owner.Id, previous, last);
            previous = end;
            if (i % steps == steps - 1 && previous != new PhysicalTime(last))
                throw new ArgumentException("Motion contains a missing body tail.");
        }
    }

    private static PhysicalTime ValidatePiece(ReadOnlySpan<byte> piece, GpuBodyId owner, PhysicalTime previous, uint last)
    {
        var start = new PhysicalTime(U32(piece, StartOrdinalOffset));
        var end = new PhysicalTime(U32(piece, EndOrdinalOffset));
        if (U64(piece, OwnerOffset) != owner.Value || start != previous ||
            end.Ordinal != start.Ordinal + 1 || end > new PhysicalTime(last))
            throw new ArgumentException("Invalid integrated motion identity or interval.");
        ValidateCentreOfMass(piece);
        Norm(piece, VelocityOffset, LinearVelocity.MaximumSpeed);
        Norm(piece, AngularVelocityOffset, AngularVelocity.MaximumSpeed);
        Rotation(piece).ValidateCommitted();
        return end;
    }

    private static void ValidateCentreOfMass(ReadOnlySpan<byte> piece)
    {
        // COM is an internal point: body origin remains within64m, while a rotated
        // declared16m-per-axis offset fits an additional32m per coordinate.
        var centre=Cell(piece);
        if (Math.Abs((long)centre.X)>1536 || Math.Abs((long)centre.Y)>1536 || Math.Abs((long)centre.Z)>1536)
            throw new ArgumentException("Motion COM exceeds its declared origin-plus-offset bound.");
        for (var axis=0; axis<3; axis++)
            if (!float.IsFinite(F(piece,CentreRemainderOffset+axis*4)) || F(piece,CentreRemainderOffset+axis*4)<-.5f || F(piece,CentreRemainderOffset+axis*4)>=.5f)
                throw new ArgumentException("Motion COM remainder is not canonical.");
    }

    /// <summary>Render-only sampling of the committed coefficient record; never a physical state update.</summary>
    public bool TrySample(GpuBodyId body, PhysicalTime physicalTime, out PresentedBody pose)
    {
        var bodyIndex = Array.IndexOf(_bodyIds, body);
        if (bodyIndex < 0) { pose = default; return false; }
        for (var i = 0; i < Count; i++)
        {
            var p = _bytes.AsSpan(HeaderBytes + i * PieceBytes, PieceBytes);
            if (U64(p,OwnerOffset) != body.Value) continue;
            var begin = new PhysicalTime(U32(p, StartOrdinalOffset));
            var end = new PhysicalTime(U32(p, EndOrdinalOffset));
            // The next interval owns a shared boundary; the final endpoint belongs to the committed pose.
            if (physicalTime < begin || physicalTime >= end) continue;
            var elapsed = physicalTime.SecondsSince(begin);
            var cell = Cell(p);
            var px = Position(p, 0, cell.X, elapsed);
            var py = Position(p, 1, cell.Y, elapsed);
            var pz = Position(p, 2, cell.Z, elapsed);
            var rotation = IntegratedRotation(p, elapsed);
            var centre = _localCentresOfMass[bodyIndex];
            var qx2 = rotation.X; var qy2 = rotation.Y; var qz2 = rotation.Z; var qw2 = rotation.W;
            var cx = centre.X; var cy = centre.Y; var cz = centre.Z;
            var factor = 2 / (qx2*qx2 + qy2*qy2 + qz2*qz2 + qw2*qw2);
            var tx = factor * (qy2 * cz - qz2 * cy); var ty = factor * (qz2 * cx - qx2 * cz); var tz = factor * (qx2 * cy - qy2 * cx);
            px = Origin(px, cx + qw2 * tx + qy2 * tz - qz2 * ty);
            py = Origin(py, cy + qw2 * ty + qz2 * tx - qx2 * tz);
            pz = Origin(pz, cz + qw2 * tz + qx2 * ty - qy2 * tx);
            pose = new(body,new(px.Cell,py.Cell,pz.Cell),new(px.Local,py.Local,pz.Local),rotation);
            return true;
        }
        pose=default;return false;
    }
    private static CanonicalRotation IntegratedRotation(ReadOnlySpan<byte> piece, float elapsed)
    {
        var start = Rotation(piece);
        var spin = new Vector3(F(piece, AngularVelocityOffset), F(piece, AngularVelocityOffset + 4),
            F(piece, AngularVelocityOffset + 8));
        var speed = spin.Length();
        var halfAngle = speed * elapsed * .5f;
        var scale = speed == 0 ? elapsed * .5f : MathF.Sin(halfAngle) / speed;
        var increment = new Quaternion(spin * scale, MathF.Cos(halfAngle));
        var rotated = Quaternion.Normalize(Quaternion.Multiply(increment,
            new Quaternion(start.X, start.Y, start.Z, start.W)));
        return CanonicalRotation.Normalize(rotated.X, rotated.Y, rotated.Z, rotated.W);
    }

    private static (int Cell, float Local) Origin((int Cell, float Local) centre, float offset)
    {
        var local = centre.Local - offset * 16;
        var carry = checked((int)MathF.Floor(local + .5f)); var remainder = (float)(local - carry);
        if (remainder >= .5f) { carry++; remainder -= 1f; }
        if (remainder < -.5f) { carry--; remainder += 1f; }
        return (checked(centre.Cell + carry), remainder);
    }
    private static (int Cell, float Local) Position(ReadOnlySpan<byte> p,int axis,int cell,float elapsed)
    {
        var velocity=F(p,VelocityOffset+axis*4);
        var local=F(p,CentreRemainderOffset+axis*4)+velocity*elapsed*16;
        var carry=checked((int)MathF.Floor(local+.5f));var remainder=(float)(local-carry);
        if(remainder >= .5f){carry++;remainder-=1f;}
        if(remainder < -.5f){carry--;remainder+=1f;}
        return(checked(cell+carry),remainder);
    }
    private static void Norm(ReadOnlySpan<byte> p,int offset,float maximum)
    {
        if (Zero(p.Slice(offset,PhysicsGpuAbi.VectorBytes))) return;
        var x=F(p,offset);var y=F(p,offset+4);var z=F(p,offset+8);
        if(!float.IsFinite(x)||!float.IsFinite(y)||!float.IsFinite(z)||x*x+y*y+z*z>maximum*maximum)
            throw new ArgumentException("Motion vector exceeds its declared domain.");
    }
    private static CellOrigin Cell(ReadOnlySpan<byte> p)=>new(BinaryPrimitives.ReadInt32LittleEndian(p[CellOffset..]),BinaryPrimitives.ReadInt32LittleEndian(p[(CellOffset+4)..]),BinaryPrimitives.ReadInt32LittleEndian(p[(CellOffset+8)..]));
    private static CanonicalRotation Rotation(ReadOnlySpan<byte> p)=>new(F(p,RotationOffset),F(p,RotationOffset+PhysicsGpuAbi.RotationYOffset),
        F(p,RotationOffset+PhysicsGpuAbi.RotationZOffset),F(p,RotationOffset+PhysicsGpuAbi.RotationWOffset));
    private static bool Zero(ReadOnlySpan<byte> p) => p.IndexOfAnyExcept((byte)0) < 0;
    private static uint U32(ReadOnlySpan<byte> p,int offset)=>BinaryPrimitives.ReadUInt32LittleEndian(p[offset..]);
    private static ulong U64(ReadOnlySpan<byte> p,int offset)=>BinaryPrimitives.ReadUInt64LittleEndian(p[offset..]);
    private static float F(ReadOnlySpan<byte> p,int offset)=>BinaryPrimitives.ReadSingleLittleEndian(p[offset..]);
}
