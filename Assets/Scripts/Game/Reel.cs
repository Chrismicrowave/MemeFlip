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

    [Header("Colors")]
    public Color playerColor = new(0.2f, 0.33f, 1f);
    public Color npcColor = new(1f, 0.2f, 0.33f);

    [Header("Face")]
    public Texture cardBack;

    private RawImage _faceImage;
    private Image _borderImage;

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
                _borderImage = borderTf.GetComponent<Image>();
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
        gameObject.SetActive(false);
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
                _borderImage.color = owner == Owner.Player ? playerColor : npcColor;
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
