# Velvet Vows Phase 2 asset audit

Audited the actual imported assets in the whole `Assets` tree with Unity 6000.0.84f1 on 2026-10-31. Unity's `AssetDatabase.LoadAllAssetsAtPath` found 134 animation clips in 130 model files. The scene has valid Humanoid avatars for Evelyn (Kasumi), Celeste (Rin), Adrian (Akio), the anime girl background characters, and the Peasant Nolant caretaker. Character meshes have facial blendshapes; Peasant Nolant does not.

## Usable imported animation clips

| Exact model path | Imported clip | Rig | Recommended use |
| --- | --- | --- | --- |
| `Assets/Kevin Iglesias/Human Animations/Animations/Female/Social/Conversation/HumanF@Talk01.fbx` | `HumanF@Talk01` | Humanoid | Celeste, Isabelle, Evelyn, female students talking |
| `Assets/Kevin Iglesias/Human Animations/Animations/Male/Social/Conversation/HumanM@Talk01.fbx` | `HumanM@Talk01` | Humanoid | Adrian and male students talking |
| `Assets/Kevin Iglesias/Human Animations/Animations/Female/Idles/HumanF@Idle01.fbx` | `HumanF@Idle01` | Humanoid | Female NPC idle |
| `Assets/Kevin Iglesias/Human Animations/Animations/Female/Idles/HumanF@Idle02.fbx` | `HumanF@Idle02` | Humanoid | Alternate female idle |
| `Assets/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle01.fbx` | `HumanM@Idle01` | Humanoid | Adrian and male NPC idle |
| `Assets/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle02.fbx` | `HumanM@Idle02` | Humanoid | Alternate male idle |
| `Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Walk/HumanF@Walk01_Forward.fbx` | `HumanF@Walk01_Forward` | Humanoid | Female routes and Celeste following |
| `Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Forward.fbx` | `HumanM@Walk01_Forward` | Humanoid | Prefect and male routes |
| `Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Jump/HumanF@Fall01.fbx` | `HumanF@Fall01` | Humanoid | Possible falling motion only; **not a death pose** |
| `Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Jump/HumanM@Fall01.fbx` | `HumanM@Fall01` | Humanoid | Falling motion only |
| `Assets/Stylized NPC - Peasant Nolant/Models/Peasant Nolant(Free Version).fbx` | `metarig|Idle`, `metarig|Walk` | Humanoid | Caretaker idle/patrol |
| `Assets/Kevin Iglesias/Human Animations/Animations/Masked Poses/Human@ObjectGripHands01.fbx` | `Human@ObjectGripHands01` | Humanoid | Hand grip pose only; **not a carry animation** |

The Kevin Iglesias pack also contains female and male directional walking/running, sprinting, turning, jumping, and root-motion variants. Starter Assets provides `Idle`, `Walk_N`, `Run_N`, `JumpStart`, `JumpLand`, and `InAir`. The AnimeGirls demo includes `Armature|Idle` in `Assets/AnimeGirls/BaseCharacter/Demo files/Anim_Idle.fbx`.

## Required animation gaps

No imported clips provide sitting, reading, writing, attack, victim hit/reaction, death, crouching/cleaning, carrying/dragging, piano playing, or couple/ballroom/waltz dancing. `HumanF@Fall01` is a one-second fall loop and does not satisfy the requested visible attack or persistent dead pose. The hand-grip pose does not satisfy carrying. A final waltz cannot use an actual compatible couple animation because no such clip is installed.

## Props and textures

| Available exact asset path | Possible use |
| --- | --- |
| `Assets/VNB - Home Set/Prefabs/Bedroom/Wardrobe.prefab` | Body-hiding furniture, if scaled and placed appropriately |
| `Assets/VNB - Home Set/Prefabs/Livingroom/Drawer-Cabinet Complete 1.prefab` | Smaller storage |
| `Assets/VNB - Home Set/Prefabs/Livingroom/Drawer-Cabinet Complete 2.prefab` | Smaller storage |
| `Assets/VNB - Home Set/Prefabs/Livingroom/Drawer-Cabinet Complete 3.prefab` | Smaller storage |
| `Assets/VNB - Home Set/Prefabs/Bathroom/Towel Pile.prefab` | Cleaning cloth representation |
| `Assets/VNB - Home Set/Prefabs/Bathroom/Towel (Wall).prefab` | Utility cloth representation |
| `Assets/VNB - Home Set/Prefabs/Decoration/Book (Open).prefab` | Sketchbook representation, but it contains no Evelyn drawings |

There is no named ribbon, brooch, hair accessory, blood texture, blood decal, blood VFX, mop, or piano asset in `Assets`. The existing scene may contain authored piano geometry; this audit of imported files does not establish a piano animation or dedicated piano prefab. The book prefab does not contain the required sketchbook page art. The Gothic pack's `stained_Glass_a` textures are architectural stained glass, not blood.

## Implementation consequence

The specified visible murder requires additional Humanoid animation assets. The sketchbook reveal requires page artwork or an approved in-game presentation. A stylized blood pool and ribbon can be authored in Unity, but no matching imported asset exists. Phase 2 should not claim the full route or Game View screenshots as complete until those performances exist and the route has been exercised in Play Mode.

The accepted Gothic architecture and gameplay scene were not changed during this audit.

## 2026-11-02 update: newly supplied dance archive

`Assets/Human Dance Animations FREE.unitypackage` was placed in the project after the original audit. Its contents were not unpacked into the project at that time, so the original audit correctly found no imported dance clips. The new dance FBX files and companion shoulder-grip pose files have now been extracted with their original Unity `.meta` files, without overwriting existing assets or modifying the scene. The running Unity Editor log confirms FBX import activity.

The new files include paired male and female `HumanF@Dance01.fbx` through `HumanF@Dance06.fbx` and `HumanM@Dance01.fbx` through `HumanM@Dance06.fbx` under `Assets/Kevin Iglesias/Human Animations/Animations/{Female,Male}/Social/Dance/Steps/`. The `Poses/` directories also contain `DancePose01` and `DancePose02` Begin, Loop, and Stop FBXs for each sex. These are actual dance motions. Their suitability as a synchronized *couple waltz* still needs visual testing in Unity; matching numbers alone do not establish partner choreography. The archive does not add attack, victim reaction, or death animations.

## 2026-11-02 later update: Human Combat imported

A subsequent whole-`Assets` scan found **200 FBX files** under `Assets/Kevin Iglesias/Human Animations/Animations/`, all marked Humanoid (`animationType: 3`). The newly present combat files include `Female/Combat/1H/HumanF@Attack1H01_R.fbx` (clip `HumanF@Attack1H01_R`), `Female/Combat/HumanF@CombatDamage01.fbx` (clip `HumanF@CombatDamage01`), and `Female/Combat/HumanF@Death01.fbx` (clip `HumanF@Death01`). Equivalent male clips exist. The Unity Editor log confirms import of these files. These resolve the earlier *asset availability* gap for a stylized attack, victim reaction, and death. They have not yet been visually retargeted, staged, or Play Mode tested on Evelyn and Celeste. No sitting, reading/writing, cleaning/crouching, carrying/dragging, or piano clip was found. Dance clips exist, but synchronized couple-waltz suitability remains unverified.
