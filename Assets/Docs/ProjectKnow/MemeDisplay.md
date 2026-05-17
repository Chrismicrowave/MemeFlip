# Meme Display Behavior

Three display contexts share one `VideoPlayer` (on `MemePlayer`).

## Contexts

| Context | What | Sound | slotIndex | When |
|---------|------|-------|-----------|------|
| **Hover panel** | Video (muted) or static `memeImage` | No | — | Cursor over 3D board reel |
| **Slot1 (P1)** | Video or static `memeImage` | Yes | 0 | P1's attacker or P1's damage receiver |
| **Slot2 (P2)** | Video or static `memeImage` | Yes | 1 | P2's attacker or P2's damage receiver |
| **Slot click (replay)** | Full replay (video restart + sound) | Yes | 0 or 1 | Click the slot panel GameObject |

## Slot Ownership Routing

| Slot | Side | Shows | AudioSource |
|------|------|-------|-------------|
| **Slot1** (playerSlot1) | P1 | P1's attacker, P1's damage receiver (from P2's attack) | `audioSource` |
| **Slot2** (playerSlot2) | P2 | P2's attacker, P2's damage receiver (from P1's attack) | `audioSource2` |

Each reel always appears in its owner's slot regardless of whose turn it is:
- `reel.owner == Owner.Player` → Slot1 via `ShowP1Slot()` / `slotIndex=0`
- `reel.owner == Owner.NPC` → Slot2 via `ShowP2Slot()` / `slotIndex=1`

`PlaySlot(slotIndex, targetSlot, reel, withSound)` uses `slotIndex` to route audio:
- `slotIndex=0` → plays sound on `audioSource`
- `slotIndex=1` → plays sound on `audioSource2`

This means during NPC's turn, the NPC's attacker (Owner.NPC, Slot2) and Player's target (Owner.Player, Slot1) swap which slot they occupy compared to P1's turn.

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
