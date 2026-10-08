# Unshipped source references

These exact source snapshots preserve the pre-rewrite, previously uncommitted declarations and consumers used by the CAT-001-I build cutover. They are excluded from the one current game compile list and Godot import/export by `.gdignore`; they are not fallback runtime, supported input or qualified GPU code. Other unported implementations remain at their existing source paths, outside the fixed compile list. Original catalogue, physical and animation acceptance remains required through [the current supersession map](../docs/work-orders/gpu-f16-current.md) and [test obligation inventory](../docs/verification/CAT-001-I/unsupported-scene-tests.md).

| Exact preserved input | SHA-256 |
| --- | --- |
| [BallPart.cs](cpu/BallPart.cs) | `29ec1c51077f09c0ef66d1eb7822adcfa80e211c785207d8b180174303fd507f` |
| [PartRegistry.cs](cpu/PartRegistry.cs) | `07600b14cbffe60e7e54d1b1752507cc32be47d94566c2f90b72143a9d93aa74` |
| [MachineWorld.cs](cpu/MachineWorld.cs) | `4705b951e81f210712967517ee23f91c70d3577154240a5b33ffd2529b0d8ebe` |
| [Workbench.cs](cpu/Workbench.cs) | `bff7a3b80748bf63b43b29dc2b717375d11fe1c4075fa3ee9db0668d53832a42` |
| [MachinePart.cs](cpu/MachinePart.cs) | `4a1e785abef249b0050d88afa4bcb00c6c5f9df8dc1338269f4714338baf74b0` |
| [WorkshopAnimation.cs](cpu/WorkshopAnimation.cs) | `8de5707dd4c146684fb9242080e2da1b32e65d9ed7b067a4bbf6567c990ec797` |

The source identities record preservation only. The current root project has no Geometry reference and no CPU physics source input. Actual shipped package absence and browser startup still require the current export; native reflection checks alone do not prove package contents.
