using Godot;
using System;

namespace CuriousContraptions;

public enum AcousticVoice { Speaker, Bell }

/// <summary>Presentation-only synthesized audio; simulation never reads playback state.</summary>
public static class AcousticAudio
{
    public static AudioStreamWav Create(ToneBand tone,AcousticVoice voice)
    {
        const int rate=22050;
        var frequency=tone switch {ToneBand.Low=>220,ToneBand.Mid=>440,ToneBand.High=>880,_=>throw new ArgumentOutOfRangeException(nameof(tone))};
        var duration=voice switch {AcousticVoice.Speaker=>AcousticPulse.Duration,AcousticVoice.Bell=>.7f,_=>throw new ArgumentOutOfRangeException(nameof(voice))};
        var count=(int)(rate*duration);
        var data=new byte[count*2];
        for(var i=0;i<count;i++)
        {
            var t=(float)i/rate;
            var phase=MathF.Tau*frequency*t;
            var wave=voice==AcousticVoice.Bell
                ? (MathF.Sin(phase)+.45f*MathF.Sin(phase*2.76f)*MathF.Exp(-8*t)+.2f*MathF.Sin(phase*5.4f)*MathF.Exp(-12*t))/1.65f
                : MathF.Sin(phase);
            var attack=voice==AcousticVoice.Bell?.003f:.01f;
            var envelope=MathF.Min(1,t/attack)*MathF.Pow(1-(float)i/count,2);
            var sample=(short)(wave*envelope*12000);
            data[i*2]=(byte)(sample&255);data[i*2+1]=(byte)((sample>>8)&255);
        }
        return new AudioStreamWav {Format=AudioStreamWav.FormatEnum.Format16Bits,MixRate=rate,Stereo=false,Data=data};
    }
}
