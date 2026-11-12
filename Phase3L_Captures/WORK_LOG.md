# Phase 3L implementation status

The user's full animation plan is being implemented in the existing Unity project. Do not stop at a partial handoff: remaining delivery requires final BURN and LEAVE recordings plus native macOS build/smoke verification.

## Implemented

- Five acoustic vowel weights in CharacterVoiceManager; multi-shape face expressions in AcademyCharacterExpression.
- AcademyBodyPerformance: bounded breathing/eye/secondary motion, calibrated foot grounding, two-bone hands/feet, palm contact and relaxed fingers. Death disables procedural performance through the existing presence component.
- AcademyActionPerformance: all eight ActionKind values, actual contact callback, visible carried item meshes, receiver hand targets, optional physical approach, cancellation and input restoration. Story and cleanup actions are connected.
- Existing locomotion controllers cloned under Animations/Phase3L with timing calibrated to movement speed. Idle selection and in-place turn clips configured by the editor pass.
- Spatial footsteps use contact/events with a distance fallback; existing rug/occlusion mix remains.
- Visible corpse movement into wardrobe and wardrobe door animation; visible blood cleaning and uniform washing.
- Synchronized dance clips and hand contact; prefect pauses/scans during approach.
- Murder camera now searches collision-free shots inside Studio. The full pre-fix recording exposed a black shot; do not deliver that pre-fix video as the final version.

## Validation and remaining work

- Earlier functional suite `validation_results.xml`: 9/9 passed, before final visual refinements.
- `final_contact_results.xml`: 2/2 passed, before subsequent camera/foot calibration refinements.
- `ready_results.xml`: 5/6 passed; only the synthetic step assertion failed because it sampled before LateUpdate. The test now samples at EndOfFrame. Production IK was already applying in LateUpdate.
- `final_checks_results.xml`: 6/6 passed. Includes all actions and cancellation, carry meshes, physical cleanup, acoustic lips/music/steps, exact reachable hand IK, airborne release, small raised step (sampled after LateUpdate), synchronized palm contact (~3.5 cm), visible murder camera near the south wall (central image variance ~0.012).
- Native macOS build succeeded with 0 errors, 1,007,412,388 bytes. App is universal x86_64+arm64. Native smoke succeeded (Valid=True, Errors=0, OSXPlayer) and screenshot was inspected; real opening story dialogue renders correctly. Artifacts: `Builds/Phase3L/VelvetVows.app`, `standalone_smoke.txt/png`, `standalone.log`.
- Extra locomotion verification found a real stock-grounding bug: the Default-layer capsule was counted as ground throughout a jump. Added `AcademyGrounding` on player, order -50, with downward support sphere casts excluding self/character controllers and wall normals. It runs before movement and before visible animation; skips disabled movement so cinematics/tests retain ownership.
- `grounding_motion_results.xml`: physical sprint/jump and existing library/wall/jump regression passed. Sprint 5.6 m/s, jump rise ~0.99 m, grounded false in air, Jump animation true, foot IK 0 in air and restored on landing.
- Final native rebuild succeeded (0 errors, 1,007,414,274 bytes). Final native smoke again passed Valid=True, Errors=0 and verifies AcademyGrounding exists. Final zip packaging is finishing under `Builds/Phase3L/VelvetVows-macOS.zip`; previous archive remains `_PreGrounding.zip` and is not delivery.
- ACTIVE WORKFLOW: sequential shell command runs final BURN → finalize_recording.py burn → final LEAVE → finalize_recording.py leave. A failure stops the chain. Monitor `Playthrough_Recording/phase3l_burn/progress.txt` first, then `phase3l_leave/progress.txt`. No Assets or production edits during recordings. After both, inspect proofs (murder must not be blank), confirm both XMLs Passed and decode logs empty, inspect final zip, and write the final report and handoff.
- Pre-camera-fix full BURN passed gameplay with 0 Unity errors, 621 seconds; it is retained under `Playthrough_Recording/phase3l_burn_pre_camera_fix`, `full_burn_pre_camera_fix_results.xml`, and `VelvetVows_BURN_PreCameraFix.mp4`. Main output names have been freed for the final re-recording.
- Final BURN command: set `VELVET_RECORDING_RUN=phase3l_burn`; run test `VelvetVows.Tests.RecordedPlaythroughTests.FullGameFromBeginToPerfectDuet`; results `Phase3L_Captures/full_burn_results.xml`; log `full_burn.log`.
- Final LEAVE command: set `VELVET_RECORDING_RUN=phase3l_leave`; run test `VelvetVows.Tests.RecordedPlaythroughTests.FullGameFromBeginToHeartbroken`; results `Phase3L_Captures/full_leave_results.xml`; log `full_leave.log`.
- Both record at normal time scale using actual controller movement, E and EventSystem clicks. Do not edit Assets during recording. Each takes roughly 10 minutes. Check progress.txt and timeline.tsv in its raw capture folder; provide user updates at most 60 seconds apart.
- Finalize each with `python3 Phase3L_Captures/finalize_recording.py burn` or `leave`; it asserts Passed, muxes audio, fully decodes, and extracts proofs. Inspect murder, dance and ending screenshots. The old pre-fix murder proof was uniformly dark.
- Build through Unity batch `-executeMethod VelvetVows.Editor.Phase3LAnimationPass.BuildMac -quit`. Outputs `Builds/Phase3L/VelvetVows.app`, and build_summary.txt. Explicitly builds the main scene only, not the legacy SampleScene.
- Launch built executable with `-velvet-smoke /Users/sanzar/VelvetVows/Phase3L_Captures/standalone_smoke.txt -logFile .../standalone.log -screen-fullscreen 0 -screen-width 1280 -screen-height 720`. Diagnostic flag starts the real first story scene, captures screenshot, reports scene/valid/errors and exits. It never runs in a normal launch.
- Finish with report, final videos, and macOS app link. Native smoke is not a full native playthrough: describe verification accurately.

## Project and tools

Unity: `/Applications/Unity/Hub/Editor/6000.0.84f1/Unity.app/Contents/MacOS/Unity`.
Main scene: `Assets/VelvetVows/Scenes/VelvetVows_Main.unity`.
Backup: `VelvetVows_Main_PrePhase3L.unity`.
Apply pass: `VelvetVows.Editor.Phase3LAnimationPass.Apply`; already applied, rerun only if serialized configuration changes.
No delegation authorized. Preserve existing worktree changes and older captures. Do not create a goal unless explicitly requested.
