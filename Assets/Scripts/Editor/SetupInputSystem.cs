using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class SetupInputSystem
{
    [MenuItem("Tools/Setup Input System")]
    public static void Execute()
    {
        Camera cam = Camera.main;
        if (cam != null && cam.GetComponent<PhysicsRaycaster>() == null)
            cam.gameObject.AddComponent<PhysicsRaycaster>();

        EventSystem es = Object.FindObjectOfType<EventSystem>();
        if (es == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            es = esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        Debug.Log("Input system setup complete!");
    }

    [MenuItem("Tools/Reimport TMP")]
    public static void ReimportTMP()
    {
        EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
        Debug.Log("TMP Essentials import window opened");
    }
}
