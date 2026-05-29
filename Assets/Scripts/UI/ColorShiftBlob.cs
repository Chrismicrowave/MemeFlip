using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// iPhone-style colour-shifting blob background effect.
/// Two colour sets — Set A during Player 1's turn, Set B during Player 2 / NPC.
/// Blobs drift organically; transition crossfades smoothly between sets.
///
/// Colours are edited directly on the material in the inspector.
/// This script only handles turn detection and smooth transition animation.
/// </summary>
[RequireComponent(typeof(Image))]
public class ColorShiftBlob : MonoBehaviour
{
    const string SHADER_NAME = "UI/ColorShiftBlob";

    [Header("Blob Motion")]
    public float speed = 0.2f;
    public float blobScale = 4f;

    [Header("Turn Transition")]
    [Tooltip("Seconds to crossfade between colour sets when the turn changes.")]
    public float transitionDuration = 1f;

    [Header("Spin")]
    [Tooltip("Enable continuous rotation of the blob pattern.")]
    public bool enableSpin = false;
    [Tooltip("Rotation speed in degrees per second.")]
    public float spinSpeed = 15f;

    // ── Internals ──────────────────────────────────────────────

    Image _image;
    Material _mat;
    Owner? _lastOwner;
    float _transition;
    float _transitionVelocity;
    float _spinAngle;

    static Shader _shader;

    static readonly int _Speed = Shader.PropertyToID("_Speed");
    static readonly int _BlobScale = Shader.PropertyToID("_BlobScale");
    static readonly int _Rotation = Shader.PropertyToID("_Rotation");
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

        // Instance material so runtime changes don't modify the asset
        _mat = new Material(_image.material);
        _image.material = _mat;
    }

    void Start()
    {
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

        // Spin rotation
        if (enableSpin)
        {
            _spinAngle += spinSpeed * Time.deltaTime;
            _mat.SetFloat(_Rotation, _spinAngle);
        }
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
}
