using UnityEngine;

public class SpaceBGMotion : MonoBehaviour
{
    [Header("Circular Movement")]
    [Tooltip("Radius of the circle in screen pixels")]
    public float radius = 50f;
    [Tooltip("Base speed of circular movement (degrees per second)")]
    public float circleSpeed = 30f;

    [Header("Constant Rotation (Spin)")]
    [Tooltip("Constant rotation speed (degrees per second)")]
    public float spinSpeed = 15f;

    private RectTransform _rt;
    private Vector2 _startPos;
    private float _angle;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _startPos = _rt.anchoredPosition;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        _angle += circleSpeed * dt;
        float rad = _angle * Mathf.Deg2Rad;

        Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        _rt.anchoredPosition = _startPos + offset;

        // Constant rotation (spin)
        _rt.localRotation *= Quaternion.Euler(0f, 0f, spinSpeed * dt);
    }
}
