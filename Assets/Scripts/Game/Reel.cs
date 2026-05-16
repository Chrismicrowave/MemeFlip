using UnityEngine;

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
    public Color faceDownColor = new(0.5f, 0.5f, 0.5f);

    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    public void Init(Owner owner, Vector2Int pos)
    {
        this.owner = owner;
        boardPosition = pos;
        isFaceDown = true;
        isDestroyed = false;

        // All reels start at 5/5 HP — override any SO values
        stats = new ReelStats { maxHP = 5, currentHP = 5, atk = 3 };

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
        gameObject.SetActive(false);
    }

    void ApplyVisual()
    {
        Color col = isFaceDown ? faceDownColor : (owner == Owner.Player ? playerColor : npcColor);
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(BaseColor, col);
        _renderer.SetPropertyBlock(_propBlock);
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
