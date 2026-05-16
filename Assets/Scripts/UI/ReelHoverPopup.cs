using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReelHoverPopup : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI ownerText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI statusText;
    public RawImage previewImage;

    void Start()
    {
        if (panel == null) panel = GameObject.Find("HoverCanvas/HoverPanel");
        if (ownerText == null) ownerText = GameObject.Find("HoverCanvas/HoverPanel/OwnerText")?.GetComponent<TextMeshProUGUI>();
        if (statsText == null) statsText = GameObject.Find("HoverCanvas/HoverPanel/StatsText")?.GetComponent<TextMeshProUGUI>();
        if (statusText == null) statusText = GameObject.Find("HoverCanvas/HoverPanel/FaceStatus")?.GetComponent<TextMeshProUGUI>();
        if (previewImage == null) previewImage = GameObject.Find("HoverCanvas/HoverPanel/HoverPreview")?.GetComponent<RawImage>();
        if (panel != null) panel.SetActive(false);
    }

    public void Show(Reel reel)
    {
        if (panel == null) return;

        panel.SetActive(true);
        Color c = reel.owner == Owner.Player ? reel.playerColor : reel.npcColor;
        string colorTag = $"#{(byte)(c.r * 255):X2}{(byte)(c.g * 255):X2}{(byte)(c.b * 255):X2}";

        if (reel.isDestroyed)
        {
            ownerText.text = "";
            statsText.text = "DESTROYED";
            statusText.text = "";
        }
        else if (reel.isFaceDown)
        {
            ownerText.text = "";
            statsText.text = "Flip";
            statusText.text = "";
        }
        else
        {
            ownerText.text = $"<color={colorTag}>{reel.owner}</color>";
            statsText.text = $"HP {reel.stats.currentHP}/{reel.stats.maxHP}  ATK {reel.stats.atk}  DEF {reel.stats.def}";
            statusText.text = "Face Up";
        }

        // Show meme image preview on hover
        if (previewImage != null && reel.memeData != null)
        {
            if (reel.memeData.memeImage != null)
                previewImage.texture = reel.memeData.memeImage;
            else if (reel.memeData.memeVideo != null)
                previewImage.texture = null;
        }

        // Position popup near the reel
        Vector3 screenPos = Camera.main.WorldToScreenPoint(reel.transform.position + Vector3.up * 1.5f);
        panel.transform.position = screenPos + new Vector3(0, 80, 0);
    }

    public void Hide()
    {
        if (previewImage != null)
            previewImage.texture = null;
        if (panel != null) panel.SetActive(false);
    }
}
