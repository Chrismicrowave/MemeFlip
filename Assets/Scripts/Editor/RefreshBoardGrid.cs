using UnityEditor;
using UnityEngine;

public static class RefreshBoardGrid
{
    [MenuItem("MemeFlip/Refresh Board Grid")]
    public static void Execute()
    {
        var board = Object.FindObjectOfType<Board>();
        if (board != null)
        {
            board.RefreshGrid();
            EditorUtility.SetDirty(board);
            // Mark scene as dirty so the positions are saved
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("Board grid refreshed in edit mode.");
        }
        else
        {
            Debug.LogError("No Board found in scene.");
        }
    }
}
