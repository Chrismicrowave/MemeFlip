using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    [Header("Grid")]
    public Vector2Int gridSize = new(4, 3); // rows, cols
    public float cellSpacing = 2f;

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

        // Assign each reel
        for (int i = 0; i < count; i++)
        {
            Vector2Int pos = positions[i];
            Owner owner = assignment[i];
            Reel reel = reels[i];

            reel.name = $"{owner}_Reel_{pos.x}_{pos.y}";
            reel.transform.position = GridToWorld(pos);
            reel.Init(owner, pos);
            reel.memeData = memeLibrary != null && memeLibrary.memes.Count > 0
                ? memeLibrary.memes[Random.Range(0, memeLibrary.memes.Count)]
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

        List<Vector2Int> positions = shufflable.ConvertAll(r => r.boardPosition);

        for (int i = positions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (positions[i], positions[j]) = (positions[j], positions[i]);
        }

        for (int i = 0; i < shufflable.Count; i++)
        {
            Reel reel = shufflable[i];
            Vector2Int newPos = positions[i];

            _positionMap[reel.boardPosition] = null;
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
