# Baseball Typing League

A baseball typing game built with Unity and C#. Type the word attached to each incoming pitch before it crosses home plate. Score enough to start a boss inning. The same project builds for Windows and WebGL.

## Gameplay

- **Fastball:** 3-4 letters, fast and straight.
- **Curveball:** 5-7 letters, follows an arc and briefly hides one letter.
- **Changeup:** tricky 6-8 letter words; starts fast and slows down.
- **Flaming Knuckleball:** 9+ letters, moves erratically, scores 500 base points, and clears every other pitch.
- Every early hit receives up to 100 distance-bonus points.
- Five consecutive typo-free hits increase the multiplier, up to 3x.
- A typo resets the combo. Backspace corrects the current input.
- **Check Swing:** press **1** or **ESC** to clear typed letters on the locked pitch.
- Three missed pitches strike you out and end the run.

When several pitches are active, the first typed letter locks onto the closest matching pitch. Complete words are case-insensitive.

## Levels and boss

There is no round timer. Difficulty rises with the level. Hitting **1,000 / 2,500 / 4,500 / 7,000** points (then +3,000) retracts the pitching machine and fires a **Juggernaut Pitch**: a huge slow ball with a health bar. Type the word stream under it. Each correct letter chips HP. Drain it for a Grand Slam and the next level. If it reaches the plate, the game ends.

## Juiced pitches

About one in ten pitches glows. Hitting one grants a power-up:

- **Rosin Bag** — chalk dust and 5 seconds of slow motion
- **Corked Bat** — 10 seconds; the last two letters of each word fill in
- **Golden Glove** — absorbs the next missed pitch
- **Double Play** — also hits 2-3 other balls on screen
- **Base Stealer** — a frenzy of 2-letter words

## Requirements

1. Install [Unity Hub](https://unity.com/download).
2. Install **Unity 6 (6000.6.2f1)** with these modules:
   - Microsoft Visual Studio Community or another C# editor
   - Windows Build Support (IL2CPP)
   - WebGL Build Support
3. In Unity Hub, choose **Add > Add project from disk** and select this folder.

Unity will generate `Library`, solution, and project files the first time it opens. Those generated files are ignored by git.

## Play in the editor

1. Open `Assets/Scenes/Main.unity`.
2. Press the Unity **Play** button.
3. Click **PLAY BALL**.
4. Type with the physical keyboard. No input box needs focus.

To add sounds, place clips at `Assets/Resources/Audio/` named `Pitch`, `Hit`, `HomeRun`, `Strike`, and `Foul` (with any Unity-supported audio extension). Missing clips are handled silently.

## Art

The pixel art comes from [Kenney](https://kenney.nl), released under [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) (public domain, no attribution required, but credit is appreciated). These packs are used:

- [Pixel Platformer](https://kenney.nl/assets/pixel-platformer) - batter, pitching machine, crowd, tiles, foliage
- [UI Pack - Pixel Adventure](https://kenney.nl/assets/ui-pack-pixel-adventure) - panels, buttons, bars, indicators
- [Sports Pack](https://kenney.nl/assets/sports-pack) - baseball and bat
- [Kenney Fonts](https://kenney.nl/assets/kenney-fonts) - KenPixel and Kenney Blocks

Only the sprites the game actually uses are committed, under `Assets/Resources/Kenney/` and `Assets/Resources/Fonts/`. To re-download and reinstall them, run **Baseball Typing > Download Kenney Art** in Unity's menu. The full packs are cached in `Tools/KenneyDownloads/`, which is ignored by git.

Textures import through `Assets/Editor/KenneyImportSettings.cs`, which forces point filtering, no compression, 18 pixels per unit, and the 9-slice borders the UI frames need. The world is drawn with `SpriteRenderer` objects under a pixel-perfect orthographic camera (320x180 reference), while text and UI sit on a scaled canvas above it.

## Build for Windows

Use **Baseball Typing > Build Windows** in Unity's menu. The output is:

```text
Builds/Windows/BaseballTypingGame.exe
```

Alternatively, use **File > Build Profiles**, select Windows, add `Assets/Scenes/Main.unity`, and build.

## Build for WebGL

Use **Baseball Typing > Build WebGL** in Unity's menu. The output is:

```text
Builds/WebGL/
```

WebGL builds must be served over HTTP; opening `index.html` directly can fail. From the repository root, run either:

```powershell
py -m http.server 8080 --directory Builds/WebGL
```

or:

```powershell
npx serve Builds/WebGL
```

Then open `http://localhost:8080`. Click the game once before typing so the browser gives it keyboard focus.

## Tests

In Unity, open **Window > General > Test Runner**, choose **EditMode**, and click **Run All**. Tests cover scoring, multiplier tiers, typo resets, pitch target selection, strikeout rules, and pitch values.

For command-line testing after Unity is installed:

```powershell
"C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe" `
  -batchmode -nographics -quit `
  -projectPath "$PWD" `
  -runTests -testPlatform EditMode `
  -testResults "TestResults.xml"
```

Adjust the editor version in the path to the version installed on your computer.

## Project layout

```text
Assets/
  Editor/                 Build commands and the Kenney download/import pipeline
  Resources/Kenney/       Curated Kenney sprites (world, props, UI)
  Resources/Fonts/        Kenney pixel fonts
  Scenes/Main.unity       Shared Windows/WebGL scene
  Scripts/                Gameplay, scoring, audio, and art lookup
  Scripts/Visuals/        Camera, stadium, characters, ball motion, pixel UI
  Tests/Editor/           Edit Mode tests
Packages/                 Unity package manifest
ProjectSettings/          Shared Unity project and build settings
Tools/KenneyDownloads/    Cached pack downloads (git ignored)
```
