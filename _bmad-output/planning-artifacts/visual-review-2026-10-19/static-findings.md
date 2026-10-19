# Static visual review (19 Oct 2026) — read-only

Godot 4.7.2 headless check (scratch gd/t.gd): tscn Transform3D(0,-1,0,1,0,0,0,0,1) maps mesh +Y -> (-1,0,0); cull_mode 0 == CULL_BACK (DISABLED=2); bend segment basis det = -1.

1. Funnel taper reversed: parts/scenes/funnel.tscn Mesh_Shell top_radius 0.7 / bottom_radius 1.35 (L13-21) + Visual_Shell transform (L68) puts the 0.7 end at x=-0.9 (wide inlet, collar inner 1.3) and 1.35 end at x=+0.9 (outlet, collar 0.65). Colliders (WorkshopCanonicalColliders.cs:873-) wide at -X. Fix: swap radii (top 1.35, bottom 0.7) or transform Transform3D(0,1,0,-1,0,0,0,0,1,...).
2. Glass single-sided: Mat_Shell cull_mode = 0 (CULL_BACK) in funnel/pipe/pipe_bend_45/pipe_bend_90; Mat_Sleeve powered_gate. Spec: double-sided (CAT-030 §2 Art). Fix cull_mode = 2 (+ ideally depth_draw/sorting care).
3. Navy rails missing on pipe, bends, funnel (DESIGN.md 349/353/354).
4. Funnel shell 32 sides vs spec 48 (minor).
5. Bend shell segments use reflection bases (det -1), pipe_bend_45/90 Visual_Shell_Seg_*; also bends are 4/8 discrete cylinders, gaps/overlaps at joints.
6. Bumper pedestal Visual_Base (r .3, y -.46) fully inside head sphere (r .65) -> invisible.
7. Counter sockets: Mat_Socket = housing colour #e8b764 (not gold); ActivationOut at y -0.72 buried inside Foot; PowerIn/Supply float 0.03 off housing.
8. Ball detector chevron vertex at -X (points upstream); forward crossing is +X; icon arrow points right.
9. Powered gate indicator and pressure-plate studs have no state binding (MachinePart.BuildDeclarative only duplicates material).
10. Delay: box housing, no twelve marks (DESIGN: round ochre housing + twelve marks).
11. Switch icon = toggle lever; model = round push button. Switch socket #ffd899 not gold.
12. Socket colour inconsistency: #e8b764 (delay, gate, plate, counter) vs gold #f7cb52 (clock, latch, detector) vs #ffd899 (switch).
13. Clock/Counter housing #e8b764 (link colour) vs DESIGN "ochre".
14. Springboard coil = 4 flat tori, not a continuous helix; floats 0.0375 off base and plate.
15. Wall stripes protrude only 0.001 in z (body z 0.998 vs stripe 1.0) -> z-fight risk, worse with non-uniform Visual.Scale.
16. Ball detector / powered gate collars: zero-thickness open cylinders, back-face culled (not "thick").

## Chrome confirmation (preview :8060, actual palette -> bench placement, default camera + wheel zoom x3 + right-drag orbit)
- funnel-zoom.png: cone narrow inside the WIDE collar, flaring past the narrow collar -> taper reversed (confirmed). Only near half of glass visible.
- pipe-zoom.png: only near half of the shell; no navy rails.
- pipe_bend_90-zoom.png / pipe_bend_45-v1.png: visible darker seam bands where the discrete cylinder segments overlap.
- ball_detector-zoom.png / -v1.png: cream collar renders as a half-arc "C" (open zero-thickness cylinder, back faces culled).
- powered_gate-zoom.png: collars render as half-arcs; blade bottom pokes below the sleeve.
- bumper-zoom.png: no slate pedestal visible.
- counter-zoom.png: sockets housing-coloured; top socket reads as a nick.
- wall/ramp/receiver: lit faces clip to pale yellow/mint (global lighting, DESIGN values ambient .65 + key 1.3 linear).

## Additions after advisor review
- Undriven state cues: UpdateClockLamp(true)/UpdateCounterLamps(n>0) called only from tests; production only calls them with false/0 (MachinePart.cs:220-221). Gate Visual_Indicator and plate Stud_* have no binding. Need Run-state captures.
- Port data: scene Loc_Port_* drive wiring (PartDefinition.EnsureExtracted -> GltfPhysicsBodyExtractor), but elements/*/…Declaration.cs ports disagree (BallDetector (0,.4,0) vs scene (0,1.12,0); PoweredGate power_in (0,-.7,0) vs (0,1.65,.92); Counter, PressurePlate, Battery, Bumper, Domino, SignalLamp likewise).
- Mouth locators: pipe Loc_Port_mouth_* at collar centre ±1.8 with identity basis (start faces inward +X); bends start identity (inward); funnel/bends offset 0.09 outward. Inconsistent.
- Bend det -1: demoted (Godot flips cull for mirrored instances). Real bend defect: straight 0.52 cylinders on a 2.4 m arc overlap inside / gap outside -> seam bands.
