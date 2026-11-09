using UnityEngine;
using UnityEngine.InputSystem;

public enum WeaponType { None, Gun, Grenade }

// Weapon inventory, switching (Q/E) and firing (left mouse button). Attach to the Player.
// The gun and grenade models are children of the camera, hidden until picked up.
[RequireComponent(typeof(CharacterController))]
public class PlayerWeapons : MonoBehaviour
{
    public Transform cameraTransform;
    public GameObject gunModel;
    public GameObject grenadeModel;
    public GameObject grenadePrefab;

    public float gunRange = 60f;
    public float throwForce = 14f;
    public float fireCooldown = 0.3f;
    public int bulletsPerGunPickup = 6;

    public bool HasGun { get; private set; }
    public int Bullets { get; private set; }
    public int Grenades { get; private set; }
    public WeaponType Current { get; private set; } = WeaponType.None;

    private InputAction fireAction, switchAction;
    private float lastFireTime = -999f;
    private CharacterController ownCollider;

    void Awake()
    {
        ownCollider = GetComponent<CharacterController>();

        fireAction = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");

        // Q and E both switch weapon.
        switchAction = new InputAction("Switch", InputActionType.Button);
        switchAction.AddBinding("<Keyboard>/q");
        switchAction.AddBinding("<Keyboard>/e");
    }

    void OnEnable()
    {
        fireAction.Enable();
        switchAction.Enable();
    }

    void OnDisable()
    {
        fireAction.Disable();
        switchAction.Disable();
    }

    void Start()
    {
        UpdateModels();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.GameEnded) return;

        if (switchAction.WasPressedThisFrame()) CycleWeapon();
        if (fireAction.WasPressedThisFrame()) Fire();
    }

    // ---- Pickups call these ----
    public void AddGun()
    {
        HasGun = true;
        Bullets += bulletsPerGunPickup;
        if (Current == WeaponType.None) Current = WeaponType.Gun;
        UpdateModels();
    }

    public void AddAmmo(int amount)
    {
        Bullets += amount;
    }

    public void AddGrenades(int amount)
    {
        Grenades += amount;
        if (Current == WeaponType.None) Current = WeaponType.Grenade;
        UpdateModels();
    }

    // ---- Switching ----
    private void CycleWeapon()
    {
        if (HasGun && Grenades > 0)
        {
            Current = (Current == WeaponType.Gun) ? WeaponType.Grenade : WeaponType.Gun;
            UpdateModels();
        }
    }

    // Show only the model for the weapon in hand.
    private void UpdateModels()
    {
        if (gunModel != null) gunModel.SetActive(Current == WeaponType.Gun);
        if (grenadeModel != null) grenadeModel.SetActive(Current == WeaponType.Grenade);
    }

    // ---- Firing ----
    private void Fire()
    {
        if (Time.time - lastFireTime < fireCooldown) return;

        if (Current == WeaponType.Gun) FireGun();
        else if (Current == WeaponType.Grenade) ThrowGrenade();
    }

    private void FireGun()
    {
        if (Bullets <= 0)
        {
            Debug.Log("Out of ammo!");
            return;
        }

        lastFireTime = Time.time;
        Bullets--;
        AudioManager.Instance.Play(AudioManager.Instance.gunshot, cameraTransform.position);

        // Hitscan: a ray from the centre of the screen.
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, gunRange, ~0, QueryTriggerInteraction.Ignore))
        {
            Guard guard = hit.collider.GetComponentInParent<Guard>();
            if (guard != null)
            {
                guard.TakeDamage(1);   // three hits kill a guard
            }
        }
    }

    private void ThrowGrenade()
    {
        lastFireTime = Time.time;
        Grenades--;
        AudioManager.Instance.Play(AudioManager.Instance.throwGrenade, cameraTransform.position);

        GameObject g = Instantiate(grenadePrefab,
            cameraTransform.position + cameraTransform.forward * 1f, Quaternion.identity);

        // Don't let the grenade collide with the thrower.
        Physics.IgnoreCollision(g.GetComponent<Collider>(), ownCollider);

        Rigidbody rb = g.GetComponent<Rigidbody>();
        rb.AddForce(cameraTransform.forward * throwForce + Vector3.up * 3f, ForceMode.VelocityChange);

        // Out of grenades? Fall back to the gun if we have one.
        if (Grenades <= 0)
        {
            Current = HasGun ? WeaponType.Gun : WeaponType.None;
            UpdateModels();
        }
    }
}
