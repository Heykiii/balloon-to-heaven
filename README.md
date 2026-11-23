# Balloon to Heaven

A portrait (9:16) 2.5D arcade game made with **Godot 4.5**. Steer a hot-air balloon upward through
10 sky worlds, dodge hazards, collect coins and beat a boss at the end of every world, all the way to
Heaven's Gate.

- **Play in the browser:** https://heykiii.github.io/balloon-to-heaven/
- **Android APK:** https://github.com/Heykiii/balloon-to-heaven/releases/latest

Both are built automatically by GitHub Actions on every push to `main`. No secrets or accounts are needed.

## One-time setup (repository owner)

Open **Settings → Pages → Build and deployment** and set **Source** to **GitHub Actions**.
After that, each push to `main` runs the tests, publishes the web build to Pages and attaches a fresh
APK to a GitHub Release named `build-N`.

## Controls

- **Touch / mouse:** press and drag anywhere to move the balloon left and right.
- **Keyboard:** A/D or the arrow keys. Esc (or Android Back) pauses or goes back.
- Balloons with an active ability show a button in the bottom-right corner of the HUD.

## Content

- 60 levels in 10 worlds (Green Valley to Heaven's Gate), each world ending in a boss with several phases.
- 20 balloons, each with its own ability and 5 upgrade stages (stats never go down between stages).
- Workshop: 4 permanent upgrade tracks plus unlockable trails.
- Coins and Sky Gems, 5 power-ups (Energy Shield, Coin Magnet, Sky Rocket, Extra Life, Double Coins).
- 3 stars per level: reach the top, collect enough coins, and complete the level challenge.
- Achievements, settings (music, SFX, screen shake, quality) and a validated JSON save with a backup copy.
- All sound effects and music are synthesised at start-up, so the game ships no audio files.

**Fairness:** a lane planner keeps a safe corridor open at all times. Every spawn, full-width row and
boss attack leaves a gap the balloon can reach (covered by automated tests).

## Running locally

1. Install [Godot 4.5.1](https://godotengine.org/download) (standard build, not .NET).
2. Open `project.godot` in the editor and press **Play** (F5).

### Tests

```sh
godot --headless --path . -s res://tests/run_tests.gd        # logic tests
godot --headless --path . -s res://tests/check_scripts.gd    # every script compiles
godot --headless --path . --fixed-fps 60 -s res://tests/smoke_test.gd   # autopilot plays 14 levels
```

### Exports

```sh
godot --headless --path . --export-release "Web" build/web/index.html
godot --headless --path . --export-debug "Android" build/android/balloon-to-heaven.apk   # needs Android SDK + Java 17
```

## Project layout

```
scripts/core       autoloads (Game, Events), screen host, constants, helpers
scripts/data       all content: worlds, levels, balloons, bosses, obstacles
scripts/systems    save, economy, progression, shop, workshop, achievements, lane planner
scripts/gameplay   balloon, health, abilities, obstacles, coins, power-ups, environment effects, bosses
scripts/visual     procedural 3D models, sky environments, particles, camera shake
scripts/audio      synthesised SFX and music sequencer
scripts/ui         UI kit, icons, HUD, dialogs
scripts/screens    boot, menu, map, shop, workshop, play, results, ending
tests/             logic, compile, smoke and screenshot scripts
```

The earlier Unity version is kept on the `unity-version` branch.
