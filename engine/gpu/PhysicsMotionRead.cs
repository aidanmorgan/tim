using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public enum PhysicsMotionKind : uint { FreePolynomial = 1, SupportedQuadratic = 2 }

/// <summary>Immutable GPU-authored render trajectory for exactly one complete physical commit.</summary>
public sealed class PhysicsMotionRead
{
    public const int Capacity = 72;
    public const int PieceBytes = 128;
    public const int HeaderBytes = 16;
    public const int ByteLength = HeaderBytes + Capacity * PieceBytes;
    private readonly byte[] _bytes;
    private readonly SimulationTick _tick;
    private readonly GpuBodyId? _bodyId;
    public ReadOnlySpan<byte> Bytes => _bytes;
    public int Count => checked((int)U32(_bytes, 0));
    public uint Substeps => U32(_bytes, 4);
    public uint FirstOrdinal => U32(_bytes, 8);
    public uint LastOrdinal => U32(_bytes, 12);
    private PhysicsMotionRead(ReadOnlySpan<byte> bytes, CanonicalBody? body, SimulationTick tick)
    {
        _bytes = bytes.ToArray();
        _tick = tick;
        _bodyId = body?.Id;
    }

    public static PhysicsMotionRead Decode(ReadOnlySpan<byte> bytes, CanonicalBody? body, SimulationTick tick)
    {
        var motion = new PhysicsMotionRead(bytes, body, tick);
        Validate(motion._bytes, body, tick);
        return motion;
    }

    /// <summary>Rebinds an already validated immutable payload without scanning its bytes again.</summary>
    public void ValidateBinding(CanonicalBody? body, SimulationTick tick)
    {
        // Admission's canonical zero payload has no preceding body trajectory.
        if (tick != _tick || (tick.Value != 0 && body?.Id != _bodyId))
            throw new ArgumentException("Motion does not match its owning body and commit.");
    }

    public static void Validate(ReadOnlySpan<byte> bytes, CanonicalBody? body, SimulationTick tick)
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
            U32(bytes, 8) + steps != U32(bytes, 12) || count > steps * 9 ||
            (body.HasValue ? count == 0 : count != 0))
            throw new ArgumentException("Motion does not cover its owning commit.");
        double previous = U32(bytes, 8) * 4096.0;
        for (var i = 0; i < count; i++)
        {
            var piece = bytes.Slice(HeaderBytes + checked((int)i) * PieceBytes, PieceBytes);
            var kind = (PhysicsMotionKind)U32(piece, 0);
            var start = Time(U32(piece, 4), H(piece, 16));
            var end = Time(U32(piece, 8), H(piece, 18));
            var anchor = Time(U32(piece, 12), H(piece, 20));
            if (!Enum.IsDefined(kind) || U64(piece, 24) != body!.Value.Id.Value || start != previous ||
                end <= start || end > U32(bytes, 12) * 4096.0 || anchor > start ||
                end - anchor > (kind == PhysicsMotionKind.SupportedQuadratic ? 4096 : PhysicsGpuAbi.PrimarySegmentSteps * 4096) ||
                !HalfBits.Equal(H(piece, 22), (Half)WorkshopCadenceSettings.PhysicalFrequency) ||
                !Zero(piece[44..48]) || !Zero(piece[70..72]) || !Zero(piece[78..80]) ||
                !Zero(piece[86..88]) || !Zero(piece[94..96]) || !Zero(piece[102..128]))
                throw new ArgumentException("Invalid motion piece identity, interval or padding.");
            var launch = new CanonicalBody(body.Value.Id, body.Value.Epoch, body.Value.Tick,
                Cell(piece), new(H(piece,48),H(piece,50),H(piece,52)),
                new(H(piece,64),H(piece,66),H(piece,68)));
            launch.Validate();
            Norm(piece,64,2); Norm(piece,72,64); Norm(piece,80,16); Norm(piece,88,64); Norm(piece,96,1024);
            Rotation(piece).Validate();
            PhysicsDeclarationBounds.Range(H(piece,54),(Half)0,(Half).125);
            if (kind == PhysicsMotionKind.FreePolynomial && (!Zero(piece[88..94]) || !Zero(piece[96..102])))
                throw new ArgumentException("Free motion carries constrained acceleration.");
            previous = end;
        }
        if (body.HasValue && previous != U32(bytes, 12) * 4096.0)
            throw new ArgumentException("Motion contains a missing tail.");
    }

    /// <summary>Render-only sampling of the committed coefficient record; never a physical state update.</summary>
    public bool TrySample(double physicalOrdinal, out PresentedBody pose)
    {
        if (!double.IsFinite(physicalOrdinal)) throw new ArgumentException("Invalid render sample time.");
        var units = physicalOrdinal * 4096;
        for (var i = 0; i < Count; i++)
        {
            var p = _bytes.AsSpan(HeaderBytes + i * PieceBytes, PieceBytes);
            var begin = Time(U32(p,4),H(p,16)); var end = Time(U32(p,8),H(p,18));
            // Right-continuous impact selection. The last exact endpoint comes from its committed read.
            if (units < begin || units >= end) continue;
            var elapsed = (physicalOrdinal - U32(p,12) - (double)H(p,20) / 4096) / (double)H(p,22);
            var supported = (PhysicsMotionKind)U32(p,0) == PhysicsMotionKind.SupportedQuadratic;
            var k = (double)H(p,54); var x = supported ? 0 : k * elapsed;
            var polynomial = .5 + x * ((double)(Half)(-1.0/6) + x * ((double)(Half)(1.0/24) +
                x * ((double)(Half)(-1.0/120) + x * (double)(Half)(1.0/720))));
            var cell = Cell(p);
            var px = Position(p,0,cell.X,elapsed,k,polynomial,supported);
            var py = Position(p,1,cell.Y,elapsed,k,polynomial,supported);
            var pz = Position(p,2,cell.Z,elapsed,k,polynomial,supported);
            var q = Rotation(p);
            var wx = (double)H(p,72) + (double)H(p,96) * elapsed * .5;
            var wy = (double)H(p,74) + (double)H(p,98) * elapsed * .5;
            var wz = (double)H(p,76) + (double)H(p,100) * elapsed * .5;
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
            pose = new(new(U64(p,24)),new(px.Cell,py.Cell,pz.Cell),new(px.Local,py.Local,pz.Local),rotation);
            return true;
        }
        pose=default;return false;
    }
    private static (int Cell, Half Local) Position(ReadOnlySpan<byte> p,int axis,int cell,double elapsed,double k,double polynomial,bool supported)
    {
        var velocity=(double)H(p,64+axis*2)*32;
        var acceleration=supported?(double)H(p,88+axis*2):(double)H(p,80+axis*2)-k*velocity;
        var local=(double)H(p,48+axis*2)+(velocity*elapsed+acceleration*elapsed*elapsed*polynomial)*16;
        var carry=checked((int)Math.Floor(local+.5));var remainder=(Half)(local-carry);
        if(remainder >= (Half).5){carry++;remainder=(Half)(remainder-(Half)1);}
        if(remainder < (Half)(-.5)){carry--;remainder=(Half)(remainder+(Half)1);}
        return(checked(cell+carry),remainder);
    }
    private static void Norm(ReadOnlySpan<byte> p,int offset,double maximum)
    {
        var x=(double)H(p,offset);var y=(double)H(p,offset+2);var z=(double)H(p,offset+4);
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
    private static Half H(ReadOnlySpan<byte> p,int offset)=>BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(p[offset..]));
}
