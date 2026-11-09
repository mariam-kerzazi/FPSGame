using UnityEngine;

// Holds every sound clip and plays them. Lives on the "GameManager" empty object.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioClip gunshot;
    public AudioClip throwGrenade;
    public AudioClip explosion;
    public AudioClip guardHit;
    public AudioClip playerHit;
    public AudioClip treasure;
    public AudioClip pickup;
    public AudioClip guardShot;
    public AudioClip music;
    [Range(0f, 1f)] public float musicVolume = 0.3f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Background music loops on its own AudioSource.
        if (music != null)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.clip = music;
            src.loop = true;
            src.volume = musicVolume;
            src.Play();
        }
    }

    // Plays a one-shot sound at a world position.
    public void Play(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }
    }
}
