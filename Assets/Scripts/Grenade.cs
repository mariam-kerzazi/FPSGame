using UnityEngine;

// Attach to the Grenade prefab (a small sphere with a Rigidbody and a non-trigger collider).
// It explodes after a short fuse, or immediately when it touches a guard.
public class Grenade : MonoBehaviour
{
    public float fuseTime = 2.5f;
    public float radius = 6f;
    public GameObject explosionEffectPrefab;   // optional particle effect

    private bool exploded = false;

    void Start()
    {
        Invoke(nameof(Explode), fuseTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.GetComponentInParent<Guard>() != null)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        AudioManager.Instance.Play(AudioManager.Instance.explosion, transform.position);

        if (explosionEffectPrefab != null)
        {
            Destroy(Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity), 3f);
        }

        // Anyone caught in the blast dies in one hit.
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (Collider c in hits)
        {
            Guard guard = c.GetComponentInParent<Guard>();
            if (guard != null)
            {
                guard.Kill();
            }
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
