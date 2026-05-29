using UnityEngine;
using UnityEngine.UI;

public static class DiagHandleColorSimple
{
    public static void Execute()
    {
        var go = GameObject.Find("GameCanvas");
        var ap = go.GetComponent<ActionPanel>();

        if (ap.playerSlot1HPBar?.handleRect != null)
            Debug.Log($"BAR1 handle color: {ap.playerSlot1HPBar.handleRect.GetComponent<Image>()?.color}");
        else
            Debug.Log("BAR1 handleRect is NULL");

        if (ap.playerSlot2HPBar?.handleRect != null)
            Debug.Log($"BAR2 handle color: {ap.playerSlot2HPBar.handleRect.GetComponent<Image>()?.color}");
        else
            Debug.Log("BAR2 handleRect is NULL");
    }
}
