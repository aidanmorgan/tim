using Godot;
using System;

namespace CuriousContraptions;

public enum OpticalColour { Broadband, Red, Green, Blue, Yellow, Cyan, Magenta, White }

public static class OpticalColours
{
    public const float RequiredPurity=.9f;
    public static Vector3 Mask(OpticalColour colour)=>colour switch
    {
        OpticalColour.Broadband=>Vector3.One,
        OpticalColour.Red=>Vector3.Right,
        OpticalColour.Green=>Vector3.Up,
        OpticalColour.Blue=>Vector3.Back,
        OpticalColour.Yellow=>new(1,1,0),
        OpticalColour.Cyan=>new(0,1,1),
        OpticalColour.Magenta=>new(1,0,1),
        OpticalColour.White=>Vector3.One,
        _=>throw new ArgumentOutOfRangeException(nameof(colour))
    };
    public static Color Ink(OpticalColour colour)=>colour switch
    {
        OpticalColour.Broadband=>new("#fff0a5"),
        OpticalColour.Red=>new("#de7058"),
        OpticalColour.Green=>new("#62aa78"),
        OpticalColour.Blue=>new("#5b9cdb"),
        OpticalColour.Yellow=>new("#f7cb52"),
        OpticalColour.Cyan=>new("#66b8c9"),
        OpticalColour.Magenta=>new("#ed6378"),
        OpticalColour.White=>new("#fff8e9"),
        _=>throw new ArgumentOutOfRangeException(nameof(colour))
    };
    public static float Strength(Vector3 power,OpticalColour colour)
    {
        if(colour==OpticalColour.Broadband)return (power.X+power.Y+power.Z)/3;
        var mask=Mask(colour);
        var strength=float.PositiveInfinity;
        for(var i=0;i<3;i++)if(mask[i]>0)strength=Mathf.Min(strength,power[i]);
        return strength;
    }
    public static bool Accepts(Vector3 power,OpticalColour colour,float threshold)
    {
        var strength=Strength(power,colour);
        return strength>=threshold&&(colour==OpticalColour.Broadband||
            power.Dot(Mask(colour))>=RequiredPurity*(power.X+power.Y+power.Z));
    }
    public static Color BeamInk(Vector3 power)
    {
        // Existing palette encodes channels; broadband keeps the established warm beam.
        if(power.X>0&&power.Y>0&&power.Z>0)return Ink(OpticalColour.Broadband);
        if(power.X>0&&power.Y>0)return Ink(OpticalColour.Yellow);
        if(power.Y>0&&power.Z>0)return Ink(OpticalColour.Cyan);
        if(power.X>0&&power.Z>0)return Ink(OpticalColour.Magenta);
        var total=power.X+power.Y+power.Z;
        if(total<=0)return new Color(0,0,0,0);
        return (Ink(OpticalColour.Red)*power.X+Ink(OpticalColour.Green)*power.Y+
            Ink(OpticalColour.Blue)*power.Z)/total;
    }
    public static void Marks(Node3D parent,OpticalColour colour,Vector3 centre)
    {
        if(colour==OpticalColour.Broadband)return;
        var mask=Mask(colour);
        var row=0;
        var rows=(int)(mask.X+mask.Y+mask.Z);
        foreach(var channel in new[]{OpticalColour.Red,OpticalColour.Green,OpticalColour.Blue})
        {
            if(mask.Dot(Mask(channel))==0)continue;
            var count=(int)channel;
            for(var i=0;i<count;i++)
                PartArt.Box(parent,new(.015f,.1f,.04f),Ink(channel),
                    centre+Vector3.Up*((row-(rows-1)*.5f)*.13f)+Vector3.Back*((i-(count-1)*.5f)*.12f));
            row++;
        }
    }
}
