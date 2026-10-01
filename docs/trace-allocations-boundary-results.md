# Trace reader boundary checks

1 October 2026: two initial Release invocations failed with exit 1 because only the Debug tool binary existed. No report was produced. The Release build was then run successfully (trace-allocations-release-validation.log).

Invoking the built Release tool without arguments rejects with ArgumentException: Provide one trace path (exit 134). This is the expected explicit rejection. Malformed/lost-event trace guards have not been fixture-tested; successful retained-trace reproduction is separately recorded.
