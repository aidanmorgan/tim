using System.Collections.Generic;

namespace CuriousContraptions.Abstractions;

/// <summary>What one authored parameter holds. A Choice parameter selects one of its named options.</summary>
public enum ParameterKind : byte
{
    Real = 1,
    WholeNumber = 2,
    Boolean = 3,
    Choice = 4
}

/// <summary>The unit of a parameter's values.</summary>
public enum ParameterUnit : byte
{
    None = 1,
    Metre = 2,
    Kilogram = 3,
    Second = 4,
    MetrePerSecond = 5,
    MetrePerSecondSquared = 6,
    NewtonPerMetre = 7,
    NewtonSecondPerMetre = 8,
    Joule = 9,
    Watt = 10,
    Degree = 11,
    RadianPerSecond = 12,
    NewtonMetre = 13,
    Newton = 14
}

/// <summary>One typed authored parameter of an element: its key, unit and kind, with f32 bounds and default. A Choice parameter
/// lists its options by name; its minimum is 0, its maximum is the last option's index and its default is an index.</summary>
public enum ParameterEditorPolicy : byte { Reject, Clamp }

/// <summary>When a player may change a parameter. BuildOnly values change only in Build, as a construction edit. A RunControl
/// value also has a run-time control while Running or Paused: the control changes the running world only, never the authored
/// construction, so Reset and a save keep the authored value.</summary>
public enum ParameterRunEdit : byte { BuildOnly, RunControl }

/// <summary>The engine lane a RunControl parameter drives while Running or Paused (closed set; 0 is undefined). A RunControl parameter
/// binds exactly one lane, resolved from its package's declared capabilities: today a Source store's enable state is the SourceEnable
/// lane. The compiled run-control table names each row's lane.</summary>
public enum RunControlLane : byte { SourceEnable = 1 }

public readonly record struct ParameterDescriptor(
    string Key,
    ParameterUnit Unit,
    ParameterKind Kind,
    float Minimum,
    float Maximum,
    float Default,
    IReadOnlyList<string> Options,
    ParameterEditorPolicy EditorPolicy = ParameterEditorPolicy.Reject,
    ParameterRunEdit RunEdit = ParameterRunEdit.BuildOnly);
