using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MemeSlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    // 0 = first selected (attacker slot), 1 = second selected (target slot)
    public int slotIndex;

    public void OnPointerClick(PointerEventData eventData)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.memePlayer == null || gm.actionPanel == null) return;

        Reel reel = slotIndex == 0 ? gm.FirstSelected : gm.SecondSelected;
        if (reel == null) return;

        RawImage target = slotIndex == 0 ? gm.actionPanel.playerSlot1Image : gm.actionPanel.playerSlot2Image;
        gm.memePlayer.PlaySlot(slotIndex, target, reel, true);
    }
}
