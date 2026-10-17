using UnityEngine;

/// <summary>
/// Swings the door leaf around its hinge. Call Toggle(), Open() or Close() from your own
/// interaction code (raycast, trigger, button, etc.).
/// </summary>
[DisallowMultipleComponent]
public class LowPolyDoor : MonoBehaviour
{
    [SerializeField] private Transform leaf;
    [SerializeField, Range(10f, 180f)] private float openAngle = 90f;
    [Tooltip("Off: swings away from the front (+Z). On: swings toward the front (-Z).")]
    [SerializeField] private bool swingTowardFront = false;
    [Tooltip("Degrees per second. 0 snaps instantly, like Minecraft.")]
    [SerializeField, Min(0f)] private float swingSpeed = 360f;
    [SerializeField] private bool startOpen = false;

    private bool isOpen;
    private float angle;

    public bool IsOpen => isOpen;
    public bool IsMoving => !Mathf.Approximately(angle, TargetAngle);

    private float TargetAngle => isOpen ? (swingTowardFront ? openAngle : -openAngle) : 0f;

    /// <summary>Used by the prefab builder.</summary>
    public void Bind(Transform leafTransform) => leaf = leafTransform;

    public void Open() => isOpen = true;
    public void Close() => isOpen = false;
    public void Toggle() => isOpen = !isOpen;

    /// <summary>Jumps straight to open or closed with no animation.</summary>
    public void SetOpenInstant(bool open)
    {
        isOpen = open;
        angle = TargetAngle;
        ApplyAngle();
    }

    private void Awake() => SetOpenInstant(startOpen);

    private void Update()
    {
        if (leaf == null || !IsMoving) return;
        angle = swingSpeed <= 0f ? TargetAngle : Mathf.MoveTowards(angle, TargetAngle, swingSpeed * Time.deltaTime);
        ApplyAngle();
    }

    private void ApplyAngle()
    {
        if (leaf != null) leaf.localRotation = Quaternion.Euler(0f, angle, 0f);
    }
}
