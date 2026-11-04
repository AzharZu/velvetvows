# Velvet Vows isolated animation compatibility test

Tested on 2026-11-02 in Unity 6000.0.84f1. The accepted `VelvetVows_Main.unity` was not opened for editing or saved. The isolated scene is `Assets/VelvetVows/Scenes/VelvetVows_AnimationTest.unity`.

## Setup and validation

- Actual prefab instances: `Kasumi - School.prefab` as Evelyn, `Rin - School.prefab` as Celeste, and `Akio Highschool Uniform.prefab` as Adrian. Their existing avatars passed `isValid` and `isHuman` checks.
- Existing converted character materials from `Assets/VelvetVows/Materials/Characters/` are assigned only to test-scene instances. All 15 Evelyn, 16 Celeste, and 9 Adrian material slots matched; the final Game View captures have no pink materials.
- A stable camera, neutral floor, three dance pairs, and on-screen buttons/keys 1–9 are in the test scene. Root motion is disabled for controlled in-place comparison.
- The actual Play Mode test ran all nine modes. NUnit result: **1 passed, 0 failed**. It checked imported Humanoid clips, stable Celeste root after the death clip, active persistent body, and deformed mesh bottom within 3 cm below to 8 cm above the floor. The final alignment log reported bottom 0.063 m before correction and root adjustment to -0.043 m, placing the bottom approximately 0.02 m above the floor.
- Machine-readable result: `AnimationTestCaptures/PlayModeResults.xml`.
- A normal Editor Play Mode run also captured nine actual Game View images with the test UI. Additional two-phase camera frames for each dance and Begin/Loop/Stop camera frames for each pose are in `AnimationTestCaptures/`.

## Attack and death

| Role | Exact imported FBX | Clip |
| --- | --- | --- |
| Evelyn strike | `Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_R.fbx` | `HumanF@Attack1H01_R` |
| Celeste reaction | `Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatDamage01.fbx` | `HumanF@CombatDamage01` |
| Celeste death | `Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@Death01.fbx` | `HumanF@Death01` |

The tested combination is readable as a short lunge/strike, stagger, and fall with a 1.3 m starting separation. Celeste remains visible on the floor without a fade; the final pose does not drift after the clip stops. The attack clip is a one-handed combat move and looks like a sharp empty-handed strike when no prop is fitted. This is the only requested attack combination tested, so it is the best **tested** sequence, not a ranking against untested attacks.

[Attack frame](AnimationTestCaptures/00a_Attack.png) · [Reaction frame](AnimationTestCaptures/00b_Reaction.png) · [Aftermath in Game View](AnimationTestCaptures/00_AttackDeath_GameView.png)

## Dance compatibility

The six `HumanF@Dance01–06 - Loop` clips and six `HumanM@Dance01–06 - Loop` clips under `Assets/Kevin Iglesias/Human Animations/Animations/{Female,Male}/Social/Dance/Steps/` all retarget and play on the test characters. `Dance01–06` were each run on three facing pairs. Both sexes' `DancePose01` and `DancePose02` Begin, Loop, and Stop clips also played.

| Clip | Observed use |
| --- | --- |
| Dance01 | In-place casual dance; facing partners do not hold hands or travel together. |
| Dance02 | Alternating kicks/high steps; visibly unsuitable for a formal ball or close pair. |
| Dance03 | Both dancers crouch/bounce; timing matches broadly, but no coupled movement. |
| Dance04 | Arms forward; reads as separate gestures, not partner hand placement. |
| Dance05 | Best restrained background social dance of this pack; still separate solo loops. |
| Dance06 | Another usable background social dance; still separate solo loops. |
| DancePose01/02 | Begin/Loop/Stop transitions work, but raised-arm solo poses are not a ballroom hold. |

**No tested pair is a synchronized waltz.** Matching clip numbers share timing but do not create hand contact, a ballroom hold, coordinated turning, or shared travel. With root motion off the actors hold their assigned centers; the in-place steps can skate against the floor. The two sampled frames per dance and visible Game View sequence support this assessment; precise foot-contact error was not instrumented. Use Dance05/06 sparingly for independent background guests, with Dance01 as a less formal variation. Do not use these clips for Evelyn and Adrian's final paired waltz.

[Dance01 Game View](AnimationTestCaptures/01_Dance01_GameView.png) · [Dance05 Game View](AnimationTestCaptures/05_Dance05_GameView.png) · [Dance06 Game View](AnimationTestCaptures/06_Dance06_GameView.png) · [Pose01 Game View](AnimationTestCaptures/07_DancePose01_GameView.png) · [Pose02 Game View](AnimationTestCaptures/08_DancePose02_GameView.png)

## Remaining requirement

An actual coordinated couple/waltz clip set or custom authored partnered animation is still needed for the final Evelyn–Adrian dance. This test scene does not modify the main story, architecture, gameplay, or main-scene character placement.
