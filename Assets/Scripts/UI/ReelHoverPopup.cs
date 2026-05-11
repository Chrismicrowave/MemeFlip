using TMPro;
using UnityEngine;

public class ReelHoverPopup : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI ownerText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI statusText;

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Show(Reel reel)
    {
        if (panel == null) return;

        panel.SetActive(true);
        Color c = reel.owner == Owner.Player ? reel.playerColor : reel.npcColor;
        string colorTag = $"#{(byte)(c.r * 255):X2}{(byte)(c.g * 255):X2}{(byte)(c.b * 255):X2}";

        ownerText.text = $"<color={colorTag}>{reel.owner}</color>";

        if (reel.isDestroyed)
        {
            statsText.text = "DESTROYED";
            statusText.text = "";
        }
        else if (reel.isFaceDown)
        {
            statsText.text = "HP ?  ATK ?  DEF ?";
            statusText.text = "Face Down";
        }
        else
        {
            statsText.text = $"HP {reel.stats.currentHP}/{reel.stats.maxHP}  ATK {reel.stats.atk}  DEF {reel.stats.def}";
            statusText.text = "Face Up";
        }

        // Position popup near the reel
        Vector3 screenPos = Camera.main.WorldToScreenPoint(reel.transform.position + Vector3.up * 1.5f);
        panel.transform.position = screenPos + new Vector3(0, 80, 0);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}
