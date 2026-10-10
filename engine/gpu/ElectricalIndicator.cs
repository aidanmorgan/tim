using System;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

public enum ElectricalIndicator : byte { Supply, Quarter, Half, ThreeQuarters, Full }
public readonly record struct ElectricalIndicatorSample(float Supply, float Quarter, float Half, float ThreeQuarters, float Full)
{
    public float this[ElectricalIndicator indicator] => indicator switch
    {
        ElectricalIndicator.Supply => Supply, ElectricalIndicator.Quarter => Quarter,
        ElectricalIndicator.Half => Half, ElectricalIndicator.ThreeQuarters => ThreeQuarters,
        ElectricalIndicator.Full => Full, _ => throw new ArgumentException("Unknown electrical indicator.")
    };
}
