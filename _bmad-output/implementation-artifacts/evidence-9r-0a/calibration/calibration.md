# Calibration 2026-10-19T14:03:05.5121660+00:00
machine: 16 logical CPUs, 68719476736 bytes; Google Chrome 155.0.8059.82; candidates 1,2,4,6,8
load average at start: 4.50; highest after a SerialAlone file alone: 5.23
Phase S alone: verdict Pass, 432.3 s
like-for-like serial baseline (Phase S + N=1 trial): 1668.8 s; historical 26 min 17 s (1577 s)

| N | wall s | pass | debt faults | peak RSS GiB | slowest file vs N=1 | rejections |
|---|---|---|---|---|---|---|
| 1 | 1236.5 | True | 0 | 3.21 | 1.00x (cat-002) |  |
| 2 | 620.3 | True | 0 | 5.95 | 1.01x (engine-core-2a3) |  |
| 4 | 322.7 | False | 1 | 11.59 | 1.18x (palette-placement) | NotAllPassed,DebtFaults |
| 6 | 232.9 | False | 0 | 16.13 | 1.20x (palette-placement) | NotAllPassed |
| 8 | 217.5 | False | 8 | 21.03 | 2.19x (cat-030) | NotAllPassed,DebtFaults,SlowerThanTwiceSerial,GainBelowTenPercent |

chosen N: 2
MeasuredWorkers.Current = new(Workers: 2, LogicalCpus: 16, MemoryBytes: 68719476736, ChromeVersion: "Google Chrome 155.0.8059.82", Date: "2026-10-19", Evidence: "_bmad-output/implementation-artifacts/evidence-9r-0a/calibration", IdleLoad: 5.23)
