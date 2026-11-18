using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Bundle;

/// <summary>Admission of the bounded scalar/product domain; no runtime expression interpreter.</summary>
public static class MechanicalValueDomain
{
    public static bool TryRange(MechanicalValue value, IReadOnlyList<ParameterDescriptor> parameters, ParameterUnit unit,
        out float minimum, out float maximum)
    {
        minimum = maximum = 0f;
        if (value.Source == MechanicalValueSource.Product)
        {
            if (unit != ParameterUnit.Watt || value.Constant != 0f || value.Parameter is not null ||
                value.Product is not { HasPowerUnits: true } product) return false;
            return ProductRange(product, parameters, out minimum, out maximum);
        }
        if (value.Product is not null) return false;
        return ScalarRange(value.Source, value.Constant, value.Parameter, parameters, unit, out minimum, out maximum);
    }

    private static bool ProductRange(MechanicalProduct product, IReadOnlyList<ParameterDescriptor> parameters,
        out float minimum, out float maximum)
    {
        minimum = maximum = 0f;
        var a = product.Left;
        var b = product.Right;
        if (!ScalarRange(a.Source, a.Constant, a.Parameter, parameters, a.Unit, out var amin, out var amax) ||
            !ScalarRange(b.Source, b.Constant, b.Parameter, parameters, b.Unit, out var bmin, out var bmax) ||
            amin < 0f || bmin < 0f) return false;
        minimum = amin * bmin;
        maximum = amax * bmax;
        return float.IsFinite(minimum) && float.IsFinite(maximum);
    }

    private static bool ScalarRange(MechanicalValueSource source, float constant, string? key,
        IReadOnlyList<ParameterDescriptor> parameters, ParameterUnit unit, out float minimum, out float maximum)
    {
        minimum = maximum = 0f;
        if (source == MechanicalValueSource.Constant && key is null && float.IsFinite(constant))
        {
            minimum = maximum = constant;
            return true;
        }
        if (source != MechanicalValueSource.Parameter || constant != 0f || key is null) return false;
        var matches = parameters.Where(p => p.Key == key).ToArray();
        if (matches.Length != 1) return false;
        var parameter = matches[0];
        if (parameter.Kind != ParameterKind.Real || parameter.Unit != unit || !float.IsFinite(parameter.Minimum) ||
            !float.IsFinite(parameter.Maximum) || parameter.Minimum > parameter.Maximum) return false;
        minimum = parameter.Minimum;
        maximum = parameter.Maximum;
        return true;
    }

    public static bool SliderTravel(PrismaticSliderDescriptor slider, IReadOnlyList<ParameterDescriptor> parameters)
    {
        MechanicalValue[] fields = slider.Drive is { } drive
            ? [slider.LowerLimit, slider.UpperLimit, slider.Compression, drive.Target]
            : [slider.LowerLimit, slider.UpperLimit, slider.Compression];
        foreach (var value in fields)
            if (value.Source == MechanicalValueSource.Product ||
                !TryRange(value, parameters, ParameterUnit.Metre, out _, out _)) return false;
        var keys = fields.Where(v => v.Source == MechanicalValueSource.Parameter).Select(v => v.Parameter!).Distinct().ToArray();
        var values = new Dictionary<string, float>(StringComparer.Ordinal);
        return Visit(0);

        bool Visit(int index)
        {
            if (index == keys.Length)
            {
                var lower = slider.LowerLimit.Evaluate(values);
                var upper = slider.UpperLimit.Evaluate(values);
                var initial = -slider.Compression.Evaluate(values);
                if (!(lower < upper) || lower > 0f || upper < 0f || initial < lower || initial > upper) return false;
                return slider.Drive is not { } motor ||
                    (motor.Target.Evaluate(values) >= lower && motor.Target.Evaluate(values) <= upper);
            }
            var parameter = parameters.Single(p => p.Key == keys[index]);
            values[keys[index]] = parameter.Minimum;
            if (!Visit(index + 1)) return false;
            values[keys[index]] = parameter.Maximum;
            return Visit(index + 1);
        }
    }
}
