using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class DormPlayerController : MonoBehaviour
{
    [SerializeField] private Camera view;
    [SerializeField] private Transform bedStart;
    [SerializeField] private Transform standingStart;
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float mouseSensitivity = 0.12f;

    private CharacterController character;
    private int visibleLayers;
    private CameraClearFlags normalClearFlags;
    private Color normalBackground;
    private float pitch;
    private float verticalSpeed;
    private bool wakeAllowed = true;

    public bool IsAwake { get; private set; }

    public void SetWakeAllowed(bool allowed) => wakeAllowed = allowed;

    private void Awake()
    {
        character = GetComponent<CharacterController>();
        character.enabled = false;
        transform.SetPositionAndRotation(bedStart.position, bedStart.rotation);
        visibleLayers = view.cullingMask;
        normalClearFlags = view.clearFlags;
        normalBackground = view.backgroundColor;
        view.cullingMask = 0;
        view.clearFlags = CameraClearFlags.SolidColor;
        view.backgroundColor = Color.black;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (!IsAwake)
        {
            if (wakeAllowed && keyboard != null && keyboard.eKey.wasPressedThisFrame)
                WakeUp();
            return;
        }

        if (Mouse.current != null)
        {
            Vector2 look = Mouse.current.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
            view.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        Vector2 input = Vector2.zero;
        if (keyboard != null)
        {
            input.x = keyboard.dKey.ReadValue() - keyboard.aKey.ReadValue();
            input.y = keyboard.wKey.ReadValue() - keyboard.sKey.ReadValue();
        }

        input = Vector2.ClampMagnitude(input, 1f);
        Vector3 horizontal = transform.right * input.x + transform.forward * input.y;
        verticalSpeed = character.isGrounded && verticalSpeed < 0f
            ? -2f
            : verticalSpeed + Physics.gravity.y * Time.deltaTime;
        character.Move((horizontal * walkSpeed + Vector3.up * verticalSpeed) * Time.deltaTime);
    }

    private void WakeUp()
    {
        transform.SetPositionAndRotation(standingStart.position, standingStart.rotation);
        view.transform.localRotation = Quaternion.identity;
        character.enabled = true;
        view.cullingMask = visibleLayers;
        view.clearFlags = normalClearFlags;
        view.backgroundColor = normalBackground;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        IsAwake = true;
    }

    private void OnDisable()
    {
        if (IsAwake)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
