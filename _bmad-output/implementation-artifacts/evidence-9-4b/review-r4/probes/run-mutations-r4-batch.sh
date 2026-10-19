#!/bin/zsh
# Round 4 mutant batch (Murdoch-2): the amended D23 renewal in WorkshopClockMapping.cs, run in the isolated copy mut5.
set -u
S=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad/murdoch2-9-4b
F=engine/gpu/WorkshopClockMapping.cs
r() { zsh $S/run-mutation-r4.sh "$1" tests $F "$2"; }
r M4-01-width-bound-removed 's/if \(renewing && !QualifiesThroughRefresh\(interval\)\) outcome = ClockProbeOutcome.Inconclusive;/;/'
r M4-02-inconclusive-retained 's/if \(outcome == ClockProbeOutcome.Accepted\)\n(\s+)\{/if (outcome is ClockProbeOutcome.Accepted or ClockProbeOutcome.Inconclusive)\n$1\{/g'
r M4-03-inconclusive-recalibrates 's/var expired = !renewed && outcome != ClockProbeOutcome.Inconclusive && /var expired = !renewed && /'
r M4-04-no-exhaustion 's/var exhausted = outcome == ClockProbeOutcome.Inconclusive && _recoverySends == RecoveryProbeLimit;/var exhausted = false;/'
r M4-05-renewing-no-time-bound 's/if \(State is ClockMappingState.Recovering or ClockMappingState.Renewing && \(Int128\)now.Value - _recoveryStarted > RecoveryLifetime\)/if (State == ClockMappingState.Recovering && (Int128)now.Value - _recoveryStarted > RecoveryLifetime)/'
r M4-06-renewing-no-send-cap 's/if \(State is ClockMappingState.Recovering or ClockMappingState.Renewing && _recoverySends == RecoveryProbeLimit\) return null;/if (State == ClockMappingState.Recovering && _recoverySends == RecoveryProbeLimit) return null;/'
r M4-07-renewing-sends-uncounted 's/if \(State is ClockMappingState.Recovering or ClockMappingState.Renewing\) _recoverySends\+\+;/if (State == ClockMappingState.Recovering) _recoverySends++;/'
r M4-08-record-per-exchange 's/if \(renewed && outcome == ClockProbeOutcome.Accepted\) RecordLateRenewal\(/if ((renewed && outcome == ClockProbeOutcome.Accepted) || outcome == ClockProbeOutcome.Inconclusive) RecordLateRenewal(/'
r M4-09-qualified-while-renewing 's/public bool IsQualified => State is ClockMappingState.Ready or ClockMappingState.Recovering;/public bool IsQualified => State is ClockMappingState.Ready or ClockMappingState.Recovering or ClockMappingState.Renewing;/'
r M4-10-no-pending-guard 's/\|\| Pending is not null\) return false;/) return false;/'
r M4-11-renewing-startup-spacing 's/\n\s+ClockMappingState.Renewing => 0, \/\/ the renewing exchange is taken at once//'
r M4-12-no-overlap 's/TryRetained\(reply.RequesterReceived, out retained\) && interval.Overlaps\(retained\);/TryRetained(reply.RequesterReceived, out retained);/'
r M4-13-budget-clock-not-started 's/_recoveryStarted = now.Value; \/\/ the renewal.s budget runs from the lapse/\/\/ M4-13/'
r M4-14-budget-inherited-from-recovery 's/State = ClockMappingState.Renewing;\n(\s+)ClearRecovery\(\);/State = ClockMappingState.Renewing;/'
r M4-15-conclusion-keeps-bookkeeping 's/if \(renewed\) \{ State = ClockMappingState.Ready; ClearRecovery\(\); \}/if (renewed) { State = ClockMappingState.Ready; }/'
r M4-16-width-bound-strict 's/(Expand\(fresh, RefreshSpacing\).Width \+ 2 \* TimestampUncertainty.Maximum) <= MaximumMappedWidth;/$1 < MaximumMappedWidth;/'
r M4-17-exchanges-off-by-one 's/json.WriteNumber\("exchanges", _recoverySends\);/json.WriteNumber("exchanges", _recoverySends + 1);/'
r M4-18-recovery-end-any-width 's/covered && QualifiesThroughRefresh\(fresh\)\)/covered)/'
r M4-19-retained-drift-unwidened 's/var expanded = Expand\(Interval\(_slots\[i\]\), \(long\)age\);\n(\s+)if \(!found \|\| expanded.Width < best.Width\) \{ found = true; best = expanded; \}/var expanded = Interval(_slots[i]);\n$1if (!found || expanded.Width < best.Width) { found = true; best = expanded; }/'
r M4-20-exhaustion-one-early 's/var exhausted = outcome == ClockProbeOutcome.Inconclusive && _recoverySends == RecoveryProbeLimit;/var exhausted = outcome == ClockProbeOutcome.Inconclusive && _recoverySends >= RecoveryProbeLimit - 1;/'
