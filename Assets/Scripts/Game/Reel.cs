using UnityEngine;

public enum Owner { Player, NPC }

public class Reel : MonoBehaviour
{
    [Header("Config")]
    public Owner owner;
    public ReelStats stats;

    [Header("State")]
    public bool isFaceDown = true;
    public bool isDestroyed;
    public Vector2Int boardPosition;

    [Header("Colors")]
    public Color playerColor = new(0.2f, 0.33f, 1f);
    public Color npcColor = new(1f, 0.2f, 0.33f);
    public Color faceDownColor = new(0.5f, 0.5f, 0.5f);
    public Color destroyedColor = new(0.3f, 0.3f, 0.3f);

    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    public void Init(Owner owner, ReelStats stats, Vector2Int pos)
    {
        this.owner = owner;
        this.stats = stats.Clone();
        boardPosition = pos;
        isFaceDown = true;
        isDestroyed = false;
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
        ApplyVisual();
    }

    void ApplyVisual()
    {
        Color col;
        if (isDestroyed)
            col = destroyedColor;
        else if (isFaceDown)
            col = faceDownColor; // neutral grey — all reels look the same
        else
            col = owner == Owner.Player ? playerColor : npcColor; // reveal owner on flip

        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(BaseColor, col);
        _renderer.SetPropertyBlock(_propBlock);
    }

    void OnMouseDown()
    {
        if (isDestroyed) return;
        GameManager.Instance.OnReelClicked(this);
    }

    void OnMouseEnter()
    {
        if (isDestroyed) return;
        GameManager.Instance.hoverPopup.Show(this);
    }

    void OnMouseExit()
    {
        GameManager.Instance.hoverPopup.Hide();
    }
}
