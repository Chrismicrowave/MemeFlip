using UnityEngine;
using UnityEngine.UI;

public static class DiagHandleColor
{
    public static void Execute()
    {
        var go = GameObject.Find("GameCanvas");
        if (go == null) return;
        var ap = go.GetComponent<ActionPanel>();
        if (ap == null) return;

        // Reflect private fields via names
        var f1 = typeof(ActionPanel).GetField("_handleOrgColor1", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var f2 = typeof(ActionPanel).GetField("_handleOrgColor2", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Debug.Log($"Cached handle1: {f1?.GetValue(ap)}");
        Debug.Log($"Cached handle2: {f2?.GetValue(ap)}");

        // Current live handle colors
        var bar1 = ap.playerSlot1HPBar;
        var bar2 = ap.playerSlot2HPBar;

        if (bar1?.handleRect != null)
            Debug.Log($"Live handle1 color: {bar1.handleRect.GetComponent<Image>()?.color}");
        if (bar2?.handleRect != null)
            Debug.Log($"Live handle2 color: {bar2.handleRect.GetComponent<Image>()?.color}");

        Debug.Log($"Bar1 handleRect assigned: {bar1?.handleRect != null}");
        Debug.Log($"Bar2 handleRect assigned: {bar2?.handleRect != null}");
    }
}
