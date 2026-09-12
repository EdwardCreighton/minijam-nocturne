# AGENTS.md — nocturne-gamejam

Unity 6 top-down slasher set in a nightmare-dream. Design source of truth: `Docs/Concept.md` (in Russian). Programmer spec: `Docs/TZ.md` (in Russian, must not contradict Concept). Two scenes (menu + level), no gameplay code yet.

## Stack (verified)

- Unity `6000.3.23f1` (`ProjectSettings/ProjectVersion.txt`)
- URP `17.3.0` with 2D Renderer (`Assets/Settings/UniversalRP.asset`, `Renderer2D.asset`)
- New Input System `1.20.0`, actions at `Assets/Settings/InputSystem_Actions.inputactions` (recreated in P0, GUID `2bcd...` rewired in `EditorBuildSettings.asset`) + generated C# wrapper `InputSystem_Actions.cs` (the only input API in use). Player map: `Move` / `Attack` / `Interact` (Button, NO Hold interaction — hold `0.6s` measured by code from `BalanceConfig.holdTime`) / `Pause` (`Esc`); UI map: `Navigate/Submit/Cancel/Point/Click/ScrollWheel`. No settings/exit screens in scope.
- 2D stack: `2d.sprite`, `2d.tilemap` + extras, `2d.animation`, `2d.spriteshape`, `2d.psdimporter`, `2d.aseprite`
- Target platform: browser (WebGL, `1280×720` reference, Chrome/Edge/Firefox). No `Application.Quit`, no native plugins, audio only after first click; see `Docs/TZ.md` §2.1.
- Test Framework `1.6.0` installed, no tests written yet
- Remote: `https://github.com/EdwardCreighton/minijam-nocturne.git`, branch `master`

## Project layout

- Playable scenes (2 in build, per `Docs/TZ.md`): `Assets/Scenes/MainMenu.unity` (index 0, created in P0: camera + 1280×720 Canvas + EventSystem) + `Assets/Scenes/SampleScene.unity` as `GameLevel` (index 1, rename via Editor if needed, then update `SceneLoader.GameLevel`)
- Scene template: `Assets/Settings/Scenes/URP2DSceneTemplate.unity`, `Lit2DSceneTemplate.scenetemplate`
- Pipeline/volume: `Assets/Settings/UniversalRP.asset`, `UniversalRenderPipelineGlobalSettings.asset`, `DefaultVolumeProfile.asset`
- Main character (added in `76a409c`): prefab at `Assets/Prefabs/Characters/MainCharacter.prefab` (SpriteRenderer + Animator), sprites in `Assets/Sprites/MainCharacter/`, animations in `Assets/Animations/`
- `Assets/Scripts/` (P0: `Config/BalanceConfig.cs`, `Core/SceneLoader.cs`; P1: `Core/RunState.cs` (plain class, sole economy owner), `Core/GameManager.cs` (+`GameState`, scene singleton, owns input + pause), `Core/Layers.cs` (6/7/8/9 + runtime collision matrix), `Core/FollowCam.cs`, `World/StartPoint.cs`, `World/FinishPoint.cs`, `World/Gate.cs` (data + collider toggle), `World/AttemptResetter.cs`, `UI/Hud.cs` (Points/Spent/Deaths); namespace `Nocturne.*`); no `.asmdef`. Default balance instance: `Assets/Settings/BalanceConfig.asset`. Setup guide: `Assets/SETUP.md` (update it at the end of every stage).
- `SampleScene` greybox (P1): `GameSystems`, `StartPoint`, `Gate_East (30)` / `Gate_West (50)`, `Finish_East` / `Finish_West`, `Wall_*` colliders, `HudCanvas`. Input composites fixed to `2DVector(mode=1)` (P0 `WASD`/`Arrows` paths were invalid).
- Player (P2): prefab upgraded (tag `Player`, layer 6, kinematic `Rigidbody2D` + `CapsuleCollider2D 0.6×0.8`, scripts `PlayerController/Combat/Health/Interactor/Visual` + `Combat/IDamageable`); animator typo `Attack1Upanim` fixed, animation is code-driven `Play()` (no transitions); player instance at `StartPoint`, `FollowCam.target` wired; `GameManager.OnPlayerDied` death flow (unscaled `deathDelay`, `Unspent=0`, respawn + `Snap`); enemy reset deferred to P3.
- Enemies (P3): `Enemies/Enemy.cs` (HP/damage/score, `Initialize` with difficulty mults, `AddKill` on death), `EnemyMover.cs` (chase + separation), `EnemyAttack.cs` (distance-based contact, kinematic bodies have no collision callbacks), `EnemySpawner.cs` (7 entries, `RespawnAll` on death, `origin` map); `Chaser` prefab (layer 7, red-tinted placeholder visual); `Core/MovementUtil.cs` (kinematic wall slide via `Rigidbody2D.Cast`, mandatory for all movers); `DeathRoutine` now resets enemies. One-shot setups run from the Editor menu (`Nocturne/Setup/...`) — headless batchmode hangs on licensing.
- Gates (P4): `Gate.TryOpen` (atomic spend + `Opened` event, blue→pale-green placeholder visual), `PlayerInteractor` hold logic (only spend path, resets on release/exit/damage/death/pause, debounce, nearest-gate priority), `UI/GatePrompt.cs` (cost/missing text + hold bar); both greybox gates have `SpriteRenderer` bars.

