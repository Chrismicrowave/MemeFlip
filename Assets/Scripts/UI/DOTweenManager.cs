using System.Collections;
using DG.Tweening;
using UnityEngine;

public class DOTweenManager : MonoBehaviour
{
    [Header("Reel Fly Animation")]
    [Tooltip("Duration of the reel-to-slot fly animation in seconds")]
    public float flyDuration = 0.4f;
    [Tooltip("Easing curve for the fly animation (time 0→1, value 0→1)")]
    public AnimationCurve flyMotionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("Final local scale of the flying reel clone at the slot position")]
    public float flyEndScale = 0.35f;

    [Header("Attack Animation")]
    [Tooltip("Easing for the dash-out / dash-back attack motion")]
    public Ease attackDashEase = Ease.InOutQuad;
    [Tooltip("Easing for the jitter timer (linear preserves the raw Sin/Cos wave)")]
    public Ease attackJitterEase = Ease.Linear;

    public bool IsSlot1Animating { get; private set; }
    public bool IsSlot2Animating { get; private set; }
    public bool IsAnySlotAnimating => IsSlot1Animating || IsSlot2Animating;

    // ─── Fly animation (reel clone to slot scene position) ───

    public void AnimateReelFly(Reel reel, Transform slotScenePos, int slotIndex, System.Action onComplete)
    {
        StartCoroutine(FlyReelToSlot(reel, slotScenePos, slotIndex, onComplete));
    }

    IEnumerator FlyReelToSlot(Reel reel, Transform slotScenePos, int slotIndex, System.Action onComplete)
    {
        if (slotIndex == 0) IsSlot1Animating = true;
        else IsSlot2Animating = true;

        GameObject clone = Instantiate(reel.gameObject, reel.transform.position, reel.transform.rotation, null);
        clone.transform.localScale = reel.transform.lossyScale;

        Collider col = clone.GetComponent<Collider>();
        if (col != null) Destroy(col);
        Reel reelComp = clone.GetComponent<Reel>();
        if (reelComp != null) Destroy(reelComp);

        Sequence seq = DOTween.Sequence();
        seq.Join(clone.transform.DOMove(slotScenePos.position, flyDuration).SetEase(flyMotionCurve));
        seq.Join(clone.transform.DOScale(Vector3.one * flyEndScale, flyDuration).SetEase(flyMotionCurve));

        yield return seq.WaitForCompletion();

        Destroy(clone);

        if (slotIndex == 0) IsSlot1Animating = false;
        else IsSlot2Animating = false;

        onComplete?.Invoke();
    }

    // ─── Dash-and-back attack ───

    public IEnumerator DashAndBack(Reel reel, Reel target, RectTransform slotRt)
    {
        float width = reel.GetComponent<Renderer>().bounds.size.x;
        float distance = width * reel.attackDashDistancePercent;
        Vector3 dir = (target.transform.position - reel.transform.position).normalized;
        float halfDuration = reel.attackDashDuration * 0.5f;
        Vector3 startPos = reel.transform.position;
        Vector3 targetPos = startPos + dir * distance;

        Vector2 slotOrig = slotRt != null ? slotRt.anchoredPosition : Vector2.zero;
        float slotDist = slotRt != null ? slotRt.rect.width * reel.attackDashDistancePercent : 0f;
        Vector2 slotTarget = slotOrig + Vector2.right * slotDist * Mathf.Sign(dir.x);

        Sequence seq = DOTween.Sequence();
        seq.Append(reel.transform.DOMove(targetPos, halfDuration).SetEase(attackDashEase));
        if (slotRt != null)
            seq.Join(slotRt.DOAnchorPos(slotTarget, halfDuration).SetEase(attackDashEase));
        seq.Append(reel.transform.DOMove(startPos, halfDuration).SetEase(attackDashEase));
        if (slotRt != null)
            seq.Join(slotRt.DOAnchorPos(slotOrig, halfDuration).SetEase(attackDashEase));

        yield return seq.WaitForCompletion();
    }

    // ─── Sin/Cos jitter ───

    public IEnumerator Jitter(Reel reel, RectTransform slotRt)
    {
        Vector3 origPos = reel.transform.position;
        Vector2 slotOrig = slotRt != null ? slotRt.anchoredPosition : Vector2.zero;
        float t = 0f;
        Tween tween = DOTween.To(() => t, v => t = v, 1f, reel.jitterDuration)
            .SetEase(attackJitterEase)
            .OnUpdate(() =>
            {
                float angle = t * reel.jitterDuration * reel.jitterSpeed;
                float x = Mathf.Sin(angle) * reel.jitterIntensity;
                float z = Mathf.Cos(angle * 0.7f) * reel.jitterIntensity;
                reel.transform.position = origPos + new Vector3(x, 0f, z);
                if (slotRt != null)
                    slotRt.anchoredPosition = slotOrig + new Vector2(
                        Mathf.Sin(angle) * reel.jitterSlotIntensity, 0f);
            })
            .OnComplete(() =>
            {
                reel.transform.position = origPos;
                if (slotRt != null) slotRt.anchoredPosition = slotOrig;
            });

        yield return tween.WaitForCompletion();
    }
}
