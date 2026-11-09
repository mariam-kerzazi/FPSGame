using UnityEngine;

// Attach to the GuardBullet prefab: a small sphere with a trigger collider and a Rigidbody (gravity off).
[RequireComponent(typeof(Rigidbody))]
public class GuardBullet : MonoBehaviour
{
    public float speed = 18f;
    public float lifetime = 4f;

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.AddForce(transform.forward * speed, ForceMode.VelocityChange);
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;                              // ignore zones and pickups
        if (other.GetComponentInParent<Guard>() != null) return;  // don't hit guards

        PlayerController p = other.GetComponentInParent<PlayerController>();
        if (p != null)
        {
            p.TakeDamage(1);
        }

        Destroy(gameObject);   // hit the player or a wall
    }
}
