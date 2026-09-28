using System;
using Godot;

namespace CuriousContraptions.Physics;

public sealed class HollowGeometrySettings
{
    public double MaximumError { get; }
    public int MaximumChildren { get; }
    public HollowGeometrySettings(double maximumError,int maximumChildren=4096)
    {
        if(!double.IsFinite(maximumError)||maximumError<=0||maximumChildren<3)
            throw new ArgumentException("Hollow geometry needs a positive error budget and at least three available children.");
        MaximumError=maximumError; MaximumChildren=maximumChildren;
    }
}

public sealed class HollowGeometryResult
{
    public CompoundGeometry Geometry { get; }
    public double MaximumSurfaceError { get; }
    public double MinimumBoreRadius { get; }
    public double EndExtension { get; }
    public int RingSegments { get; }
    public int PathSegments { get; }
    internal HollowGeometryResult(ConvexInstance[] children,double error,double bore,double endExtension,int ring,int path)
    {
        Geometry=new(children); MaximumSurfaceError=error; MinimumBoreRadius=bore;
        EndExtension=endExtension; RingSegments=ring; PathSegments=path;
    }
}

/// <summary>Bounded-error compound walls. Tube/frustum axes are local X.
/// Bend centreline is R*(sin(a),cos(a),0), a in [0,sweep].
/// No builder accepts a difficulty level or changes collision response.</summary>
public static class HollowGeometry
{
    private static double Reserve(double scale)=>128*Math.ScaleB(1,-52)*Math.Max(1,scale);
    private static double Sagitta(double halfAngle)=>2*Math.Pow(Math.Sin(halfAngle*.5),2);
    private static double RingError(double radius,int count)=>radius*Sagitta(Math.PI/count)/Math.Cos(Math.PI/count);
    private static double Budget(HollowGeometrySettings settings,double scale,double bore)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var reserve=Reserve(scale);
        if(!double.IsFinite(scale)||!double.IsFinite(scale*scale)||settings.MaximumError<=4*reserve||settings.MaximumError>=bore)
            throw new ArgumentException("Error budget must resolve the declared scale and leave the bore open.");
        return settings.MaximumError-reserve;
    }
    private static int RingCount(double radius,double budget,HollowGeometrySettings settings)
    {
        // radius*(sec(pi/n)-1) <= budget; asin avoids subtracting nearly equal cosines.
        var halfAngle=2*Math.Asin(Math.Sqrt(budget/(2*(radius+budget))));
        var estimate=Math.Max(3,Math.Ceiling(Math.PI/halfAngle));
        if(!double.IsFinite(estimate)||estimate>settings.MaximumChildren)
            throw new ArgumentException("Requested surface precision exceeds the convex-child budget.");
        var count=(int)estimate;
        while(RingError(radius,count)>budget)
            if(++count>settings.MaximumChildren) throw new ArgumentException("Requested surface precision exceeds the convex-child budget.");
        return count;
    }
    private static void Radial(double inner,double outer)
    {
        if(!double.IsFinite(inner)||!double.IsFinite(outer)||inner<=0||outer<=inner)
            throw new ArgumentException("Shell radii must be finite, positive and strictly ordered.");
    }
    private static ConvexInstance Child(CollisionVector[] vertices,double rounding)
    {
        CollisionVector center=default;
        foreach(var vertex in vertices) center+=vertex/vertices.Length;
        // Centre each child for useful swept bounds. Subtract the exact float
        // boundary value, not its pre-conversion double approximation.
        var origin=new Vector3((float)center.X,(float)center.Y,(float)center.Z);
        if(!origin.IsFinite()) throw new ArgumentException("Child pose exceeds the scene transform range.");
        var offset=CollisionVector.From(origin);
        var local=new CollisionVector[vertices.Length];
        for(var i=0;i<vertices.Length;i++) local[i]=vertices[i]-offset;
        var hull=new ConvexHull(local);
        ConvexGeometry geometry=rounding==0?hull:new ConvexRounded(hull,rounding);
        return new(geometry,new(Basis.Identity,origin));
    }
    public static HollowGeometryResult Tube(double halfLength,double innerRadius,double outerRadius,HollowGeometrySettings settings)=>
        Frustum(halfLength,innerRadius,innerRadius,outerRadius-innerRadius,settings);

    public static HollowGeometryResult Frustum(double halfLength,double inletRadius,double outletRadius,double thickness,
        HollowGeometrySettings settings)
    {
        if(!double.IsFinite(halfLength)||halfLength<=0||!double.IsFinite(thickness)||thickness<=0)
            throw new ArgumentException("Shell half-length and radial thickness must be finite and positive.");
        Radial(inletRadius,inletRadius+thickness); Radial(outletRadius,outletRadius+thickness);
        var radius=Math.Max(inletRadius,outletRadius)+thickness;
        var bore=Math.Min(inletRadius,outletRadius); var scale=halfLength+radius;
        var budget=Budget(settings,scale,bore); var count=RingCount(radius,budget,settings);
        var cosine=Math.Cos(Math.PI/count); var children=new ConvexInstance[count];
        for(var ring=0;ring<count;ring++)
        {
            var a=Math.Tau*ring/count; var b=Math.Tau*(ring+1)/count;
            var vertices=new CollisionVector[8]; var index=0;
            foreach(var end in new[]{-1,1})
            {
                var inner=end<0?inletRadius:outletRadius; var outer=(inner+thickness)/cosine;
                foreach(var r in new[]{inner,outer})
                foreach(var angle in new[]{a,b})
                    vertices[index++]=new(end*halfLength,r*Math.Cos(angle),r*Math.Sin(angle));
            }
            children[ring]=Child(vertices,0);
        }
        var error=RingError(radius,count)+Reserve(scale);
        return new(children,error,bore-error,Reserve(scale),count,1);
    }
    public static HollowGeometryResult Bend(double radius,double sweep,double innerRadius,double outerRadius,HollowGeometrySettings settings)
    {
        Radial(innerRadius,outerRadius);
        if(!double.IsFinite(radius)||radius<=outerRadius||!double.IsFinite(sweep)||sweep<=0||sweep>Math.PI)
            throw new ArgumentException("Bend needs a non-self-intersecting centreline and sweep in (0,pi].");
        var scale=radius+outerRadius; var budget=Budget(settings,scale,Math.Min(innerRadius,radius-outerRadius));
        var rings=RingCount(outerRadius,budget*.5,settings);
        while(outerRadius/Math.Cos(Math.PI/rings)>=radius)
            if(++rings>settings.MaximumChildren) throw new ArgumentException("Bend cross-section cannot fit the convex-child budget.");
        var outer=outerRadius/Math.Cos(Math.PI/rings);
        var ringError=RingError(outerRadius,rings);
        var coefficient=2*radius+outerRadius+outer;
        var step=4*Math.Asin(Math.Sqrt((budget-ringError)/(2*coefficient)));
        var estimate=Math.Max(1,Math.Ceiling(sweep/step));
        if(!double.IsFinite(estimate)||estimate>settings.MaximumChildren/rings)
            throw new ArgumentException("Requested bend precision exceeds the convex-child budget.");
        var paths=(int)estimate;
        double Error(int count)=>ringError+coefficient*Sagitta(sweep/(2*count));
        while(Error(paths)>budget)
            if(++paths>settings.MaximumChildren/rings) throw new ArgumentException("Requested bend precision exceeds the convex-child budget.");
        var padding=(radius+outerRadius)*Sagitta(sweep/(2*paths));
        var children=new ConvexInstance[checked(rings*paths)];
        for(var along=0;along<paths;along++)
        for(var ring=0;ring<rings;ring++)
        {
            var vertices=new CollisionVector[8]; var index=0;
            foreach(var a in new[]{sweep*along/paths,sweep*(along+1)/paths})
            foreach(var r in new[]{innerRadius,outer})
            foreach(var p in new[]{Math.Tau*ring/rings,Math.Tau*(ring+1)/rings})
            {
                var radial=radius+r*Math.Cos(p);
                vertices[index++]=new(radial*Math.Sin(a),radial*Math.Cos(a),r*Math.Sin(p));
            }
            children[along*rings+ring]=Child(vertices,padding);
        }
        var error=Error(paths)+Reserve(scale);
        return new(children,error,innerRadius-error,padding+Reserve(scale),rings,paths);
    }
}
