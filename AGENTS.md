# AGENTS.md — nocturne-gamejam

Fresh Unity 6 game-jam project. Single scene, no gameplay code yet.

## Stack (verified)

- Unity `6000.3.23f1` (`ProjectSettings/ProjectVersion.txt`)
- URP `17.3.0` with 2D Renderer (`Assets/Settings/UniversalRP.asset`, `Renderer2D.asset`)
- New Input System `1.20.0`, actions at `Assets/Settings/InputSystem_Actions.inputactions` (already wired in `EditorBuildSettings.asset`)
- 2D stack: `2d.sprite`, `2d.tilemap` + extras, `2d.animation`, `2d.spriteshape`, `2d.psdimporter`, `2d.aseprite`
- Test Framework `1.6.0` installed, no tests written yet
- Remote: `https://github.com/EdwardCreighton/minijam-nocturne.git`, branch `master`

## Project layout

- Playable scene: `Assets/Scenes/SampleScene.unity` (only scene in build list)
- Scene template: `Assets/Settings/Scenes/URP2DSceneTemplate.unity`, `Lit2DSceneTemplate.scenetemplate`
- Pipeline/volume: `Assets/Settings/UniversalRP.asset`, `UniversalRenderPipelineGlobalSettings.asset`, `DefaultVolumeProfile.asset`
- No `Assets/Scripts/`, no `.asmdef`, no `*.cs` files yet — create them as needed

## How to work

- No README, no build scripts, no CI, no `npm`/`dotnet` commands. All build/test runs go through the Unity Editor (open this folder as the project).
- Play: open `SampleScene.unity`. Build target list = `EditorBuildSettings.asset` `m_Scenes`.
- Tests: Test Framework package only; run via Editor Test Runner (EditMode/PlayMode). Note `.gitignore` excludes `InitTestScene*.unity*` — PlayMode test scenes are ephemeral.
- Game-jam scope: prefer small MonoBehaviour scripts under `Assets/Scripts/`; add an `.asmdef` only if compile-time separation is actually needed.

## Unity gotchas (do not guess otherwise)

- Every new asset needs its `.meta` file committed. Never delete/rename a `.meta` alone; rename via the Editor so the GUID follows.
- Never edit or commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.sln`, `*.csproj` — all git-ignored. `nocturne-gamejam.sln` in root is local-only and stale-prone; the source of truth is `Assets/`, `Packages/`, `ProjectSettings/`.
- `Assets/*.unity` and `*.asset` files are YAML — editable by hand for small merges, but prefer the Editor/Inspector for pipeline, renderer, and volume changes.
- URP 2D lighting requires the 2D Renderer (`Renderer2D.asset`) and 2D lights; default 3D lights/shaders will not behave as expected.
- Input: use the existing `InputSystem_Actions.inputactions` asset; do not add legacy `Input.GetAxis` paths or a second actions asset without reason.
