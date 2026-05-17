# Meme Display Behavior

Three display contexts share one `VideoPlayer` (on `MemePlayer`).

## Contexts

| Context | What | Sound | slotIndex | When |
|---------|------|-------|-----------|------|
| **Hover preview** | Video (muted) or static `memeImage` | No | — | Cursor over any 3D board reel (always plays) |
| **Slot1 (P1's slot)** | Video or static `memeImage` | Yes | 0 | Valid flip: P1's attacker or P1's damage receiver |
| **Slot2 (P2/NPC's slot)** | Video or static `memeImage` | Yes | 1 | Valid flip: P2's attacker or P2's damage receiver |
| **Slot click (replay)** | Full replay (video restart + sound) | Yes | 0 or 1 | Click a slot panel that has an assigned reel |

## Valid Flip Rule (strict)

A flip is **valid** only when:
1. **First pick**: the face-down reel belongs to `_currentPlayer` (correct owner)
2. **Second pick**: the face-down reel belongs to the opponent (correct owner, opposite of first)

On **valid** flip:
- Both attacker and target display in their **owner-assigned slots** (P1→Slot1, P2/NPC→Slot2)
- Both slots play their meme **with sound** (`withSound: true`)
- The `PlaySlot` call triggers on each valid assignment after ownership is confirmed

On **invalid** flip:
- NO slot display — the reel flips face-up on the board but never appears in any slot panel
- No sound plays
- Game enters ShowResult phase with "invalid attack" message
- Clicking outside dismisses without attack resolution

## Slot Ownership Routing

| Slot | Side | Shows | AudioSource |
|------|------|-------|-------------|
| **Slot1** (`playerSlot1`) | P1 | P1's attacker, P1's damage receiver (from P2's attack) | `audioSource` (slotIndex=0) |
| **Slot2** (`playerSlot2`) | P2/NPC | P2/NPC's attacker, P2's damage receiver (from P1's attack) | `audioSource2` (slotIndex=1) |

Each reel always appears in its owner's slot regardless of whose turn it is:
- `reel.owner == Owner.Player` → Slot1 via `ShowP1Slot()` / `slotIndex=0`
- `reel.owner == Owner.NPC` → Slot2 via `ShowP2Slot()` / `slotIndex=1`

`PlaySlot(slotIndex, targetSlot, reel, withSound)` uses `slotIndex` to route audio:
- `slotIndex=0` → plays sound on `audioSource`
- `slotIndex=1` → plays sound on `audioSource2`

**Both slots play sound on a valid attack** — attacker and target each get their own audio triggered with `withSound=true`. The attacker slot is set first (during `HandleSelectFirst`), the target slot is set second (during `HandleSelectSecond` after validation passes).

## Hover Preview (always plays)

Hover preview activates on ANY reel hover — face-down or face-up, alive or destroyed:
- **Face-down reels**: Shows hover panel saying "Flip" with no preview image
- **Face-up alive reels**: Shows full hover panel with video (muted) or static image, plus HP/ATK stats
- **Destroyed reels**: Shows "DESTROYED" statut

Hover **must always attempt video playback** when the reel has a video clip and the reel is face-up. It should not be gated by `_currentVideoSlot` — if a slot is already using the VideoPlayer, fall back to static image, but never silently skip.

## Constraints

- **Hover hides on cursor exit** — panel deactivates, preview texture cleared.
- **Two slots show different content** — slot1 and slot2 each display their own assigned reel's content.
- **Hover does not interrupt slot video** — `StopHover()` checks `_currentVideoSlot`; if a slot is using the VideoPlayer, hover stops video but keeps playing slot video.
- **Slot2 video does not visually corrupt slot1** — when `PlaySlot` switches to slot2's video, slot1 is restored to its reel's `memeImage` (if available).
- **One VideoPlayer**: only the most recently `PlaySlot`-called slot gets live video. The other slot shows a static `memeImage`. If no `memeImage` exists for that reel, it shows the RawImage's background color (dark).
- **Hover plays video even when a slot is using the VideoPlayer** — fallback to static image is acceptable when the VideoPlayer is occupied, but hover should always show *something* (never silently no-op).

## Code Map

- `MemePlayer.cs` — `PlaySlot()`, `PlayHover()`, `StopHover()`, `PlaySound()`, `PlaySlotSound()`, `Stop()`
- `GameManager.cs` — hover enter/exit logic, selection `PlaySlot` calls, ownership validation
- `ReelHoverPopup.cs` — `Show()`/`Hide()` for hover panel (preview texture set by `MemePlayer.PlayHover()`)
- `MemeSlotClickHandler.cs` — slot click replays full preview via `PlaySlot()`
- `ActionPanel.cs` — sets static `memeImage` in slots alongside text, `ShowP1Slot()`/`ShowP2Slot()` owner-routed methods

## Known Conflicts (as of layout-tweaks branch)

1. **Target slot plays muted on valid attack** — `HandleSelectSecond` calls `PlaySlot(..., false)` for the target; should be `true`
2. **Invalid flips still show in slots** — `ShowP1Slot`/`ShowP2Slot` called before the ownership check; invalid flips should skip slot display entirely
3. **Hover video gated by `_currentVideoSlot`** — `PlayHover` won't play video if a slot is using the VideoPlayer; should always attempt preview (video if free, image otherwise)
