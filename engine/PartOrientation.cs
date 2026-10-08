using System;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CuriousContraptions;

/// <summary>Lossless authored basis. Euler angles are input gestures, never saved state.
/// Construction supports proper rigid orientations only; dimensions belong to part parameters.</summary>
[JsonConverter(typeof(PartOrientationJsonConverter))]
public sealed record PartOrientation
{
    public Vector3 X { get; }
    public Vector3 Y { get; }
    public Vector3 Z { get; }
    public static PartOrientation Identity { get; }=new(Vector3.UnitX,Vector3.UnitY,Vector3.UnitZ);
    public PartOrientation(Vector3 x,Vector3 y,Vector3 z)
    {
        static bool Finite(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z);
        if(!Finite(x)||!Finite(y)||!Finite(z)||
            Math.Abs(x.LengthSquared()-1)>1e-5||Math.Abs(y.LengthSquared()-1)>1e-5||
            Math.Abs(z.LengthSquared()-1)>1e-5||Math.Abs(Vector3.Dot(x,y))>1e-5||
            Math.Abs(Vector3.Dot(x,z))>1e-5||Math.Abs(Vector3.Dot(y,z))>1e-5||
            Math.Abs(Vector3.Dot(x,Vector3.Cross(y,z))-1)>1e-4)
            throw new ArgumentException("Authored orientation must be a finite proper rigid basis.");
        X=x;Y=y;Z=z;
    }
    public Vector3 Transform(Vector3 point)=>X*point.X+Y*point.Y+Z*point.Z;
    public static PartOrientation operator *(PartOrientation a,PartOrientation b)
    {
        ArgumentNullException.ThrowIfNull(a);ArgumentNullException.ThrowIfNull(b);
        return new(a.Transform(b.X),a.Transform(b.Y),a.Transform(b.Z));
    }
    /// <summary>Authoring boundary, Y-X-Z order matching the scene editor.</summary>
    public static PartOrientation FromEulerDegrees(float x,float y,float z)
    {
        if(!float.IsFinite(x)||!float.IsFinite(y)||!float.IsFinite(z))
            throw new ArgumentOutOfRangeException(nameof(x),"Euler input must be finite.");
        const float radians=MathF.PI/180;
        var sx=MathF.Sin(x*radians);var cx=MathF.Cos(x*radians);
        var sy=MathF.Sin(y*radians);var cy=MathF.Cos(y*radians);
        var sz=MathF.Sin(z*radians);var cz=MathF.Cos(z*radians);
        return new(new(cy*cz+sy*sx*sz,cx*sz,-sy*cz+cy*sx*sz),
            new(-cy*sz+sy*sx*cz,cx*cz,sy*sz+cy*sx*cz),new(sy*cx,-sx,cy*cx));
    }
    public static PartOrientation FromEulerDegrees(ReadOnlySpan<float> degrees)
    {
        if(degrees.Length!=3)throw new ArgumentException("Euler input requires three components.",nameof(degrees));
        return FromEulerDegrees(degrees[0],degrees[1],degrees[2]);
    }
    /// <summary>UI gesture-recipe boundary only. Never use this lossy view for saving or Reset.</summary>
    public Vector3 ToEulerDegrees()
    {
        var cosine=MathF.Sqrt(Z.X*Z.X+Z.Z*Z.Z);
        var x=MathF.Atan2(-Z.Y,cosine);
        var y=cosine>1e-6?MathF.Atan2(Z.X,Z.Z):MathF.Atan2(-X.Z,X.X);
        var z=cosine>1e-6?MathF.Atan2(X.Y,Y.Y):0;
        return new Vector3(x,y,z)*(180/MathF.PI);
    }
}

public sealed class PartOrientationJsonConverter : JsonConverter<PartOrientation>
{
    public override bool HandleNull=>true;
    public override PartOrientation Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)
    {
        if(reader.TokenType!=JsonTokenType.StartArray)throw new JsonException("Orientation requires nine basis components.");
        Span<float> values=stackalloc float[9];
        for(var i=0;i<values.Length;i++)
            if(!reader.Read()||reader.TokenType!=JsonTokenType.Number||!reader.TryGetSingle(out values[i]))
                throw new JsonException("Orientation requires nine finite basis components.");
        if(!reader.Read()||reader.TokenType!=JsonTokenType.EndArray)
            throw new JsonException("Orientation requires exactly nine basis components.");
        try {return new(new(values[0],values[1],values[2]),new(values[3],values[4],values[5]),new(values[6],values[7],values[8]));}
        catch(ArgumentException error) {throw new JsonException("Invalid authored orientation.",error);}
    }
    public override void Write(Utf8JsonWriter writer,PartOrientation value,JsonSerializerOptions options)
    {
        if(value is null)throw new JsonException("Orientation cannot be null.");
        writer.WriteStartArray();
        foreach(var column in new[]{value.X,value.Y,value.Z})
        {
            writer.WriteNumberValue(column.X);writer.WriteNumberValue(column.Y);writer.WriteNumberValue(column.Z);
        }
        writer.WriteEndArray();
    }
}
