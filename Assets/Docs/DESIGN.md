# MemeFlip — Design

<!-- Stability tiers: locked (CHALLENGE always) | settled (CHALLENGE + options) | in-flux (note conflict, proceed) | TBD (ask user before implementing) -->

## Core loop  <!-- stability: locked -->

Each turn, a player flips two of their own reels face-up from a shared 3x4 board (12 positions, 6 per player in two sets). The first reel flipped enters an action state — the player chooses Attack or Defence against the second reel flipped (which has no action). Stats (HP/ATK/DEF) resolve the outcome. Play passes to the opponent. Win by reducing all 6 of the opponent's reels to 0 HP. The tension comes from remembering which reel is where (memory layer) while sequencing flips to maximise stat advantage (TCG layer).

## Game principles  <!-- stability: locked -->

- **Memory + TCG hybrid**: the board is face-down — you must remember your reel positions. But memory alone isn't enough; you also need favourable stat matchups.
- **First-flip agency**: only the first reel in a pair gets an action (Attack or Defence). The second is purely reactive — so flip order matters.
- **Reel asymmetry**: the two sets of 6 are not identical — they have different stat spreads, creating deck-building/roster choices.
- **Scene as screen**: menus, settings, and gameplay exist in the same scene. The camera rotates (not teleports) to transition between them — no additive scene loads for UI.

## Non-goals  <!-- stability: locked -->

- **No meme video/audio playback** in this milestone — reel content is static art for now. Video + music integration is Phase 2.
- **No online multiplayer** — PvP is local-pass-and-play if ever. NPC opponent only for MVP.
- **No deck-building UI** — reel sets are pre-configured for MVP. Player picks from preset lineups.
- **No animation system** beyond basic flips and stat change feedback — no character models or cutscenes.

---

## Unit behaviors

### Reel (Card)  <!-- stability: settled, last-updated: 2026-05-11 -->

- States: Face-Down → Flipping → Face-Up (Action/Reactive) → Resolving → Face-Down (end of turn) or Destroyed
- Trigger conditions:
  - Player clicks a face-down reel → Flipping state begins
  - First flipped reel in a turn → enters Action substate (player chooses Attack or Skill). Skill is a special move (healing, board-range attack, etc.) — implemented in a later milestone.
  - Second flipped reel → enters Reactive substate (no player choice); the first reel's action targets this reel
  - After action resolution → Reel returns face-down unless its HP ≤ 0 (Destroyed)
- Stats: HP (current/max), ATK (base damage), DEF (damage reduction)
- Damage formula: `damage = ATK - target.DEF`, minimum 1
- Should never: allow a player to flip more than 2 reels per turn
- Twitch interaction: does not pause — turn-based, no real-time urgency

### Player  <!-- stability: settled, last-updated: 2026-05-11 -->

- Has 6 reels assigned from a preset set, placed randomly in their half of the 3x4 board
- Has 2 **Shuffle** charges per game. On their turn, a player may shuffle all their face-down reels (randomising their positions on the board) instead of flipping. That round no flips are made — the turn passes immediately.
- Eliminated when all 6 reels are destroyed
- Turn sequence options:
  - **Flip turn**: Flip 1 (choose Attack/Skill) → Flip 2 (reactive) → Resolve → End Turn
  - **Shuffle turn**: Shuffle all face-down reels → End Turn (consumes one shuffle charge)

### NPC  <!-- stability: in-flux -->

- Same rules as player, AI-driven
- AI behaviour: prioritise matching high-ATK reels with low-DEF opponent reels, or simple random. Tune after MVP.

---

## Scene contracts  <!-- stability: in-flux -->

### MainGame (single scene, camera-rotated)

- Purpose: houses all game states — main menu, settings, and the battle board
- Load-bearing entities:
  - `Board` — 12 position markers (GridLayout), arranged 4×3 (4 rows × 3 columns)
  - `Reels` — 12 reel GameObjects, 6 Player + 6 NPC, **fully intermixed** randomly on the 4×3 grid
  - `CameraRig` — rotates around a pivot point; positions mapped for Menu, Settings, and Battle angles
  - `UI/StartScreen` — canvas group, visible at menu camera angle
  - `UI/SettingsScreen` — canvas group, visible at settings camera angle
  - `UI/GameHUD` — always-visible overlay during battle (turn indicator, player stats, action buttons)
  - `GameManager` — turn state machine, win/loss detection
  - `NPCLogic` — simple AI decision-making
- Exit conditions: none (single scene game; restart resets board state)

---

## Open questions  <!-- stability: TBD -->

- ~~**Board layout**: 3x4 means 12 positions. How are the two sets of 6 arranged?~~ **Decided**: random order per set. Each player has 2 shuffle charges per game.
- ~~**Action resolution formula**:~~ **Decided**: `damage = ATK - target.DEF`, minimum 1.
- ~~**Defence action**:~~ **Replaced**: no Defence button. First-flip action is Attack or Skill (skill implemented later).
- ~~**Player HP**:~~ **Decided**: no player HP pool — eliminated when all 6 reels are destroyed.
- **Reel sets**: how many preset lineups for MVP? Are they themed?
- **Camera rotation speed**: should transitions be instant (snap) or animated (lerp)? If animated, duration?
- **NPC difficulty**: one AI difficulty for MVP, or multiple options?
