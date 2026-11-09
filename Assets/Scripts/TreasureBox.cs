using UnityEngine;

// Attach to each of the three treasure boxes. The collider must have "Is Trigger" checked.
// zoneRadius defines the invisible circular protection zone used by the guards.
public class TreasureBox : MonoBehaviour
{
    public float zoneRadius = 12f;

    public bool IsCollected { get; private set; }

    void Update()
    {
        if (!IsCollected)
        {
            transform.Rotate(0f, 45f * Time.deltaTime, 0f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsCollected) return;
        if (other.GetComponent<PlayerController>() == null) return;

        IsCollected = true;
        AudioManager.Instance.Play(AudioManager.Instance.treasure, transform.position);
        GameManager.Instance.CollectTreasure();

        // Hide it but keep the object alive: guards still read this box's position and IsCollected flag.
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        GetComponent<Collider>().enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, zoneRadius);
    }
}
