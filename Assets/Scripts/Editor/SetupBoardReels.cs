using UnityEditor;
using UnityEngine;

public static class SetupBoardReels
{
    [MenuItem("MemeFlip/Setup Board Reels")]
    public static void Execute()
    {
        // 1. Create stat SO assets
        string folder = "Assets/Settings/ReelStats";
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Settings", "ReelStats");

        var balanced = CreateSO(folder, "Balanced", 10, 3, 2);
        var tanky = CreateSO(folder, "Tanky", 10, 2, 4);
        var glassCannon = CreateSO(folder, "GlassCannon", 10, 5, 1);
        var soList = new[] { balanced, tanky, glassCannon };

        // 2. Find Board
        var boardGO = GameObject.Find("Board");
        if (boardGO == null)
        {
            Debug.LogError("No Board GameObject found in scene.");
            return;
        }

        // 3. Remove old runtime children (except Cube floor)
        var reelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Reel.prefab");
        if (reelPrefab == null)
        {
            Debug.LogError("Reel prefab not found at Assets/Prefabs/Reel.prefab");
            return;
        }

        // 4. Place 12 reels as children of Board
        for (int i = 0; i < 12; i++)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(reelPrefab, boardGO.transform);
            go.name = $"Reel_{i}";

            var reel = go.GetComponent<Reel>();
            if (reel != null)
            {
                reel.statsSO = soList[i % soList.Length];
                EditorUtility.SetDirty(reel);
            }
        }

        // 5. Assign the prefab reference on GameManager
        var gm = GameObject.FindObjectOfType<GameManager>();
        if (gm != null)
        {
            var board = boardGO.GetComponent<Board>();
            SerializedObject so = new SerializedObject(board);
            so.Update();
            so.ApplyModifiedProperties();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Board setup complete — 12 reels placed with SO stats.");
    }

    static ReelStatsSO CreateSO(string folder, string name, int hp, int atk, int def)
    {
        var so = ScriptableObject.CreateInstance<ReelStatsSO>();
        so.maxHP = hp;
        so.atk = atk;
        so.def = def;
        string path = $"{folder}/{name}.asset";
        AssetDatabase.CreateAsset(so, path);
        return so;
    }
}
