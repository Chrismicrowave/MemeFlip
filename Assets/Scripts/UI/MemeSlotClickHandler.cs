using UnityEngine;
using UnityEngine.EventSystems;

public class MemeSlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    // 0 = first selected (attacker slot), 1 = second selected (target slot)
    public int slotIndex;

    public void OnPointerClick(PointerEventData eventData)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.memePlayer == null) return;

        Reel reel = slotIndex == 0 ? gm.FirstSelected : gm.SecondSelected;
        if (reel != null)
            gm.memePlayer.PlayFull(reel);
    }
}