## Main character animation (from `76a409c`, "add main character")

- Single Animator Controller `Assets/Animations/MainCharacter.controller` (Base Layer only) drives the prefab's SpriteRenderer.
- 16 clips, 4-directional (Down/Left/Right/Up) × 4 states: `MainCharacter_{Idle,Run,Attack1,Attack2}{Direction}` — each is a single-frame `.anim` (no multi-frame clips).
- **No parameters and no transitions yet**: the state machine is a flat list of 16 states with no wiring. Any future locomotion/attack logic must add animator parameters + transitions (or drive states from code).
- Sprites: `Assets/Sprites/MainCharacter/{IDLE,RUN,ATTACK 1,ATTACK 2}/*.png`, imported with per-state `.meta` files (single sprite per direction, no sprite sheets/animators).

## Game design (from `Docs/Concept.md` — do not contradict)

- Top-down slasher in one large continuous location: zones differ visually but connect seamlessly, no loading screens or hard borders. Prefer a single scene; do not split zones into separate Unity scenes. Only allowed split: `MainMenu` ↔ `GameLevel` (see `Docs/TZ.md` §4).
- One fixed Start (spawn after start and after every death); several alternative Finishes — reaching any one ends the run. Different Finishes need different routes/gate costs.
- Loop: explore → kill enemies → earn points → choose route → open gates (Hold `Interact` `0.6s`, radius `1.5`, atomic `TrySpend`, progress ring) → enemy difficulty rises → explore further. Death: respawn at Start and grind through already-opened area again. Pause (`Player/Pause` on `Esc`) is mandatory: `TogglePause` + `Time.timeScale`.
- Gates (door / portal / barrier / organic — any form): each has its own point cost, player picks which to open. An opened gate stays open forever, including after death.
- Points are both currency and progress/difficulty metric. Kills grant them; opening a gate subtracts its cost from the unspent balance. Single owner: `RunState` (`Unspent`, `SpentTotal`, `OpenedGateIds`; no wallet class).
- Difficulty = total points ever **spent** on gates. It only grows, never resets — express via enemy stats, new types, group composition, or behavior. Starter defaults: `N=100, K=0.25, M=0.15, maxLevel=5` (see `Docs/TZ.md` §8.2).
- Death persistence (implement run-persistent vs attempt-local state separately):

  | State | After death |
  |---|---|
  | Position | reset to Start |
  | Unspent points | reset to 0 |
  | Spent points / difficulty | kept, never decreases |
  | Opened gates | kept open |

## How to work

- No README, no build scripts, no CI, no `npm`/`dotnet` commands. All build/test runs go through the Unity Editor (open this folder as the project).
- Play: open `SampleScene.unity` (direct level debug creates a default `RunState`); real flow starts from `MainMenu.unity`. Build target list = `EditorBuildSettings.asset` `m_Scenes`.
- Tests: Test Framework package only; run via Editor Test Runner (EditMode/PlayMode). Note `.gitignore` excludes `InitTestScene*.unity*` — PlayMode test scenes are ephemeral.
- Game-jam scope: prefer small MonoBehaviour scripts under `Assets/Scripts/`; add an `.asmdef` only if compile-time separation is actually needed.

## Unity gotchas (do not guess otherwise)

- Every new asset needs its `.meta` file committed. Never delete/rename a `.meta` alone; rename via the Editor so the GUID follows.
- Never edit or commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.sln`, `*.csproj` — all git-ignored. `nocturne-gamejam.sln` in root is local-only and stale-prone; the source of truth is `Assets/`, `Packages/`, `ProjectSettings/`.
- `Assets/*.unity` and `*.asset` files are YAML — editable by hand for small merges, but prefer the Editor/Inspector for pipeline, renderer, and volume changes.
- URP 2D lighting requires the 2D Renderer (`Renderer2D.asset`) and 2D lights; default 3D lights/shaders will not behave as expected.
- Input: use the existing `InputSystem_Actions.inputactions` asset; do not add legacy `Input.GetAxis` paths or a second actions asset without reason.
- Input maps: `MainMenu` uses the `UI` map only, `GameLevel` uses the `Player` map (`SwitchCurrentActionMap`); `Pause` lives in `Player`, `UI/Cancel` is menu-only.
