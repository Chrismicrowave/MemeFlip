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
        if (panel != null)
        {
            panel.SetActive(false);
            panel.GetComponent<RectTransform>().pivot = Vector2.zero;
        }
    }

    public void Show(Reel reel)
    {
        if (panel == null || _pinned) return;

        bool showFlip = reel.isFaceDown && !reel.isDestroyed;

        panel.SetActive(!showFlip);

        if (showFlip) return;

        UpdateStats(reel);

        // Preview texture is set by MemePlayer.PlayHover (handles video + image)
        if (previewImage != null)
            previewImage.gameObject.SetActive(!reel.isFaceDown && !reel.isDestroyed);

        Vector3 reelScreenPos = Camera.main.WorldToScreenPoint(reel.transform.position);
        float margin = 15f;
        float worldMargin = 0.15f;

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        bool panelFlipLeft = panelRt != null && reelScreenPos.x + margin + panelRt.rect.width > Screen.width;
        panel.transform.position = reel.transform.position + new Vector3(
            panelFlipLeft ? -worldMargin : worldMargin,
            worldMargin,
            0f
        );
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
        var gm = GameManager.Instance;
        Color c = (gm != null && gm.CurrentPlayer == Owner.Player) ? reel.colorSetA1 : reel.colorSetB1;
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
            statsText.text = $"HP {reel.stats.currentHP}/{reel.stats.maxHP}  ATK {reel.stats.atk}";
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
