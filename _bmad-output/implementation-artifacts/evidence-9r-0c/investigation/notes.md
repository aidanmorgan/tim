Balloon/Tennis investigation notes (19 Oct 2026, read-only)

Art: parts/catalog/balloon.tres:4,12 + tennis.tres:4,12 -> parts/scenes/ball.tscn (hardcoded orange Mat_Ball + stripe torus, r=0.34).
  No Ball material sub-resource. MachinePart.BuildDeclarative:239 sizes only Basketball/Bowling; Definition.Color only read at MachinePart.cs:330-337 (switch).
  Pre-6.5d BallPart tinted with Definition.Color (git show 6e66118e:parts/BallPart.cs).
Admission: 8f682f84 (6.5d) deleted Parameters lines from balloon/tennis.tres -> PartRegistry.cs:30-31/37-38 legacy skip no longer fires.
  -> registered -> GenerateFree (PartRegistry.cs:57, WorkshopInventory.cs:79-86) -> palette.
Fault: Workshop.cs:521 async void Click -> MachineWorld.CaptureInstance -> MachineWorld.Gpu.cs:138 default arm throws
  ArgumentException "Unsupported instance part id 'balloon'." Outside SubmitConstruction try (WorkshopGpu.cs:46-66). PushUndo already ran (Workshop.cs:515).
  Contrast CommitSelected WorkshopGpu.cs:73-85 catches. Preview (ChooseTool->CreatePreview) does not throw: BuildDeclarative falls through.
Set diff catalog ids vs CaptureInstance arms: only balloon, tennis.
Downstream also unsupported: BallMaterial.For WorkshopConstruction.cs:59-64; WorkbenchFootprint.Of WorkbenchCapacity.cs:55; wire decode WorkshopWire.cs:402.
Native: dotnet test --filter PartRegistryTests|ActualResourceAndSceneUseCanonicalMaterialBits -> 6/6 pass, asserting 24 parts incl Tennis/Balloon.
Planning: epics.md:124, 1895-1931 Story 9A.14 already removes Tennis/Balloon from palette (decision 18 Oct); 9a-14 backlog behind 9.4b, 9R, 9.4c, 9A.0-9A.13.
