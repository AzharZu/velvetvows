# Velvet Vows 3.0 — Phase 2C verification

## Implementation

- Safety copy: `Assets/VelvetVows/Scenes/VelvetVows_Main_PrePhase2C.unity`. Its SHA-256 is `9dd6b468a3095c467ac856d04781e304d0aeb3190d3a85a2db07b20ca0a50990`, matching the main scene before Phase 2C installation.
- The accepted main scene now contains `PHASE 2C BLACK ROSE BALL`, ten existing academy-prefab guests, one warm point light and the `A PERFECT DUET` ending panel. No earlier architecture, gameplay coordinates, colliders, NPCs, triggers or source assets were replaced.
- Phase 2B success shows `THE NEXT EVENING`, then starts the Ball while the screen is black. The existing `THE FALLEN ROSE` failure path remains. Evelyn and Adrian are placed inside the existing Ballroom under the fade.
- Isabelle's answer branches at Reputation 5. Adrian approaches and speaks the requested dialogue. The revelation resolves Lovesick deliberately. The ending buttons reload the scene into the opening or the existing start menu.
- Evelyn and Adrian use reversible runtime material tinting on existing clothing. No compatible dress or suit mesh was found. Evelyn still has a short school-uniform silhouette; Adrian reads as a dark suit-like uniform.
- Four background guests use the imported `HumanF/M@Dance05 - Loop` and `HumanF/M@Dance06 - Loop`; four use `HumanF/M@Talk01`, two use `HumanF/M@Idle01`. The lead pair uses the actual `HumanF/M@DancePose01 - Begin/Loop/Stop`. `DancePose02` was reviewed and rejected for the lead pair because it read as unrelated solo movement. These clips do not form a true synchronized partner waltz.
- Music: `LLT_04_Pocket_Keys_Vol_2_(Free)_Loop_60bpm.wav`, a 32-second stereo loop, faded in at volume 0.19. It is used as a restrained piano bed, not identified as a waltz.

## Changed project files

- `Assets/VelvetVows/Scenes/VelvetVows_Main.unity`
- `Assets/VelvetVows/Scenes/VelvetVows_Main_PrePhase2C.unity` (new backup)
- `Assets/VelvetVows/Scripts/Phase2BCleanupManager.cs`
- `Assets/VelvetVows/Scripts/GameManager.cs`
- `Assets/VelvetVows/Scripts/GameState.cs`
- `Assets/VelvetVows/Scripts/Phase2ALovesick.cs`
- `Assets/VelvetVows/Scripts/MusicManager.cs`
- `Assets/VelvetVows/Scripts/Phase2CBallDirector.cs` (new)
- `Assets/VelvetVows/Scripts/Phase2CBallGuest.cs` (new)
- `Assets/VelvetVows/Editor/Phase2CInstaller.cs` (new)
- `Assets/VelvetVows/Tests/PlayMode/Phase2CPlayModeTests.cs` (new)

Unity generated matching `.meta` files for new assets. `VelvetVows_AnimationTest.unity` was not modified.

## Play Mode results

All tests ran in a separate project copy using the actual main scene and Unity 6000.0.84f1.

- Phase 2A: 2/2 passed, including the full player route to Phase 2B.
- Phase 2B: 6/6 passed, including four physical cleanup interactions, timer, body/blood detection, Prefect patrol and retry.
- Black Rose/Lovesick regression: 1/1 passed.
- Phase 2C: 4/4 passed: real successful cleanup opens the Ball, failure retains `THE FALLEN ROSE`, high Reputation changes Isabelle's response, and the full Ball reaches the ending with working Play Again and Main Menu.
- A graphical Play Mode pass captured seven actual Game View screenshots; these were visually inspected. No pink materials remained in the final capture.

## Game View captures

1. [Ballroom establishing](Phase2C_Captures/01_ballroom_establishing.png)
2. [Evelyn's formal palette](Phase2C_Captures/02_evelyn_formal.png)
3. [Isabelle conversation](Phase2C_Captures/03_isabelle_conversation.png)
4. [Adrian approaches](Phase2C_Captures/04_adrian_approaching.png)
5. [Dance](Phase2C_Captures/05_dance.png)
6. [Revelation](Phase2C_Captures/06_revelation.png)
7. [A Perfect Duet](Phase2C_Captures/07_a_perfect_duet.png)

## Visual limitations

- The available clothing meshes remain visibly school-style; the runtime palette does not create a true evening gown.
- Lead dance clips are independent stylized poses. The scene stages them as a short side-by-side duet, then turns both actors toward each other for the revelation. It is not synchronized couple choreography or a waltz.
- Background guest prefabs repeat some academy student models, though phase offsets and dance/talk/idle roles reduce visible repetition.
