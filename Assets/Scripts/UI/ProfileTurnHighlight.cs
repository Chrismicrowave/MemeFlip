using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Highlights the active player's profile group.
/// Active group → full scale, gradients enabled.
/// Inactive group → 50% scale, gradients disabled.
/// </summary>
public class ProfileTurnHighlight : MonoBehaviour
{
    [Header("Profile Groups")]
    public GameObject profileP1;
    public GameObject profileP2;

    [Header("Settings")]
    [Tooltip("Scale of the inactive profile (0-1).")]
    public float inactiveScale = 0.5f;
    [Tooltip("Duration of the scale animation in seconds.")]
    public float animDuration = 0.4f;

    Owner? _lastOwner;

    void Start()
    {
        Apply(ResolveOwner());
    }

    void Update()
    {
        Owner? current = ResolveOwner();
        if (current.HasValue && current != _lastOwner)
        {
            _lastOwner = current;
            Apply(current.Value);
        }
    }

    void Apply(Owner active)
    {
        SetGroup(profileP1, active == Owner.Player);
        SetGroup(profileP2, active != Owner.Player);
    }

    void SetGroup(GameObject group, bool active)
    {
        if (group == null) return;

        Vector3 targetScale = active ? Vector3.one : (Vector3.one * inactiveScale);
        group.transform.DOKill();
        group.transform.DOScale(targetScale, animDuration).SetEase(Ease.OutCubic);

        // Toggle all UIGradients in the group
        var gradients = group.GetComponentsInChildren<UIGradient>(true);
        foreach (var g in gradients)
            g.enabled = active;

        // Toggle all Image raycastTarget as well (optional UX)
        var images = group.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
            img.raycastTarget = active;
    }

    static Owner ResolveOwner()
    {
        var gm = GameManager.Instance;
        if (gm == null) return Owner.Player;

        switch (gm.currentPhase)
        {
            case TurnPhase.NPCTurn:
                return Owner.NPC;
            default:
                return gm.CurrentPlayer;
        }
    }
}
