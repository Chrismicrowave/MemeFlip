using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Board : MonoBehaviour
{
    [Header("Grid")]
    public Vector2Int gridSize = new(4, 3); // rows, cols
    public float cellSpacing = 2f;

    [Header("Dynamic Scaling")]
    public float dynamicScaleMax = 1.8f;
    public float dynamicScaleRadius = 3f;
    [Tooltip("Higher = sharper falloff near cursor, more distinction between closest reels")]
    public float dynamicScaleCurve = 2f;

    [Header("Reel Fly Animation")]
    [Tooltip("Duration of the reel-to-slot fly animation in seconds")]
    public float flyDuration = 0.4f;
    [Tooltip("Easing curve for the fly animation (time 0→1, value 0→1)")]
    public AnimationCurve flyMotionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("Final local scale of the flying reel clone at the slot position")]
    public float flyEndScale = 0.35f;

    public bool IsSlot1Animating { get; private set; }
    public bool IsSlot2Animating { get; private set; }
    public bool IsAnySlotAnimating => IsSlot1Animating || IsSlot2Animating;

    [Header("Memes")]
    public MemeLibrary memeLibrary;

    public List<Reel> AllReels { get; private set; } = new();

    // Track grid by position for shuffle operations
    private readonly Dictionary<Vector2Int, Reel> _positionMap = new();

    public void Initialize()
    {
        AllReels.Clear();
        _positionMap.Clear();

        // Collect pre-placed reel children
        Reel[] reels = GetComponentsInChildren<Reel>();
        if (reels.Length == 0)
        {
            Debug.LogError("No Reel children found under Board. Place 12 Reel prefabs in the scene under Board.");
            return;
        }

        // Generate all grid positions
        List<Vector2Int> positions = new();
        for (int row = 0; row < gridSize.x; row++)
        for (int col = 0; col < gridSize.y; col++)
            positions.Add(new Vector2Int(row, col));

        // Trim to available reels if fewer than grid
        int count = Mathf.Min(reels.Length, positions.Count);

        // Shuffle positions randomly
        for (int i = positions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (positions[i], positions[j]) = (positions[j], positions[i]);
        }

        // First half → Player, second half → NPC, then shuffle
        Owner[] assignment = new Owner[count];
        int half = count / 2;
        for (int i = 0; i < count; i++)
            assignment[i] = i < half ? Owner.Player : Owner.NPC;

        for (int i = assignment.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (assignment[i], assignment[j]) = (assignment[j], assignment[i]);
        }

        // Shuffle memes for unique deal-like assignment
        List<MemeData> shuffledMemes = new();
        if (memeLibrary != null && memeLibrary.memes.Count > 0)
        {
            shuffledMemes = new List<MemeData>(memeLibrary.memes);
            for (int i = shuffledMemes.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffledMemes[i], shuffledMemes[j]) = (shuffledMemes[j], shuffledMemes[i]);
            }
        }

        // Assign each reel
        for (int i = 0; i < count; i++)
        {
            Vector2Int pos = positions[i];
            Owner owner = assignment[i];
            Reel reel = reels[i];

            reel.name = $"{owner}_Reel_{pos.x}_{pos.y}";
            reel.transform.position = GridToWorld(pos);
            reel.Init(owner, pos);
            reel.memeData = shuffledMemes.Count > 0
                ? shuffledMemes[i % shuffledMemes.Count]
                : null;

            AllReels.Add(reel);
            _positionMap[pos] = reel;
        }
    }

    public void ShuffleOwnerReels(Owner owner)
    {
        List<Reel> shufflable = AllReels.FindAll(r =>
            r.owner == owner && !r.isDestroyed && r.isFaceDown);

        if (shufflable.Count < 2) return;

        // Collect all positions this owner's reels occupy (including destroyed)
        List<Vector2Int> allOwnerPositions = new();
        foreach (var reel in AllReels)
            if (reel.owner == owner)
                allOwnerPositions.Add(reel.boardPosition);

        if (allOwnerPositions.Count < shufflable.Count) return;

        // Shuffle all owner positions, take first N for alive reels
        for (int i = allOwnerPositions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (allOwnerPositions[i], allOwnerPositions[j]) = (allOwnerPositions[j], allOwnerPositions[i]);
        }

        for (int i = 0; i < shufflable.Count; i++)
        {
            Reel reel = shufflable[i];
            Vector2Int newPos = allOwnerPositions[i];

            if (_positionMap.ContainsKey(reel.boardPosition))
                _positionMap[reel.boardPosition] = null;

            reel.boardPosition = newPos;
            _positionMap[newPos] = reel;
            reel.transform.position = GridToWorld(newPos);
        }
    }

    /// <summary>Shuffle all alive face-down reels across every grid position (slots left empty are possible).</summary>
    public void ShuffleAllFaceDown()
    {
        List<Reel> shufflable = AllReels.FindAll(r => !r.isDestroyed && r.isFaceDown);
        if (shufflable.Count < 1) return;

        // Generate all grid positions as the pool
        List<Vector2Int> allPositions = new();
        for (int row = 0; row < gridSize.x; row++)
        for (int col = 0; col < gridSize.y; col++)
            allPositions.Add(new Vector2Int(row, col));

        // Shuffle all positions
        for (int i = allPositions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (allPositions[i], allPositions[j]) = (allPositions[j], allPositions[i]);
        }

        // Clear old position map entries
        foreach (var reel in shufflable)
            if (_positionMap.ContainsKey(reel.boardPosition))
                _positionMap[reel.boardPosition] = null;

        for (int i = 0; i < shufflable.Count; i++)
        {
            Reel reel = shufflable[i];
            Vector2Int newPos = allPositions[i];
            reel.boardPosition = newPos;
            _positionMap[newPos] = reel;
            reel.transform.position = GridToWorld(newPos);
        }
    }

    public List<Reel> GetAliveFaceDown(Owner owner)
    {
        return AllReels.FindAll(r =>
            r.owner == owner && !r.isDestroyed && r.isFaceDown);
    }

    public List<Reel> GetAliveFaceDownAll()
    {
        return AllReels.FindAll(r => !r.isDestroyed && r.isFaceDown);
    }

    public bool HasAliveReels(Owner owner)
    {
        return AllReels.Exists(r => r.owner == owner && !r.isDestroyed);
    }

    public int AliveCount(Owner owner)
    {
        return AllReels.FindAll(r => r.owner == owner && !r.isDestroyed).Count;
    }

    public void UpdateDynamicScales(Vector3 mouseWorldPos)
    {
        foreach (var reel in AllReels)
        {
            if (reel == null || reel.isDestroyed) continue;
            float dist = Vector3.Distance(mouseWorldPos, reel.transform.position);
            float t = 1f - Mathf.Clamp01(dist / dynamicScaleRadius);
            t = Mathf.Pow(t, dynamicScaleCurve);
            float scale = Mathf.Lerp(1f, dynamicScaleMax, t);
            reel.transform.localScale = Vector3.one * scale;
            reel.ShowFlipPrompt(scale > 1.1f);
        }
    }

    public void ResetDynamicScales()
    {
        foreach (var reel in AllReels)
            if (reel != null && !reel.isDestroyed)
            {
                reel.transform.localScale = Vector3.one;
                reel.ShowFlipPrompt(false);
            }
    }

    /// <summary>
    /// Fly a clone of the reel from its board position to the target slot scene position.
    /// Calls onComplete after the animation finishes.
    /// </summary>
    public void AnimateReelToSlot(Reel reel, Transform slotScenePos, int slotIndex, System.Action onComplete)
    {
        StartCoroutine(AnimateReelToSlotCoroutine(reel, slotScenePos, slotIndex, onComplete));
    }

    IEnumerator AnimateReelToSlotCoroutine(Reel reel, Transform slotScenePos, int slotIndex, System.Action onComplete)
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

    [ContextMenu("Refresh Grid")]
    public void RefreshGrid()
    {
        Reel[] reels = GetComponentsInChildren<Reel>();
        int cols = gridSize.y;
        for (int i = 0; i < reels.Length; i++)
        {
            int row = i / cols;
            int col = i % cols;
            Vector2Int pos = new Vector2Int(row, col);
            reels[i].boardPosition = pos;
            reels[i].transform.position = GridToWorld(pos);
        }

        if (Application.isPlaying)
        {
            for (int i = 0; i < reels.Length; i++)
            {
                if (!AllReels.Contains(reels[i]))
                    AllReels.Add(reels[i]);
                _positionMap[reels[i].boardPosition] = reels[i];
            }
        }
    }

    Vector3 GridToWorld(Vector2Int pos)
    {
        float x = (pos.y - (gridSize.y - 1) / 2f) * cellSpacing;
        float z = (pos.x - (gridSize.x - 1) / 2f) * cellSpacing;
        return transform.position + new Vector3(x, 0f, z);
    }
}
