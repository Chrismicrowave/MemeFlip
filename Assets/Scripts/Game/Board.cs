using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    [Header("Grid")]
    public Vector2Int gridSize = new(4, 3); // rows, cols
    public float cellSpacing = 2.2f;
    public Vector3 boardCenter = Vector3.zero;

    [Header("Prefab")]
    public GameObject reelPrefab;

    public List<Reel> AllReels { get; private set; } = new();

    // Track grid by position for shuffle operations
    private readonly Dictionary<Vector2Int, Reel> _positionMap = new();

    public void Initialize()
    {
        AllReels.Clear();
        _positionMap.Clear();

        // Generate all 12 positions
        List<Vector2Int> positions = new();
        for (int row = 0; row < gridSize.x; row++)
        for (int col = 0; col < gridSize.y; col++)
            positions.Add(new Vector2Int(row, col));

        // Shuffle positions randomly
        for (int i = positions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (positions[i], positions[j]) = (positions[j], positions[i]);
        }

        // First 6 positions → Player, last 6 → NPC
        Owner[] assignment = new Owner[12];
        for (int i = 0; i < 12; i++)
            assignment[i] = i < 6 ? Owner.Player : Owner.NPC;

        // Shuffle the assignment too so owners are fully intermixed
        for (int i = assignment.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (assignment[i], assignment[j]) = (assignment[j], assignment[i]);
        }

        // Create reels
        for (int i = 0; i < 12; i++)
        {
            Vector2Int pos = positions[i];
            Owner owner = assignment[i];

            Vector3 worldPos = GridToWorld(pos);
            GameObject go = Instantiate(reelPrefab, worldPos, Quaternion.identity, transform);
            go.name = $"{owner}_Reel_{pos.x}_{pos.y}";

            Reel reel = go.GetComponent<Reel>();
            ReelStats stats = GenerateStats();
            reel.Init(owner, stats, pos);

            AllReels.Add(reel);
            _positionMap[pos] = reel;
        }
    }

    public void ShuffleOwnerReels(Owner owner)
    {
        // Collect all alive, face-down reels of this owner
        List<Reel> shufflable = AllReels.FindAll(r =>
            r.owner == owner && !r.isDestroyed && r.isFaceDown);

        if (shufflable.Count < 2) return;

        // Collect their current positions
        List<Vector2Int> positions = shufflable.ConvertAll(r => r.boardPosition);

        // Permute positions randomly
        for (int i = positions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (positions[i], positions[j]) = (positions[j], positions[i]);
        }

        // Move each reel to its new position
        for (int i = 0; i < shufflable.Count; i++)
        {
            Reel reel = shufflable[i];
            Vector2Int newPos = positions[i];

            // Update position map
            _positionMap[reel.boardPosition] = null;
            reel.boardPosition = newPos;
            _positionMap[newPos] = reel;

            // Update world position
            reel.transform.position = GridToWorld(newPos);
        }
    }

    public List<Reel> GetAliveFaceDown(Owner owner)
    {
        return AllReels.FindAll(r =>
            r.owner == owner && !r.isDestroyed && r.isFaceDown);
    }

    public bool HasAliveReels(Owner owner)
    {
        return AllReels.Exists(r => r.owner == owner && !r.isDestroyed);
    }

    public int AliveCount(Owner owner)
    {
        return AllReels.FindAll(r => r.owner == owner && !r.isDestroyed).Count;
    }

    Vector3 GridToWorld(Vector2Int pos)
    {
        float x = (pos.y - (gridSize.y - 1) / 2f) * cellSpacing; // cols on X
        float z = (pos.x - (gridSize.x - 1) / 2f) * cellSpacing; // rows on Z
        return boardCenter + new Vector3(x, 0f, z);
    }

    static ReelStats GenerateStats()
    {
        return new ReelStats
        {
            maxHP = Random.Range(3, 7),
            currentHP = 0, // set below
            atk = Random.Range(2, 6),
            def = Random.Range(1, 4)
        };
    }
}
