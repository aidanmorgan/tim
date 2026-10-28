using CuriousContraptions.Abstractions;
using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public enum PhysicsMotionKind : uint { FreePolynomial = 1, SupportedQuadratic = 2, ForceDrivenQuadratic = 3 }

/// <summary>Immutable GPU-authored render trajectory for exactly one complete physical commit. Every lane is a named offset and every
/// game value an IEEE-754 f32 (Story 9A.44); the physical frequency is a U32.</summary>
public sealed class PhysicsMotionRead
{
    /// <summary>One piece per substep: room for the largest admitted substep count per tick (structural, not a population).</summary>
    public const int PiecesPerBody = 8;
    public const int PieceBytes = 160;
    public const int HeaderBytes = 16;
    // Header: piece count, substeps per commit, and the commit's first and last physical-step ordinals (U32 each).
    public const int CountOffset = 0;
    public const int SubstepsOffset = 4;
    public const int FirstOrdinalOffset = 8;
    public const int LastOrdinalOffset = 12;
    // Piece: kind and the start, end and anchor ordinals (U32), their f32 phases, the U32 physical frequency, the owner (U64),
    // the COM cell (I32 x3) and f32 remainder, the f32 drag rate, rotation, velocities and accelerations, and a flagged f32 vector.
    // The drag rate is the body's declared linear drag (1/s), so a free piece samples the same exponential decay the solver applies.
    public const int KindOffset = 0;
    public const int StartOrdinalOffset = 4;
    public const int EndOrdinalOffset = 8;
    public const int AnchorOrdinalOffset = 12;
    public const int StartPhaseOffset = 16;
    public const int EndPhaseOffset = 20;
    public const int AnchorPhaseOffset = 24;
    public const int FrequencyOffset = 28;
    public const int OwnerOffset = 32;
    public const int CellOffset = 40;
    public const int CentreRemainderOffset = 52;
    public const int DragRateOffset = 64;
    public const int RotationOffset = 68;
    public const int VelocityOffset = 84;
    public const int AngularVelocityOffset = 96;
    public const int GravityOffset = 108;
    public const int SupportedAccelerationOffset = 120;
    public const int AngularAccelerationOffset = 132;
    /// <summary>A bounded f32 vector lane, present only while its U32 flag is one; the solver writes it zero.</summary>
    public const int FlaggedVectorOffset = 144;
    public const int VectorFlagOffset = 156;
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
            var kind = (PhysicsMotionKind)U32(piece, KindOffset);
            var start = Time(U32(piece, StartOrdinalOffset), F(piece, StartPhaseOffset));
            var end = Time(U32(piece, EndOrdinalOffset), F(piece, EndPhaseOffset));
            var anchor = Time(U32(piece, AnchorOrdinalOffset), F(piece, AnchorPhaseOffset));
            var flag = U32(piece, VectorFlagOffset);
            if (!Enum.IsDefined(kind) || U64(piece, OwnerOffset) != owner.Id.Value || start != previous ||
                end.Ordinal != start.Ordinal + 1 || end.Phase != start.Phase || end > new PhysicalTime(last) || anchor > start ||
                end > new PhysicalTime(anchor.Ordinal + (kind != PhysicsMotionKind.FreePolynomial ? 1u : PhysicsGpuAbi.PrimarySegmentSteps), anchor.Phase) ||
                U32(piece, FrequencyOffset) != WorkshopCadenceSettings.PhysicalFrequency ||
                flag > 1 || (flag == 0 && !Zero(piece.Slice(FlaggedVectorOffset, PhysicsGpuAbi.VectorBytes))))
                throw new ArgumentException("Invalid motion piece identity, interval or padding.");
            // COM is an internal point: body origin remains within64m, while a rotated
            // declared16m-per-axis offset fits an additional32m per coordinate.
            var centre=Cell(piece);
            if (Math.Abs((long)centre.X)>1536 || Math.Abs((long)centre.Y)>1536 || Math.Abs((long)centre.Z)>1536)
                throw new ArgumentException("Motion COM exceeds its declared origin-plus-offset bound.");
            for (var axis=0; axis<3; axis++)
                if (!float.IsFinite(F(piece,CentreRemainderOffset+axis*4)) || F(piece,CentreRemainderOffset+axis*4)<-.5f || F(piece,CentreRemainderOffset+axis*4)>=.5f)
                    throw new ArgumentException("Motion COM remainder is not canonical.");
            Norm(piece,VelocityOffset,LinearVelocity.MaximumSpeed); Norm(piece,AngularVelocityOffset,AngularVelocity.MaximumSpeed);
            Norm(piece,GravityOffset,16); Norm(piece,SupportedAccelerationOffset,64); Norm(piece,AngularAccelerationOffset,1024);
            Rotation(piece).ValidateCommitted();
            PhysicsDeclarationBounds.Range(F(piece,DragRateOffset),0f,.125f);
            if (kind == PhysicsMotionKind.FreePolynomial &&
                (!Zero(piece.Slice(SupportedAccelerationOffset,PhysicsGpuAbi.VectorBytes)) ||
                 !Zero(piece.Slice(AngularAccelerationOffset,PhysicsGpuAbi.VectorBytes))))
                throw new ArgumentException("Free motion carries constrained acceleration.");
            Norm(piece,FlaggedVectorOffset,64);
            previous = end;
            if (i % steps == steps - 1 && previous != new PhysicalTime(last))
                throw new ArgumentException("Motion contains a missing body tail.");
        }
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
            var begin = Time(U32(p,StartOrdinalOffset),F(p,StartPhaseOffset)); var end = Time(U32(p,EndOrdinalOffset),F(p,EndPhaseOffset));
            // Right-continuous impact selection. The last exact endpoint comes from its committed read.
            if (physicalTime < begin || physicalTime >= end) continue;
            var elapsed = physicalTime.SecondsSince(new(U32(p,AnchorOrdinalOffset), F(p,AnchorPhaseOffset)));
            var supported = (PhysicsMotionKind)U32(p,KindOffset) != PhysicsMotionKind.FreePolynomial;
            var k = F(p,DragRateOffset); var x = supported ? 0 : k * elapsed;
            var polynomial = .5f + x * ((-1f/6) + x * ((1f/24) +
                x * ((-1f/120) + x * (1f/720))));
            var cell = Cell(p);
            var px = Position(p,0,cell.X,elapsed,k,polynomial,supported);
            var py = Position(p,1,cell.Y,elapsed,k,polynomial,supported);
            var pz = Position(p,2,cell.Z,elapsed,k,polynomial,supported);
            var q = Rotation(p);
            var wx = F(p,AngularVelocityOffset) + F(p,AngularAccelerationOffset) * elapsed * .5f;
            var wy = F(p,AngularVelocityOffset+4) + F(p,AngularAccelerationOffset+4) * elapsed * .5f;
            var wz = F(p,AngularVelocityOffset+8) + F(p,AngularAccelerationOffset+8) * elapsed * .5f;
            var speed = MathF.Sqrt(wx*wx+wy*wy+wz*wz);
            var rotation = q;
            if (speed != 0 && elapsed != 0)
            {
                var halfAngle = speed * elapsed * .5f;
                var scale = MathF.Sin(halfAngle)/speed;
                var ax=wx*scale; var ay=wy*scale; var az=wz*scale; var aw=MathF.Cos(halfAngle);
                var qx=q.X;var qy=q.Y;var qz=q.Z;var qw=q.W;
                var rx=aw*qx+qw*ax+ay*qz-az*qy; var ry=aw*qy+qw*ay+az*qx-ax*qz;
                var rz=aw*qz+qw*az+ax*qy-ay*qx; var rw=aw*qw-ax*qx-ay*qy-az*qz;
                var inverse=1/MathF.Sqrt(rx*rx+ry*ry+rz*rz+rw*rw);
                rotation=CanonicalRotation.Normalize((float)(rx*inverse),(float)(ry*inverse),(float)(rz*inverse),(float)(rw*inverse));
            }
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
    private static (int Cell, float Local) Origin((int Cell, float Local) centre, float offset)
    {
        var local = centre.Local - offset * 16;
        var carry = checked((int)MathF.Floor(local + .5f)); var remainder = (float)(local - carry);
        if (remainder >= .5f) { carry++; remainder -= 1f; }
        if (remainder < -.5f) { carry--; remainder += 1f; }
        return (checked(centre.Cell + carry), remainder);
    }
    private static (int Cell, float Local) Position(ReadOnlySpan<byte> p,int axis,int cell,float elapsed,float k,float polynomial,bool supported)
    {
        var velocity=F(p,VelocityOffset+axis*4);
        var acceleration=supported?F(p,SupportedAccelerationOffset+axis*4):F(p,GravityOffset+axis*4)-k*velocity;
        var local=F(p,CentreRemainderOffset+axis*4)+(velocity*elapsed+acceleration*elapsed*elapsed*polynomial)*16;
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
    private static PhysicalTime Time(uint ordinal,float phase)
    {
        if(!PhysicalPhase.IsAdmitted(ordinal,phase))
            throw new ArgumentException("Invalid motion time.");
        return new(ordinal, phase);
    }
    private static CellOrigin Cell(ReadOnlySpan<byte> p)=>new(BinaryPrimitives.ReadInt32LittleEndian(p[CellOffset..]),BinaryPrimitives.ReadInt32LittleEndian(p[(CellOffset+4)..]),BinaryPrimitives.ReadInt32LittleEndian(p[(CellOffset+8)..]));
    private static CanonicalRotation Rotation(ReadOnlySpan<byte> p)=>new(F(p,RotationOffset),F(p,RotationOffset+PhysicsGpuAbi.RotationYOffset),
        F(p,RotationOffset+PhysicsGpuAbi.RotationZOffset),F(p,RotationOffset+PhysicsGpuAbi.RotationWOffset));
    private static bool Zero(ReadOnlySpan<byte> p) => p.IndexOfAnyExcept((byte)0) < 0;
    private static uint U32(ReadOnlySpan<byte> p,int offset)=>BinaryPrimitives.ReadUInt32LittleEndian(p[offset..]);
    private static ulong U64(ReadOnlySpan<byte> p,int offset)=>BinaryPrimitives.ReadUInt64LittleEndian(p[offset..]);
    private static float F(ReadOnlySpan<byte> p,int offset)=>BinaryPrimitives.ReadSingleLittleEndian(p[offset..]);
}
