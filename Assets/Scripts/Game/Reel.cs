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

    public void Init(Owner owner, Vector2Int pos)
    {
        this.owner = owner;
        boardPosition = pos;
        isFaceDown = true;
        isDestroyed = false;

        if (statsSO != null)
        {
            stats = new ReelStats
            {
                maxHP = statsSO.maxHP,
                currentHP = statsSO.maxHP,
                atk = statsSO.atk,
                def = statsSO.def
            };
        }
        else
        {
            stats = new ReelStats { maxHP = 5, currentHP = 5, atk = 3, def = 2 };
        }

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
            col = faceDownColor;
        else
            col = owner == Owner.Player ? playerColor : npcColor;

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
