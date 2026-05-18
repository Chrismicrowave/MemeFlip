# Game Rules

## Board

- 3x4 grid (12 reels total)
- 6 reels per player, assigned randomly each game
- Memes dealt randomly from the `MemeLibrary`
- Reel positions on the grid are shuffled at game start (not in player order)

## Per-Round Turn Flow

Each player gets 1 round with exactly **2 picks**:

### Pick 1 — Attacker Selection

Flip a face-down reel. This counts as your first pick regardless of outcome.

| Pick result | Behavior |
|---|---|
| **Own reel** (valid) | Chosen as attacker. Flies to owner's slot panel. Advances to Pick 2. |
| **Opponent's reel** (invalid) | Revealed on the board for info. Tracked as invalid attacker. Advances to Pick 2. |

### Pick 2 — Target Selection (or info)

Flip any other face-down reel. Animates into the target's slot panel for display.

| After Pick 1 | Pick 2 result | What happens |
|---|---|---|
| Valid attacker (own reel) | Opponent's reel | Attack resolves — dash animation, damage dealt (HP -= ATK, min 1). Target destroyed if HP ≤ 0. |
| Valid attacker (own reel) | Wrong owner | "Invalid attack" message. ShowResult. Click away → player gets to retry (stays same phase). |
| Invalid attacker (opponent's reel) | Any reel | "No valid attacker" message. ShowResult. Click away → turn ends, opponent starts. |

### Click Away

During `ShowResult`, clicking anywhere outside the board dismisses and advances the turn. This is the standard "continue" action.

### Shuffle

- 2 charges per player per game
- Costs 1 charge to use
- Flips down all alive face-down reels and randomizes their positions
- In **VsNPC mode**: shuffle ends your turn immediately → NPC turn
- In **VsPlayer mode**: shuffle passes turn to the other player
- Shuffle is only available in `PlayerSelectFirst` phase (before Pick 1 resolves)

## Attack Resolution

- Damage = `Mathf.Max(1, attacker.atk)` — always at least 1
- Target's `currentHP` is reduced by damage
- If target HP reaches 0, reel is destroyed (can't be flipped or selected again)
- After attack, `ShowResult` phase — click away to continue

## Win Condition

Eliminate all 6 of the opponent's reels. Game over screen with the winner.

## Modes

| Mode | Players | Notes |
|---|---|---|
| VsNPC | Player vs AI | NPC picks randomly from its alive face-down reels. 0.8s delay between actions. |
| VsPlayer | 2 players hot-seat | No AI — both players use the same screen. Turn prompts show "Player 1"/"Player 2". |

## Invalid Picks (Info-Gathering)

Picking an opponent's reel as attacker reveals its stats for information. This is a strategic mechanic — you might sacrifice your turn to scout the opponent's board. The revealed reel flips back down at turn end along with all other selections.
