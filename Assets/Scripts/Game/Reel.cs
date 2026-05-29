using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum Owner { Player, NPC }

public class Reel : MonoBehaviour
{
    [Header("Config")]
    public Owner owner;
    public ReelStats stats;
    public ReelStatsSO statsSO;
    public MemeData memeData;

    [Header("State")]
    public bool isFaceDown = true;
    public bool isDestroyed;
    public Vector2Int boardPosition;

    [Header("Attack Animation")]
    [Tooltip("Percentage of reel width to dash right during attack (0-1)")]
    public float attackDashDistancePercent = 0.1f;
    [Tooltip("Duration in seconds for the full dash-and-return animation")]
    public float attackDashDuration = 0.3f;
    [Tooltip("How far the target shakes (world units)")]
    public float jitterIntensity = 0.05f;
    [Tooltip("How long the jitter lasts in seconds")]
    public float jitterDuration = 0.3f;
    [Tooltip("Jitter oscillation speed")]
    public float jitterSpeed = 20f;
    [Tooltip("How far the playerslot shakes (UI pixels)")]
    public float jitterSlotIntensity = 8f;

    [Header("Gradient Colour Sets")]
    [Tooltip("Gradient colour 1 during Player 1's turn.")]
    public Color colorSetA1 = new(0.3f, 0.6f, 1.0f);
    [Tooltip("Gradient colour 2 during Player 1's turn.")]
    public Color colorSetA2 = new(0.1f, 0.8f, 0.6f);
    [Tooltip("Gradient colour 1 during NPC / Player 2's turn.")]
    public Color colorSetB1 = new(0.9f, 0.3f, 0.3f);
    [Tooltip("Gradient colour 2 during NPC / Player 2's turn.")]
    public Color colorSetB2 = new(0.9f, 0.7f, 0.1f);

    [Header("Face")]
    public Texture cardBack;

    private RawImage _faceImage;
    private Image _borderImage;
    private UIGradient _borderGradient;
    private TextMeshProUGUI _flipPrompt;

    void Awake()
    {
        Transform canvas = transform.Find("Canvas");
        if (canvas != null)
        {
            Transform faceTf = canvas.Find("mask/FaceImage");
            if (faceTf != null)
                _faceImage = faceTf.GetComponent<RawImage>();

            Transform borderTf = canvas.Find("Border");
            if (borderTf != null)
            {
                _borderImage = borderTf.GetComponent<Image>();
                _borderGradient = borderTf.GetComponent<UIGradient>();
            }

            Transform flipTf = canvas.Find("FlipPrompt");
            if (flipTf != null)
                _flipPrompt = flipTf.GetComponent<TextMeshProUGUI>();
        }
    }

    public void Init(Owner owner, Vector2Int pos)
    {
        this.owner = owner;
        boardPosition = pos;
        isFaceDown = true;
        isDestroyed = false;

        int hp = Random.Range(4, 9);
        stats = new ReelStats { maxHP = hp, currentHP = hp, atk = Random.Range(2, 5) };

        ApplyVisual();
    }

    public void FlipUp()
    {
        if (!isFaceDown || isDestroyed) return;
        isFaceDown = false;
        ApplyVisual();
    }

    public void FlipDown()
    {
        if (isFaceDown || isDestroyed) return;
        isFaceDown = true;
        ApplyVisual();
    }

    public void DestroyReel()
    {
        isDestroyed = true;
        isFaceDown = false;
        if (_borderImage != null) _borderImage.enabled = false;
        ShowFlipPrompt(false);
        gameObject.SetActive(false);
    }

    public void ShowFlipPrompt(bool show)
    {
        if (_flipPrompt != null)
            _flipPrompt.gameObject.SetActive(show && isFaceDown);
    }

    void ApplyVisual()
    {
        if (isDestroyed) return;

        if (isFaceDown)
        {
            if (_faceImage != null) _faceImage.texture = cardBack;
            if (_borderImage != null) _borderImage.enabled = false;
        }
        else
        {
            if (_faceImage != null)
            {
                Texture tex = memeData?.memeImage;
                _faceImage.texture = tex ?? cardBack;
            }

            if (_borderImage != null)
            {
                _borderImage.enabled = true;
                bool isPlayerOwned = owner == Owner.Player;
                if (_borderGradient != null)
                {
                    _borderGradient.m_color1 = isPlayerOwned ? colorSetA1 : colorSetB1;
                    _borderGradient.m_color2 = isPlayerOwned ? colorSetA2 : colorSetB2;
                }
            }
        }
    }

    public void OnClick()
    {
        if (isDestroyed) return;
        GameManager.Instance.OnReelClicked(this);
    }

    public void OnHoverEnter()
    {
    }

    public void OnHoverExit()
    {
    }
}
