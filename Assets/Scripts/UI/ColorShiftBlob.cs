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

    [Header("Colour Sets")]
    [Tooltip("Colours shown during Player 1's turn. Add/remove freely.")]
    public List<Color> colorSetA = new()
    {
        new Color(0.30f, 0.60f, 1.00f), // blue
        new Color(0.10f, 0.80f, 0.60f), // teal
    };

    [Tooltip("Colours shown during Player 2 / NPC turn. Add/remove freely.")]
    public List<Color> colorSetB = new()
    {
        new Color(0.90f, 0.30f, 0.30f), // red
        new Color(0.90f, 0.70f, 0.10f), // orange
    };

    [Header("Blob Motion")]
    [Tooltip("How fast the blobs drift.")]
    public float speed = 0.2f;
    [Tooltip("Scale of each blob. Higher = smaller, tighter blobs.")]
    public float blobScale = 4f;

    [Header("Turn Transition")]
    [Tooltip("Duration (seconds) to crossfade between colour sets on turn change.")]
    public float transitionDuration = 1f;

    // --- internals ---
    Image _image;
    Material _mat;
    Owner? _lastOwner;
    float _transition;
    float _transitionVelocity; // for smooth damp
    static Shader _shader;

    // Property IDs (cached once)
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

        // Load shader once
        if (_shader == null)
            _shader = Shader.Find(SHADER_NAME);

        if (_shader == null || !_shader.isSupported)
        {
            Debug.LogError($"[ColorShiftBlob] Shader \"{SHADER_NAME}\" not found — disabling.");
            enabled = false;
            return;
        }

        // Instance the material so we don't modify the asset
        _mat = new Material(_shader);
        _image.material = _mat;
    }

    void Start()
    {
        PushColorsToShader();
        _mat.SetFloat(_Speed, speed);
        _mat.SetFloat(_BlobScale, blobScale);

        // Determine initial owner from GameManager
        _lastOwner = GetCurrentOwner();
        _transition = _lastOwner == Owner.Player ? 0f : 1f;
        _mat.SetFloat(_Transition, _transition);
    }

    void Update()
    {
        if (_mat == null) return;

        var gm = GameManager.Instance;
        if (gm == null) return;

        // Sync anim params (cheap enough to do every frame)
        _mat.SetFloat(_Speed, speed);
        _mat.SetFloat(_BlobScale, blobScale);

        // Detect owner change
        Owner? current = GetCurrentOwner();
        if (current.HasValue && current != _lastOwner)
        {
            _lastOwner = current;
            _transitionVelocity = 0f; // reset smooth-damp velocity on direction change
        }

        // Animate transition
        float target = _lastOwner == Owner.Player ? 0f : 1f;

        if (transitionDuration <= 0f)
        {
            _transition = target;
        }
        else
        {
            // SmoothDamp for an ease-in-out feel without overshoot
            float smoothTime = transitionDuration * 0.32f; // tune so 85% of journey fits in duration
            _transition = Mathf.SmoothDamp(_transition, target, ref _transitionVelocity, smoothTime);
        }

        _mat.SetFloat(_Transition, _transition);
    }

    /// <summary>Call after editing colour lists in the inspector at runtime.</summary>
    public void RefreshColors()
    {
        if (_mat != null) PushColorsToShader();
    }

    void PushColorsToShader()
    {
        var cA = ToArray4(colorSetA);
        var cB = ToArray4(colorSetB);

        _mat.SetColor(_ColorA1, cA[0]);
        _mat.SetColor(_ColorA2, cA[1]);
        _mat.SetColor(_ColorA3, cA[2]);
        _mat.SetColor(_ColorA4, cA[3]);

        _mat.SetColor(_ColorB1, cB[0]);
        _mat.SetColor(_ColorB2, cB[1]);
        _mat.SetColor(_ColorB3, cB[2]);
        _mat.SetColor(_ColorB4, cB[3]);
    }

    /// <summary>Pad/slice list to exactly 4 entries. Alpha = 0 means inactive.</summary>
    static Color[] ToArray4(List<Color> list)
    {
        var result = new Color[MAX_COLORS];
        for (int i = 0; i < MAX_COLORS; i++)
        {
            if (i < list.Count)
                result[i] = list[i];
            else
                result[i] = new Color(0, 0, 0, 0); // alpha 0 = inactive in shader
        }
        return result;
    }

    static Owner? GetCurrentOwner()
    {
        // Use reflection on the phase as a fallback — we just need to know
        // whose turn's visual to show.
        var gm = GameManager.Instance;
        if (gm == null) return null;

        // In PlayerSelectFirst the current player is the one whose turn it is.
        // In NPCTurn it's the NPC.
        // At game start before any turn, default to Player.
        switch (gm.currentPhase)
        {
            case TurnPhase.PlayerSelectFirst:
            case TurnPhase.PlayerSelectSecond:
            case TurnPhase.Resolving:
                // Those phases mean we're mid-turn — show the owner of the current turn.
                // We use a public accessor exposed on GameManager.
                return gm.CurrentPlayer;

            case TurnPhase.NPCTurn:
                return Owner.NPC;

            case TurnPhase.ShowResult:
                // While showing result, keep the colour of whoever just acted.
                // gm.CurrentPlayer is stale (already flipped), so hold last value.
                return null; // null → no change, keep existing

            case TurnPhase.GameOver:
                return null;

            default:
                return null;
        }
    }

    // Rebuild shader colours if the inspector is edited at runtime
    void OnValidate()
    {
        if (Application.isPlaying && _mat != null)
            PushColorsToShader();
    }
}
