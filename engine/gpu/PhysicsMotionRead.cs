using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public enum PhysicsMotionKind : uint { FreePolynomial = 1, SupportedQuadratic = 2, ForceDrivenQuadratic = 3 }

/// <summary>Immutable GPU-authored render trajectory for exactly one complete physical commit.</summary>
public sealed class PhysicsMotionRead
{
    public const int Capacity = PhysicsBodyReadSet.Capacity * 8;
    public const int PieceBytes = 128;
    public const int HeaderBytes = 16;
    // Piece: kind 0, start/end/anchor ordinals 4..16, phases and rate (Half) 16..24, body id 24..32, COM cell 32..44, COM remainder
    // (Half) 48..54, drag rate (Half) 54..56, rotation (Half) 56..64, linear velocity m/s (f32) 64..76, angular velocity rad/s (f32)
    // 76..88, gravity 88..94, supported acceleration 96..102, angular acceleration 104..110, bounded lane 112..118 with flag 120 (Half).
    public const int VelocityOffset = 64;
    public const int AngularVelocityOffset = 76;
    public const int GravityOffset = 88;
    public const int SupportedAccelerationOffset = 96;
    public const int AngularAccelerationOffset = 104;
    public const int ByteLength = HeaderBytes + Capacity * PieceBytes;
    private readonly byte[] _bytes;
    private readonly SimulationTick _tick;
    private readonly GpuBodyId[] _bodyIds;
    private readonly MetreVector[] _localCentresOfMass;
    public ReadOnlySpan<byte> Bytes => _bytes;
    public int Count => checked((int)U32(_bytes, 0));
    public uint Substeps => U32(_bytes, 4);
    public uint FirstOrdinal => U32(_bytes, 8);
    public uint LastOrdinal => U32(_bytes, 12);
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
            if (bodies[i].Body.Id != _bodyIds[i] || !HalfBits.Equal(bodies[i].LocalCentreOfMass, _localCentresOfMass[i]))
                throw new ArgumentException("Motion body identity or centre of mass changed.");
    }

    public static void Validate(ReadOnlySpan<byte> bytes, PhysicsBodyReadSet bodies, SimulationTick tick)
    {
        if (bytes.Length != ByteLength) throw new ArgumentException("Invalid motion payload width.");
        var count = U32(bytes, 0);
        if (count > Capacity || !Zero(bytes[(HeaderBytes + checked((int)count) * PieceBytes)..]))
            throw new ArgumentException("Invalid motion capacity or unused slots.");
        if (tick.Value == 0)
        {
            if (!Zero(bytes)) throw new ArgumentException("Admission has no preceding motion interval.");
            return;
        }
        var steps = U32(bytes, 4);
        if (steps is not (2 or 4 or 8) || U32(bytes, 12) != checked(tick.Value * steps) ||
            U32(bytes, 8) + steps != U32(bytes, 12) || count != steps * bodies.Count)
            throw new ArgumentException("Motion does not cover its owning commit.");
        double previous = U32(bytes, 8) * 4096.0;
        for (var i = 0; i < count; i++)
        {
            var owner = bodies[checked((int)(i / steps))].Body;
            if (i % steps == 0) previous = U32(bytes, 8) * 4096.0;
            var piece = bytes.Slice(HeaderBytes + checked((int)i) * PieceBytes, PieceBytes);
            var kind = (PhysicsMotionKind)U32(piece, 0);
            var start = Time(U32(piece, 4), H(piece, 16));
            var end = Time(U32(piece, 8), H(piece, 18));
            var anchor = Time(U32(piece, 12), H(piece, 20));
            if (!Enum.IsDefined(kind) || U64(piece, 24) != owner.Id.Value || start != previous ||
                end - start != 4096 || end > U32(bytes, 12) * 4096.0 || anchor > start ||
                end - anchor > (kind != PhysicsMotionKind.FreePolynomial ? 4096 : PhysicsGpuAbi.PrimarySegmentSteps * 4096) ||
                !HalfBits.Equal(H(piece, 22), (Half)WorkshopCadenceSettings.PhysicalFrequency) ||
                !Zero(piece[44..48]) || !Zero(piece[94..96]) || !Zero(piece[102..104]) || !Zero(piece[110..112]) ||
                U32(piece, 120) > 1 || !Zero(piece[118..120]) || !Zero(piece[124..128]) ||
                (U32(piece, 120) == 0 && !Zero(piece[112..118])))
                throw new ArgumentException("Invalid motion piece identity, interval or padding.");
            // COM is an internal point: body origin remains within64m, while a rotated
            // declared16m-per-axis offset fits an additional32m per coordinate.
            var centre=Cell(piece);
            if (Math.Abs((long)centre.X)>1536 || Math.Abs((long)centre.Y)>1536 || Math.Abs((long)centre.Z)>1536)
                throw new ArgumentException("Motion COM exceeds its declared origin-plus-offset bound.");
            for (var axis=0; axis<3; axis++)
                if (!Half.IsFinite(H(piece,48+axis*2)) || H(piece,48+axis*2)<(Half)(-.5) || H(piece,48+axis*2)>=(Half).5)
                    throw new ArgumentException("Motion COM remainder is not canonical.");
            F32Norm(piece,VelocityOffset,LinearVelocity.MaximumSpeed); F32Norm(piece,AngularVelocityOffset,AngularVelocity.MaximumSpeed);
            Norm(piece,GravityOffset,16); Norm(piece,SupportedAccelerationOffset,64); Norm(piece,AngularAccelerationOffset,1024);
            Rotation(piece).ValidateCommitted();
            PhysicsDeclarationBounds.Range(H(piece,54),(Half)0,(Half).125);
            if (kind == PhysicsMotionKind.FreePolynomial &&
                (!Zero(piece[SupportedAccelerationOffset..(SupportedAccelerationOffset+6)]) ||
                 !Zero(piece[AngularAccelerationOffset..(AngularAccelerationOffset+6)])))
                throw new ArgumentException("Free motion carries constrained acceleration.");
            Norm(piece,112,64);
            previous = end;
            if (i % steps == steps - 1 && previous != U32(bytes, 12) * 4096.0)
                throw new ArgumentException("Motion contains a missing body tail.");
        }
    }

    /// <summary>Render-only sampling of the committed coefficient record; never a physical state update.</summary>
    public bool TrySample(GpuBodyId body, double physicalOrdinal, out PresentedBody pose)
    {
        if (!double.IsFinite(physicalOrdinal)) throw new ArgumentException("Invalid render sample time.");
        var bodyIndex = Array.IndexOf(_bodyIds, body);
        if (bodyIndex < 0) { pose = default; return false; }
        var units = physicalOrdinal * 4096;
        for (var i = 0; i < Count; i++)
        {
            var p = _bytes.AsSpan(HeaderBytes + i * PieceBytes, PieceBytes);
            if (U64(p,24) != body.Value) continue;
            var begin = Time(U32(p,4),H(p,16)); var end = Time(U32(p,8),H(p,18));
            // Right-continuous impact selection. The last exact endpoint comes from its committed read.
            if (units < begin || units >= end) continue;
            var elapsed = (physicalOrdinal - U32(p,12) - (double)H(p,20) / 4096) / (double)H(p,22);
            var supported = (PhysicsMotionKind)U32(p,0) != PhysicsMotionKind.FreePolynomial;
            var k = (double)H(p,54); var x = supported ? 0 : k * elapsed;
            var polynomial = .5 + x * ((double)(Half)(-1.0/6) + x * ((double)(Half)(1.0/24) +
                x * ((double)(Half)(-1.0/120) + x * (double)(Half)(1.0/720))));
            var cell = Cell(p);
            var px = Position(p,0,cell.X,elapsed,k,polynomial,supported);
            var py = Position(p,1,cell.Y,elapsed,k,polynomial,supported);
            var pz = Position(p,2,cell.Z,elapsed,k,polynomial,supported);
            var q = Rotation(p);
            var wx = (double)F(p,AngularVelocityOffset) + (double)H(p,AngularAccelerationOffset) * elapsed * .5;
            var wy = (double)F(p,AngularVelocityOffset+4) + (double)H(p,AngularAccelerationOffset+2) * elapsed * .5;
            var wz = (double)F(p,AngularVelocityOffset+8) + (double)H(p,AngularAccelerationOffset+4) * elapsed * .5;
            var speed = Math.Sqrt(wx*wx+wy*wy+wz*wz);
            var rotation = q;
            if (speed != 0 && elapsed != 0)
            {
                var halfAngle = speed * elapsed * .5;
                var scale = Math.Sin(halfAngle)/speed;
                var ax=wx*scale; var ay=wy*scale; var az=wz*scale; var aw=Math.Cos(halfAngle);
                var qx=(double)q.X;var qy=(double)q.Y;var qz=(double)q.Z;var qw=(double)q.W;
                var rx=aw*qx+qw*ax+ay*qz-az*qy; var ry=aw*qy+qw*ay+az*qx-ax*qz;
                var rz=aw*qz+qw*az+ax*qy-ay*qx; var rw=aw*qw-ax*qx-ay*qy-az*qz;
                var inverse=1/Math.Sqrt(rx*rx+ry*ry+rz*rz+rw*rw);
                rotation=new((Half)(rx*inverse),(Half)(ry*inverse),(Half)(rz*inverse),(Half)(rw*inverse));
            }
            var centre = _localCentresOfMass[bodyIndex];
            var qx2 = (double)rotation.X; var qy2 = (double)rotation.Y; var qz2 = (double)rotation.Z; var qw2 = (double)rotation.W;
            var cx = (double)centre.X; var cy = (double)centre.Y; var cz = (double)centre.Z;
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
    private static (int Cell, Half Local) Origin((int Cell, Half Local) centre, double offset)
    {
        var local = (double)centre.Local - offset * 16;
        var carry = checked((int)Math.Floor(local + .5)); var remainder = (Half)(local - carry);
        if (remainder >= (Half).5) { carry++; remainder = (Half)(remainder - (Half)1); }
        if (remainder < (Half)(-.5)) { carry--; remainder = (Half)(remainder + (Half)1); }
        return (checked(centre.Cell + carry), remainder);
    }
    private static (int Cell, Half Local) Position(ReadOnlySpan<byte> p,int axis,int cell,double elapsed,double k,double polynomial,bool supported)
    {
        var velocity=(double)F(p,VelocityOffset+axis*4);
        var acceleration=supported?(double)H(p,SupportedAccelerationOffset+axis*2):(double)H(p,GravityOffset+axis*2)-k*velocity;
        var local=(double)H(p,48+axis*2)+(velocity*elapsed+acceleration*elapsed*elapsed*polynomial)*16;
        var carry=checked((int)Math.Floor(local+.5));var remainder=(Half)(local-carry);
        if(remainder >= (Half).5){carry++;remainder=(Half)(remainder-(Half)1);}
        if(remainder < (Half)(-.5)){carry--;remainder=(Half)(remainder+(Half)1);}
        return(checked(cell+carry),remainder);
    }
    private static void Norm(ReadOnlySpan<byte> p,int offset,double maximum)
    {
        if (Zero(p.Slice(offset,6))) return;
        var x=(double)H(p,offset);var y=(double)H(p,offset+2);var z=(double)H(p,offset+4);
        if(!double.IsFinite(x)||!double.IsFinite(y)||!double.IsFinite(z)||x*x+y*y+z*z>maximum*maximum)
            throw new ArgumentException("Motion vector exceeds its declared domain.");
    }
    private static void F32Norm(ReadOnlySpan<byte> p,int offset,double maximum)
    {
        if (Zero(p.Slice(offset,12))) return;
        var x=(double)F(p,offset);var y=(double)F(p,offset+4);var z=(double)F(p,offset+8);
        if(!double.IsFinite(x)||!double.IsFinite(y)||!double.IsFinite(z)||x*x+y*y+z*z>maximum*maximum)
            throw new ArgumentException("Motion vector exceeds its declared domain.");
    }
    private static double Time(uint ordinal,Half phase)
    {
        if(!Half.IsFinite(phase)||phase<(Half)(-2048)||phase>=(Half)2048||(ordinal==0&&phase<(Half)0))
            throw new ArgumentException("Invalid motion time.");
        return ordinal*4096.0+(double)phase;
    }
    private static CellOrigin Cell(ReadOnlySpan<byte> p)=>new(BinaryPrimitives.ReadInt32LittleEndian(p[32..]),BinaryPrimitives.ReadInt32LittleEndian(p[36..]),BinaryPrimitives.ReadInt32LittleEndian(p[40..]));
    private static CanonicalRotation Rotation(ReadOnlySpan<byte> p)=>new(H(p,56),H(p,58),H(p,60),H(p,62));
    private static bool Zero(ReadOnlySpan<byte> p) => p.IndexOfAnyExcept((byte)0) < 0;
    private static uint U32(ReadOnlySpan<byte> p,int offset)=>BinaryPrimitives.ReadUInt32LittleEndian(p[offset..]);
    private static ulong U64(ReadOnlySpan<byte> p,int offset)=>BinaryPrimitives.ReadUInt64LittleEndian(p[offset..]);
    private static float F(ReadOnlySpan<byte> p,int offset)=>BinaryPrimitives.ReadSingleLittleEndian(p[offset..]);
    private static Half H(ReadOnlySpan<byte> p,int offset)=>BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(p[offset..]));
}
