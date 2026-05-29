using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles clicks on slot panel MemeImages to replay the meme.
/// Resolves the correct reel for each slot by owner (not by selection order),
/// matching how ShowP1Slot/ShowP2Slot display reels.
/// </summary>
public class MemeSlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    // 0 = Slot1 (P1's slot), 1 = Slot2 (P2/NPC's slot)
    public int slotIndex;

    public void OnPointerClick(PointerEventData eventData)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.memePlayer == null || gm.actionPanel == null) return;

        // Resolve reel by slot owner (matches visual slot display from ShowSlotAfterAnimation)
        Reel reel = GetReelForSlot(gm, slotIndex);
        if (reel == null) return;

        RawImage target = slotIndex == 0 ? gm.actionPanel.playerSlot1Image : gm.actionPanel.playerSlot2Image;
        gm.memePlayer.PlaySlot(slotIndex, target, reel, true);
    }

    static Reel GetReelForSlot(GameManager gm, int slot)
    {
        var fs = gm.FirstSelected;
        var ss = gm.SecondSelected;
        // Slot1 = Player-owned reel, Slot2 = NPC-owned reel
        if (slot == 0)
            return fs != null && fs.owner == Owner.Player ? fs : (ss != null && ss.owner == Owner.Player ? ss : null);
        else
            return fs != null && fs.owner != Owner.Player ? fs : (ss != null && ss.owner != Owner.Player ? ss : null);
    }
}
