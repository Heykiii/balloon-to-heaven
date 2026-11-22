# Balloon to Heaven

A portrait (9:16) 2.5D arcade game made in **Unity 6** (6000.0.58f2, built-in render pipeline). Steer a hot air balloon by dragging your finger, dodge hazards, collect coins and climb through 10 worlds to Heaven's Gate.

- **Play in the browser:** https://heykiii.github.io/balloon-to-heaven/ (available after the first successful CI run)
- **Android APK:** [latest release](https://github.com/Heykiii/balloon-to-heaven/releases/latest)

## What's in the game

| Area | Content |
| --- | --- |
| Campaign | 60 levels in 10 worlds × 6 levels, each world ends with a boss |
| Worlds | Green Valley, Mountain Kingdom, Rainbow Skies, Thunderstorm, Frozen Heaven, Fire Kingdom, Cosmic Journey, Golden Clouds, Celestial Realm, Heaven's Gate |
| Bosses | 10 bosses with 3 or 4 phases. You hit them by collecting the crystals they drop |
| Balloons | 20 balloons, each with its own ability and 5 visual upgrade stages |
| Workshop | Durability, Coin Magnet, Movement and Shield Duration (5 levels each), plus 5 gem trails |
| Economy | Coins and Sky Gems. One-time rewards can't be claimed twice |
| Power-ups | Coin Magnet, Energy Shield, Sky Rocket, Extra Life, Double Coins |
| Stars | 3 per level: reach the top, hit the coin target, beat the level challenge |
| Other | 17 achievements, versioned and checksummed JSON save with a backup slot, settings, campaign ending |

**Fairness:** a lane planner always keeps a two-lane safe corridor open, and the corridor only moves onto lanes that are free. Timed hazards flash a warning before they turn on.

Everything is created from code: meshes, textures, UI sprites, particles, sound effects and music for each world. The project has no imported art or audio.

## Project layout

```
Assets/
  Resources/Shaders/   ToonLit, Unlit, Particle, Sky, Shield (found by name at runtime)
  Scripts/
    Core/              services, events, scene entry, pooling, tweens
    Content/           GameDatabase + ContentFactory (all 60 levels, 20 balloons, bosses...)
    Balloons/ Bosses/ Obstacles/ PowerUps/ Worlds/ Levels/ Player/ Upgrades/ Economy/
    Progression/ Saving/ Audio/ Visuals/ UI/
    Editor/            content/scene generators, project configurator, build script
  Tests/EditMode/      NUnit tests (content, save, economy, progression, shop, fairness)
```

Every scene holds a single `SceneEntry` object, and the screens are built at runtime.

## Opening in Unity

1. Install **Unity 6000.0.58f2** with the WebGL and/or Android modules (Unity Hub).
2. Open the folder in Unity Hub.
3. On first open, `AutoSetup` runs these steps for you:
   - writes the ScriptableObject assets to `Assets/ScriptableObjects` and `Assets/Resources/GameDatabase.asset`
   - creates the 7 scenes in `Assets/Scenes` and adds them to the build settings
   - applies the player settings (portrait, bundle id, WebGL compression, IL2CPP/ARM64)
4. Open `Assets/Scenes/Boot.unity` and press Play. In the Game view, use a 9:16 resolution such as 1080×1920.

You can also run each step from the menu: **Balloon to Heaven → Setup / Content / Build**. Edit levels, balloons and bosses in the generated assets. If the database asset is missing, the game builds the same content in memory.

Controls: drag with your finger or mouse, or use the arrow keys / A and D. Esc pauses the game.

## Automatic builds (GitHub Actions + GameCI)

`.github/workflows/build.yml` runs on every push to `main`:

1. **EditMode tests**
2. **WebGL build**, deployed to **GitHub Pages**
3. **Android APK**, attached to a new GitHub **Release** (also kept as a workflow artifact)

### One-time setup by the repository owner

**1. Add the Unity license secrets.** Go to **Settings → Secrets and variables → Actions → New repository secret** and add:

| Secret | Value |
| --- | --- |
| `UNITY_LICENSE` | The full contents of your `Unity_lic.ulf` file |
| `UNITY_EMAIL` | The email of your Unity account |
| `UNITY_PASSWORD` | The password of your Unity account |

To get a `Unity_lic.ulf` with a free Personal license, sign in to Unity Hub on your computer and activate a Personal license. Then open the file at:
- Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
- macOS: `/Library/Application Support/Unity/Unity_lic.ulf`
- Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`

Paste all of its text, from `<?xml` to the end, into `UNITY_LICENSE`. See the [GameCI activation guide](https://game.ci/docs/github/activation) for details.

**2. Turn on GitHub Pages.** Go to **Settings → Pages → Build and deployment → Source** and choose **GitHub Actions**.

**3. Re-run the workflow.** Open **Actions → Test, build and publish → Run workflow**. Without the secrets, the workflow stops early and shows a warning; it does not fail.

Optional release signing: by default the APK is signed with a debug key, which is fine for side-loading. To sign with your own key, set `ANDROID_KEYSTORE_PATH`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME` and `ANDROID_KEYALIAS_PASS` for the build step.

### Installing the APK

Open the latest Release on your Android phone and download `BalloonToHeaven.apk`. Open it and allow installs from your browser when asked. The phone needs Android 7.0 or newer and a 64-bit CPU.

## Building locally

From the menu, choose **Balloon to Heaven → Build → WebGL** or **Android APK**. From the command line:

```
Unity -batchmode -quit -projectPath . -buildTarget WebGL \
  -executeMethod BalloonToHeaven.EditorTools.BuildScript.BuildWebGL -customBuildPath Builds/WebGL
```

## Status

The code was written without a Unity Editor. The runtime, editor and test code compile cleanly against Unity engine and editor reference assemblies, with small stand-ins for `UnityEngine.UI`. It has **not been run inside Unity yet**, so expect some tuning on first play: difficulty, visual scale and UI layout on unusual aspect ratios. The first CI run with the license secrets is the first real compile, test run and build.
