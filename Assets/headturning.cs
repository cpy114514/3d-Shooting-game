using UnityEngine;
using UnityEngine.InputSystem;

public class ModelLookAtMouse : MonoBehaviour
{
    public float lookStrengthX = 30f; // 左右转
    public float lookStrengthY = 15f; // 上下点头
    public float smoothSpeed = 8f;

    private Quaternion startRotation;

    void Start()
    {
        startRotation = transform.localRotation;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        float mouseX = (mousePos.x / Screen.width - 0.5f) * 2f;
        float mouseY = (mousePos.y / Screen.height - 0.5f) * 2f;

        float yaw = mouseX * lookStrengthX;
        float pitch = -mouseY * lookStrengthY;

        Quaternion targetRotation = startRotation * Quaternion.Euler(pitch, yaw, 0f);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            smoothSpeed * Time.deltaTime
        );
    }
}