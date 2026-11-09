using UnityEngine;
using UnityEngine.InputSystem;

// First-person movement + lives, using Unity's new Input System.
// Attach to the Player capsule (it also needs a CharacterController). The Main Camera is a child of the Player.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public Transform cameraTransform;
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpHeight = 1.2f;
    public float gravity = -20f;
    public float lookSensitivity = 0.1f;

    public int maxLives = 3;
    public int lives = 3;
    public float invulnerableSeconds = 0.5f;

    private CharacterController controller;
    private InputAction moveAction, lookAction, jumpAction, runAction;
    private float verticalVelocity;
    private float pitch;
    private float lastHitTime = -999f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Move: WASD and the arrow keys both feed one Vector2 action.
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");

        lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        runAction = new InputAction("Run", InputActionType.Button, "<Keyboard>/leftShift");
    }

    void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        runAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        jumpAction.Disable();
        runAction.Disable();
    }

    void Start()
    {
        lives = maxLives;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.GameEnded) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        Look();
        Move();
    }

    // Mouse X turns the whole body, mouse Y tilts only the camera.
    private void Look()
    {
        Vector2 delta = lookAction.ReadValue<Vector2>() * lookSensitivity;
        transform.Rotate(0f, delta.x, 0f);

        pitch -= delta.y;
        pitch = Mathf.Clamp(pitch, -80f, 80f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        move = Vector3.ClampMagnitude(move, 1f);

        float speed = runAction.IsPressed() ? runSpeed : walkSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (controller.isGrounded && jumpAction.WasPressedThisFrame())
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * speed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    // Called by GuardBullet when a bullet hits the player.
    public void TakeDamage(int amount = 1)
    {
        if (GameManager.Instance.GameEnded) return;
        if (Time.time - lastHitTime < invulnerableSeconds) return;
        lastHitTime = Time.time;

        lives -= amount;
        Debug.Log("Player hit! Lives left: " + lives);

        AudioManager.Instance.Play(AudioManager.Instance.playerHit, transform.position);
        if (GameUI.Instance != null) GameUI.Instance.FlashDamage();

        if (lives <= 0)
        {
            lives = 0;
            GameManager.Instance.PlayerDied();
        }
    }

    // Called by a health-kit Pickup. Returns false if the player is already at full health.
    public bool Heal()
    {
        if (lives >= maxLives) return false;
        lives++;
        Debug.Log("Health restored. Lives: " + lives);
        return true;
    }
}
