# Meme Playing Feature — Implementation Plan

## Overview

Play video/image+sound memes when the player hovers or clicks on reels/slot panels. Uses a dedicated `MemeDisplay` UI area with a `MemePlayer` component that handles both video (`VideoPlayer`) and image+sound (`AudioSource`) playback.

**Meme assignment:** Since reels are randomized each game, memes are pulled from a `MemeLibrary` ScriptableObject pool and randomly assigned during `Board.Initialize()`.

## Architecture

```
MemeLibrary (SO — list of MemeData entries)
     │
     ▼  (randomly assigned in Board.Initialize)
Reel.memeData ──► MemeDisplay/MemePlayer
                       ├─ VideoPlayer → RenderTexture → RawImage
                       └─ AudioSource (for image+sound or video audio)

Reel.OnHoverEnter()  ──► MemePlayer.PlayMuted(this)
Reel.OnHoverExit()   ──► MemePlayer.Stop()
Reel.OnClick()       ──► MemePlayer.PlayFull(this)
SlotPanel click      ──► MemePlayer.PlayFull(currentSlotReel)
```

- **Hover**: muted video preview (or static image for image memes)
- **Click**: video restarts from 0 with audio (or image + sound plays)
- **Exit hover**: stop preview

---

## Step-by-step

### Step 1 — Create MemeData + MemeLibrary + update Reel.cs
- `MemeData.cs` — Serializable class (VideoClip, Texture2D, AudioClip)
- `MemeLibrary.cs` — ScriptableObject (list of MemeData)
- Add `MemeData` field to `Reel.cs`
- Add `MemeLibrary` reference to `Board.cs`
- `Board.Initialize()` assigns random MemeData to each Reel

### Step 2 — Create MemePlayer.cs component
- Wraps `VideoPlayer` + `AudioSource` targeting a `RenderTexture` → `RawImage`
- `PlayMuted(Reel)` — mute preview for hover
- `PlayFull(Reel)` — unmuted restart for click
- `Stop()` — clear display, stop everything
- Handles both video memes (play VideoPlayer) and image memes (set RawImage texture + play AudioSource)

### Step 3 — Create MemeDisplay UI + assets in scene
- Create RenderTexture asset for video output
- Add `MemeDisplay` GameObject under `GameCanvas` with `RawImage`
- Add `MemePlayer` component (with VideoPlayer + AudioSource)
- Position appropriately on screen
- Assign MemeLibrary asset (populated with meme entries)

### Step 4 — Wire hover (muted preview)
- In `GameManager.Update()` hover logic → call `MemePlayer.PlayMuted(reel)` / `Stop()`

### Step 5 — Wire click (full playback)
- In `Reel.OnClick()` → after game logic, call `MemePlayer.PlayFull(this)`

### Step 6 — Wire slot panel clicks
- Add click handlers to PlayerSlotPanel and NPCSlotPanel
- On click, play meme of the reel currently shown in that slot

### Step 7 — Populate MemeLibrary with assets
- Create MemeLibrary ScriptableObject asset
- Fill from meme.mp4, .png, and .mp3 files

---

## Files Changed

| File | Action |
|------|--------|
| `Assets/Scripts/Game/MemeData.cs` | **New** — serializable meme entry |
| `Assets/Scripts/Game/MemeLibrary.cs` | **New** — ScriptableObject pool |
| `Assets/Scripts/Game/Reel.cs` | Edit — add MemeData field |
| `Assets/Scripts/Game/Board.cs` | Edit — random meme assignment in Init |
| `Assets/Scripts/UI/MemePlayer.cs` | **New** — core playback component |
| `Assets/MainGame.unity` | Edit — add MemeDisplay, wire references |
| `Assets/Scripts/Game/GameManager.cs` | Edit — wire meme playback |
| `Assets/Scripts/UI/ActionPanel.cs` | Edit — slot panel click handlers |

## Asset Dependencies

- `Assets/Memes/MemeRenderTexture.renderTexture` — for VideoPlayer output
- `Assets/Settings/MemeLibrary.asset` — meme pool populated with clips

---

## Checklist

- [ ] Step 1: MemeData + MemeLibrary + Reel/Board changes
- [ ] Step 2: Create MemePlayer.cs
- [ ] Step 3: Create MemeDisplay UI + RenderTexture + MemeLibrary asset in scene
- [ ] Step 4: Wire hover → muted preview
- [ ] Step 5: Wire click → full playback
- [ ] Step 6: Wire slot panel clicks
- [ ] Step 7: Populate MemeLibrary with assets
- [ ] Test: hover shows muted preview
- [ ] Test: click plays with sound
- [ ] Test: slot panel click triggers replay
- [ ] Test: image-only meme (static + sound) works
- [ ] Commit and push branch
