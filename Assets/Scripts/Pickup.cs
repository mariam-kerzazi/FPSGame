using UnityEngine;

public enum PickupType { Gun, Ammo, Grenade, HealthKit }

// One script for all pickups. Set "type" in the Inspector. The collider must have "Is Trigger" checked.
public class Pickup : MonoBehaviour
{
    public PickupType type;
    public int amount = 1;      // bullets for Ammo, count for Grenade (ignored for Gun / HealthKit)

    void Update()
    {
        transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        PlayerWeapons weapons = player.GetComponent<PlayerWeapons>();
        bool collected = false;

        switch (type)
        {
            case PickupType.Gun:
                if (!weapons.HasGun)          // there is only one gun
                {
                    weapons.AddGun();
                    collected = true;
                }
                break;
            case PickupType.Ammo:
                weapons.AddAmmo(amount);
                collected = true;
                break;
            case PickupType.Grenade:
                weapons.AddGrenades(amount);
                collected = true;
                break;
            case PickupType.HealthKit:
                collected = player.Heal();    // false if already at full lives, so the kit stays
                break;
        }

        if (collected)
        {
            Debug.Log("Picked up: " + type);
            AudioManager.Instance.Play(AudioManager.Instance.pickup, transform.position);
            Destroy(gameObject);
        }
    }
}
