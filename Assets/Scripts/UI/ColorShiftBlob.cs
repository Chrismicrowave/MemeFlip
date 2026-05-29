using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// iPhone-style colour-shifting blob background effect.
/// Two colour sets — Set A during Player 1's turn, Set B during Player 2 / NPC.
/// Blobs drift organically; transition crossfades smoothly between sets.
/// Drop this on the SpaceBG Image GameObject — it creates its own material.
/// </summary>
[RequireComponent(typeof(Image))]
public class ColorShiftBlob : MonoBehaviour
{
    const int MAX_COLORS = 4;
    const string SHADER_NAME = "UI/ColorShiftBlob";

    // ── Colour Sets ────────────────────────────────────────────
    // Shown in the inspector as expandable lists.
    // Add / remove entries freely.  Alpha > 0.5 = active blob.
    // Each entry becomes one drifting blob on screen.

    [Header("Set A — Player's Turn")]
    [Tooltip("Colours shown during Player 1's turn.  Add or remove entries.")]
    public List<Color> colorSetA = new()
    {
        new Color(0.30f, 0.60f, 1.00f),
        new Color(0.10f, 0.80f, 0.60f),
    };

    [Header("Set B — NPC / Player 2's Turn")]
    [Tooltip("Colours shown during NPC / Player 2's turn.  Add or remove entries.")]
    public List<Color> colorSetB = new()
    {
        new Color(0.90f, 0.30f, 0.30f),
        new Color(0.90f, 0.70f, 0.10f),
    };

    // ── Blob Motion ───────────────────────────────────────────

    [Header("Blob Motion")]
    public float speed = 0.2f;
    public float blobScale = 4f;

    // ── Turn Transition ────────────────────────────────────────

    [Header("Turn Transition")]
    [Tooltip("Seconds to crossfade between colour sets when the turn changes.")]
    public float transitionDuration = 1f;

    // ── Internals ──────────────────────────────────────────────

    Image _image;
    Material _mat;
    Owner? _lastOwner;
    float _transition;
    float _transitionVelocity;

    static Shader _shader;

    // Cached property IDs
    static readonly int _ColorA1 = Shader.PropertyToID("_ColorA1");
    static readonly int _ColorA2 = Shader.PropertyToID("_ColorA2");
    static readonly int _ColorA3 = Shader.PropertyToID("_ColorA3");
    static readonly int _ColorA4 = Shader.PropertyToID("_ColorA4");
    static readonly int _ColorB1 = Shader.PropertyToID("_ColorB1");
    static readonly int _ColorB2 = Shader.PropertyToID("_ColorB2");
    static readonly int _ColorB3 = Shader.PropertyToID("_ColorB3");
    static readonly int _ColorB4 = Shader.PropertyToID("_ColorB4");
    static readonly int _Speed = Shader.PropertyToID("_Speed");
    static readonly int _BlobScale = Shader.PropertyToID("_BlobScale");
    static readonly int _Transition = Shader.PropertyToID("_Transition");

    void Awake()
    {
        _image = GetComponent<Image>();

        if (_shader == null)
            _shader = Shader.Find(SHADER_NAME);

        if (_shader == null || !_shader.isSupported)
        {
            Debug.LogError("[ColorShiftBlob] Shader \"" + SHADER_NAME + "\" not found – disabling.");
            enabled = false;
            return;
        }

        _mat = new Material(_shader);
        _image.material = _mat;
    }

    void Start()
    {
        SyncListsToMaterial();
        _mat.SetFloat(_Speed, speed);
        _mat.SetFloat(_BlobScale, blobScale);

        _lastOwner = ResolveCurrentOwner();
        _transition = _lastOwner == Owner.Player ? 0f : 1f;
        _mat.SetFloat(_Transition, _transition);
    }

    void Update()
    {
        if (_mat == null) return;

        var gm = GameManager.Instance;
        if (gm == null) return;

        _mat.SetFloat(_Speed, speed);
        _mat.SetFloat(_BlobScale, blobScale);

        Owner? current = ResolveCurrentOwner();
        if (current.HasValue && current != _lastOwner)
        {
            _lastOwner = current;
            _transitionVelocity = 0f;
        }

        float target = _lastOwner == Owner.Player ? 0f : 1f;

        if (transitionDuration <= 0f)
            _transition = target;
        else
        {
            float smoothTime = transitionDuration * 0.32f;
            _transition = Mathf.SmoothDamp(_transition, target, ref _transitionVelocity, smoothTime);
        }

        _mat.SetFloat(_Transition, _transition);
    }

    // ── Public helpers ─────────────────────────────────────────

    /// <summary>Call after editing colour lists in the inspector at runtime.</summary>
    public void RefreshColors()
    {
        if (_mat != null) SyncListsToMaterial();
    }

    // ── Internals ──────────────────────────────────────────────

    void SyncListsToMaterial()
    {
        var cA = PadTo4(colorSetA);
        var cB = PadTo4(colorSetB);

        _mat.SetColor(_ColorA1, cA[0]);
        _mat.SetColor(_ColorA2, cA[1]);
        _mat.SetColor(_ColorA3, cA[2]);
        _mat.SetColor(_ColorA4, cA[3]);

        _mat.SetColor(_ColorB1, cB[0]);
        _mat.SetColor(_ColorB2, cB[1]);
        _mat.SetColor(_ColorB3, cB[2]);
        _mat.SetColor(_ColorB4, cB[3]);
    }

    static Color[] PadTo4(List<Color> list)
    {
        var result = new Color[MAX_COLORS];
        for (int i = 0; i < MAX_COLORS; i++)
            result[i] = i < list.Count ? list[i] : Color.clear; // alpha 0 → inactive in shader
        return result;
    }

    static Owner? ResolveCurrentOwner()
    {
        var gm = GameManager.Instance;
        if (gm == null) return null;

        switch (gm.currentPhase)
        {
            case TurnPhase.PlayerSelectFirst:
            case TurnPhase.PlayerSelectSecond:
            case TurnPhase.Resolving:
                return gm.CurrentPlayer;

            case TurnPhase.NPCTurn:
                return Owner.NPC;

            default:
                return null; // ShowResult / GameOver → keep existing colours
        }
    }

    void OnValidate()
    {
        // Fire-and-forget: push colours to material asset so they're visible in-editor.
        // In edit mode _mat may be null; we find the material via the Image instead.
        if (Application.isPlaying) return;
        if (_image == null) _image = GetComponent<Image>();
        if (_image == null) return;
        var mat = _image.material;
        if (mat == null || mat.shader == null) return;

        var cA = PadTo4(colorSetA);
        var cB = PadTo4(colorSetB);

        mat.SetColor(_ColorA1, cA[0]);
        mat.SetColor(_ColorA2, cA[1]);
        mat.SetColor(_ColorA3, cA[2]);
        mat.SetColor(_ColorA4, cA[3]);
        mat.SetColor(_ColorB1, cB[0]);
        mat.SetColor(_ColorB2, cB[1]);
        mat.SetColor(_ColorB3, cB[2]);
        mat.SetColor(_ColorB4, cB[3]);
    }
}
