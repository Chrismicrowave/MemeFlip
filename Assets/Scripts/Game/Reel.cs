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

    [Header("Face")]
    public Texture cardBack;

    private Renderer _faceRenderer;
    private MaterialPropertyBlock _propBlock;
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private UnityEngine.UI.Image _borderImage;

    void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
        var faceTf = transform.Find("Face");
        if (faceTf != null)
            _faceRenderer = faceTf.GetComponent<Renderer>();

        var borderTf = transform.Find("Canvas/Border");
        if (borderTf != null)
            _borderImage = borderTf.GetComponent<UnityEngine.UI.Image>();
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
        if (_faceRenderer == null || isDestroyed) return;

        if (isFaceDown)
        {
            // material already has card-back as default — leave it
            _faceRenderer.SetPropertyBlock(null);
            if (_borderImage != null) _borderImage.enabled = false;
        }
        else
        {
            _faceRenderer.GetPropertyBlock(_propBlock);
            Texture tex = memeData?.memeImage;
            if (tex != null)
                _propBlock.SetTexture(BaseMap, tex);
            _faceRenderer.SetPropertyBlock(_propBlock);

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
