using Godot;
using System;

namespace CuriousContraptions;

public enum OpticalColour { Broadband, Red, Green, Blue }

public static class OpticalColours
{
    public const float RequiredPurity=.9f;
    public static Vector3 Mask(OpticalColour colour)=>colour switch
    {
        OpticalColour.Broadband=>Vector3.One,
        OpticalColour.Red=>Vector3.Right,
        OpticalColour.Green=>Vector3.Up,
        OpticalColour.Blue=>Vector3.Back,
        _=>throw new ArgumentOutOfRangeException(nameof(colour))
    };
    public static Color Ink(OpticalColour colour)=>colour switch
    {
        OpticalColour.Broadband=>new("#fff0a5"),
        OpticalColour.Red=>new("#de7058"),
        OpticalColour.Green=>new("#62aa78"),
        OpticalColour.Blue=>new("#5b9cdb"),
        _=>throw new ArgumentOutOfRangeException(nameof(colour))
    };
    public static float Strength(Vector3 power,OpticalColour colour)=>
        colour==OpticalColour.Broadband?(power.X+power.Y+power.Z)/3:power.Dot(Mask(colour));
    public static bool Accepts(Vector3 power,OpticalColour colour,float threshold)
    {
        var strength=Strength(power,colour);
        return strength>=threshold&&(colour==OpticalColour.Broadband||
            strength>=RequiredPurity*(power.X+power.Y+power.Z));
    }
    public static Color BeamInk(Vector3 power)
    {
        // Existing palette encodes channels; broadband keeps the established warm beam.
        if(power.X>0&&power.Y>0&&power.Z>0)return Ink(OpticalColour.Broadband);
        var total=power.X+power.Y+power.Z;
        if(total<=0)return new Color(0,0,0,0);
        return (Ink(OpticalColour.Red)*power.X+Ink(OpticalColour.Green)*power.Y+
            Ink(OpticalColour.Blue)*power.Z)/total;
    }
    public static void Marks(Node3D parent,OpticalColour colour,Vector3 centre)
    {
        var count=colour switch
        {
            OpticalColour.Broadband=>0,OpticalColour.Red=>1,
            OpticalColour.Green=>2,OpticalColour.Blue=>3,
            _=>throw new ArgumentOutOfRangeException(nameof(colour))
        };
        for(var i=0;i<count;i++)
            PartArt.Box(parent,new(.015f,.12f,.045f),Ink(colour),
                centre+Vector3.Back*((i-(count-1)*.5f)*.12f));
    }
}
