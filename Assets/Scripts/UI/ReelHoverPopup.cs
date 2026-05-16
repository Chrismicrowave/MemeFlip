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

    public void Show(Reel reel, Vector2 screenPos)
    {
        if (panel == null || _pinned) return;

        panel.SetActive(true);
        UpdateStats(reel);

        // Show preview image only for face-up reels; hide for face-down ("Flip" text only)
        if (previewImage != null)
        {
            if (!reel.isFaceDown && !reel.isDestroyed && reel.memeData != null)
            {
                previewImage.gameObject.SetActive(true);
                if (reel.memeData.memeImage != null)
                    previewImage.texture = reel.memeData.memeImage;
                else
                    previewImage.texture = null;
            }
            else
            {
                previewImage.gameObject.SetActive(false);
            }
        }

        // Position near cursor, offset so cursor doesn't cover content
        Vector3 pos = screenPos + new Vector2(20, -20);
        // Clamp to screen bounds
        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt != null)
        {
            float w = rt.rect.width * rt.lossyScale.x;
            float h = rt.rect.height * rt.lossyScale.y;
            pos.x = Mathf.Clamp(pos.x, 0, Screen.width - w);
            pos.y = Mathf.Clamp(pos.y, h, Screen.height);
        }
        panel.transform.position = pos;
    }

    bool _pinned;
    Vector2 _pinnedPos;

    public void Pin(Reel reel, Vector2 screenPos)
    {
        _pinned = true;
        _pinnedPos = screenPos;
        if (panel == null) return;
        panel.SetActive(true);
        UpdateStats(reel);
        // Ensure preview is visible when pinned (reel is always face-up at this point)
        if (previewImage != null)
            previewImage.gameObject.SetActive(true);
        panel.transform.position = screenPos;
    }

    void UpdateStats(Reel reel)
    {
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
    }

    public void Unpin()
    {
        _pinned = false;
    }

    public void Hide()
    {
        if (_pinned) return;
        if (previewImage != null)
            previewImage.texture = null;
        if (panel != null) panel.SetActive(false);
    }
}
