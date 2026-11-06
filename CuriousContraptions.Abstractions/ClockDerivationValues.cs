using System;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

/// <summary>One derivation exchange of the master context with the Simulation, taken under one derivation epoch.</summary>
public readonly record struct ClockDerivationExchange(long Before, long Master, long After, ClockDerivationEpoch Epoch);
/// <summary>What one exchange proved. Only a bracket at most the declared maximum bracket
/// wide is evidence; a wider one, or none at all, proves nothing either way.</summary>
public enum ClockDerivationOutcome : byte { Consistent, Slewed, Rederived, Inconclusive, Unanswered }
public enum ClockDerivationReport : byte { Check, Startup, Summary }
