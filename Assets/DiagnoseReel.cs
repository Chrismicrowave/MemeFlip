using UnityEngine;
using UnityEngine.UI;

public static class DiagnoseReel
{
    public static void Execute()
    {
        var board = GameObject.FindObjectOfType<Board>();
        if (board == null) { Debug.LogError("No Board found"); return; }

        foreach (var reel in board.AllReels)
        {
            if (reel == null) continue;
            Debug.Log($"--- {reel.name} ---");
            var canvas = reel.transform.Find("Canvas");
            if (canvas == null) { Debug.Log("  Canvas: null"); continue; }

            var faceTf = canvas.Find("FaceImage");
            Debug.Log($"  Find('FaceImage'): {(faceTf != null ? faceTf.name : "null")}");
            Debug.Log($"  FaceImage path: {(faceTf != null ? GetPath(faceTf) : "N/A")}");

            var ri = faceTf?.GetComponent<RawImage>();
            Debug.Log($"  RawImage: {(ri != null ? "found" : "null")}");
            if (ri != null)
            {
                Debug.Log($"  texture: {(ri.texture != null ? ri.texture.name : "null")}");
                Debug.Log($"  enabled: {ri.enabled}");
                Debug.Log($"  color: {ri.color}");
                Debug.Log($"  rectTransform size: {ri.rectTransform.rect.size}");
            }

            // Check mask
            var mask = canvas.GetComponentInChildren<Mask>();
            Debug.Log($"  Mask in children: {(mask != null ? mask.name : "none")}");
            if (mask != null)
            {
                Debug.Log($"  Mask enabled: {mask.enabled}");
                Debug.Log($"  ShowMaskGraphic: {mask.showMaskGraphic}");
            }

            // Check mask GO
            var maskTf = canvas.Find("mask");
            if (maskTf != null)
            {
                Debug.Log($"  mask GO active: {maskTf.gameObject.activeSelf}");
                var maskImg = maskTf.GetComponent<Image>();
                Debug.Log($"  mask Image: {(maskImg != null ? "found" : "null")}");
                if (maskImg != null)
                {
                    Debug.Log($"  mask sprite: {(maskImg.sprite != null ? maskImg.sprite.name : "null")}");
                    Debug.Log($"  mask color: {maskImg.color}");
                    Debug.Log($"  mask raycastTarget: {maskImg.raycastTarget}");
                }
            }
        }

        // Flip a reel face up to see what happens
        var alive = board.GetAliveFaceDown(Owner.Player);
        if (alive.Count > 0)
        {
            var testReel = alive[0];
            Debug.Log($"\nFlipping {testReel.name} face-up...");
            testReel.FlipUp();

            var ri2 = testReel.transform.Find("Canvas")?.Find("FaceImage")?.GetComponent<RawImage>();
            if (ri2 != null)
                Debug.Log($"After flip - texture: {(ri2.texture != null ? ri2.texture.name : "null")}");
        }
    }

    static string GetPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return GetPath(t.parent) + "/" + t.name;
    }
}
