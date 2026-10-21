using System;

namespace CuriousContraptions.Gpu;

/// <summary>Why a stored construction save was refused by its structural pre-validation. Closed set; the display text is the
/// exception message.</summary>
public enum WorkshopSaveRejection : uint
{
    /// <summary>Never a rejection; constructing one with it is a programmer error.</summary>
    None = 0,
    /// <summary>The stored browser text is not base64.</summary>
    NotBase64,
    /// <summary>Shorter than its size prefix declares.</summary>
    Truncated,
    /// <summary>Longer than its size prefix declares.</summary>
    TrailingBytes,
    /// <summary>Not a construction save: another root's identifier, or none.</summary>
    UnknownIdentifier,
    /// <summary>The FlatBuffers verifier refused its offsets, vectors or strings.</summary>
    VerifierRejected,
    /// <summary>A required record or field is absent.</summary>
    MissingField,
    /// <summary>An enum field holds an undefined value.</summary>
    UndefinedEnum,
    /// <summary>A numeric field is NaN or infinite.</summary>
    NonFinite,
    /// <summary>A numeric field is outside its declared range, or a part differs from its declared material.</summary>
    OutOfRange,
    /// <summary>No delivered element has that identity.</summary>
    UnknownElement,
    /// <summary>Two instances share one identity.</summary>
    DuplicateInstance,
    /// <summary>A connection names an instance the save does not hold.</summary>
    UnresolvedReference,
    /// <summary>A connection does not join matching declared ports.</summary>
    InvalidConnection,
    /// <summary>The next part identity is not above every saved identity.</summary>
    InvalidAllocator,
}

/// <summary>A stored construction save failed structural pre-validation, before any domain construction ran. It is the
/// player's refusal: the message is shown and the current bench is kept. Deliberately not an ArgumentException, so a domain
/// argument guard that fires after pre-validation stays a programmer-error fault and is never shown as this refusal.</summary>
public sealed class WorkshopSaveRejectedException : Exception
{
    public WorkshopSaveRejectedException(WorkshopSaveRejection reason, string message, Exception? cause = null) : base(message, cause)
    {
        if (reason == WorkshopSaveRejection.None || !Enum.IsDefined(reason))
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Undefined save rejection.");
        Reason = reason;
    }

    public WorkshopSaveRejection Reason { get; }
}
