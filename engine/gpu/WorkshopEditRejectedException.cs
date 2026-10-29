using System;

namespace CuriousContraptions.Gpu;

/// <summary>Why a proposed Workshop edit was refused. Closed set; the display text is the exception message.</summary>
public enum WorkshopEditRejection : uint
{
    /// <summary>Never a rejection; constructing one with it is a programmer error.</summary>
    None = 0,
    /// <summary>A fixed puzzle part, fixture or authored declaration would change.</summary>
    FixedPart,
    /// <summary>The selected part has no setting of that kind to edit.</summary>
    NotConfigurable,
    /// <summary>A player-entered value is outside its authored range.</summary>
    OutOfRange,
    /// <summary>No part identity is left to allocate.</summary>
    IdentityExhausted,
    /// <summary>The stored browser save uses other simulation settings.</summary>
    SaveSettingsMismatch,
    /// <summary>A connection is duplicated or does not join matching declared ports.</summary>
    InvalidConnection,
    /// <summary>A level output (State Latch, Pressure Plate) may drive only an input that admits a level.</summary>
    LevelDrivesLevelInputOnly,
    /// <summary>An input that follows a level output takes no other signal.</summary>
    LevelInputFollowsOneLevel,
    /// <summary>The power input already has a supplier.</summary>
    PowerInputSupplied,
    /// <summary>The puzzle's inventory is used up.</summary>
    InventoryExceeded,
    /// <summary>The puzzle does not admit that part, or lacks one it requires.</summary>
    InventoryUnsupported,
    /// <summary>The puzzle's authored settings do not admit the change.</summary>
    PuzzleSettings,
    /// <summary>The Workshop has no bench placement for that part.</summary>
    UnsupportedPart,
    /// <summary>A wire would close a loop through a gate whose output more input power can switch off.</summary>
    NonmonotoneFeedback,
    /// <summary>A payload starts inside a solid hollow wall.</summary>
    InitialOverlap,
}

/// <summary>
/// A declared rule refused a proposed Workshop edit: a construction, a part's settings, a connection or a puzzle change
/// (a stored save is refused by WorkshopSaveRejectedException). It is the only edit refusal a UI handler shows as one (with
/// an indeterminate transport and a save refusal); any other exception, ArgumentException included,
/// is a defect. It is an ArgumentException
/// so the decode and admission boundaries that reject invalid input by that type keep rejecting it.
/// </summary>
public sealed class WorkshopEditRejectedException : ArgumentException
{
    public WorkshopEditRejection Reason { get; }

    public WorkshopEditRejectedException(WorkshopEditRejection reason, string message, Exception? cause = null) : base(message, cause)
    {
        if (reason == WorkshopEditRejection.None || !Enum.IsDefined(reason))
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Undefined edit rejection.");
        Reason = reason;
    }
}
