# Meme Display Behavior

Three display contexts share one `VideoPlayer` (on `MemePlayer`).

## Contexts

| Context | What | Sound | When |
|---------|------|-------|------|
| **Hover panel** | Video (muted) or static `memeImage` | No | Cursor over 3D board reel |
| **Player Slot 1** | Video or static `memeImage` | Yes | First reel flipped/selected |
| **Player Slot 2** | Video or static `memeImage` | Yes | Second reel flipped/selected |
| **Slot click (replay)** | Full replay (video restart or image + sound) | Yes | Click the slot panel GameObject |

## Constraints

- **Hover hides on cursor exit** — panel deactivates, preview texture cleared.
- **Two slots show different content** — slot1 and slot2 each display their own assigned reel's content.
- **Hover does not interrupt slot video** — `StopHover()` checks `_currentVideoSlot`; if a slot is using the VideoPlayer, hover stops video but keeps playing slot video.
- **Slot2 video does not visually corrupt slot1** — when `PlaySlot` switches to slot2's video, slot1 is restored to its reel's `memeImage` (if available).
- **One VideoPlayer**: only the most recently `PlaySlot`-called slot gets live video. The other slot shows a static `memeImage`. If no `memeImage` exists for that reel, it shows the RawImage's background color (dark).
- **Hover plays video** only when no slot is actively using the VideoPlayer (`_currentVideoSlot == null`).

## Code Map

- `MemePlayer.cs` — `PlaySlot()`, `PlayHover()`, `StopHover()`, `PlaySound()`, `Stop()`
- `GameManager.cs` — hover enter/exit logic, selection `PlaySlot` calls
- `ReelHoverPopup.cs` — `Show()`/`Hide()` for hover panel (preview texture set by `MemePlayer.PlayHover()`)
- `MemeSlotClickHandler.cs` — slot click replays full preview via `PlaySlot()`
- `ActionPanel.cs` — sets static `memeImage` in slots alongside text
