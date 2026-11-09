using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

// Tracks the overall game state: treasures, kills, win/lose. Lives on the "GameManager" empty object.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public PlayerController player;

    public int TreasuresCollected { get; private set; }
    public int TotalTreasures { get; private set; }
    public int Kills { get; private set; }
    public int GuardsAlive { get; private set; }
    public int TotalGuards { get; private set; }
    public bool GameEnded { get; private set; }
    public string EndMessage { get; private set; } = "";

    // Once this is true, every remaining guard switches to the Flee state.
    public bool AllTreasuresCollected
    {
        get { return TotalTreasures > 0 && TreasuresCollected >= TotalTreasures; }
    }

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        if (player == null) player = FindAnyObjectByType<PlayerController>();
    }

    void Start()
    {
        TotalTreasures = FindObjectsByType<TreasureBox>().Length;
        TotalGuards = FindObjectsByType<Guard>().Length;
        GuardsAlive = TotalGuards;
        Debug.Log("Treasures: " + TotalTreasures + ", Guards: " + TotalGuards);
    }

    void Update()
    {
        // After the game ends, press R to restart the level.
        if (GameEnded && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void CollectTreasure()
    {
        TreasuresCollected++;
        Debug.Log("Treasures collected: " + TreasuresCollected + "/" + TotalTreasures);
        if (AllTreasuresCollected)
        {
            Debug.Log("All treasures collected! The guards are fleeing - hunt them down!");
        }
        CheckWin();
    }

    public void GuardKilled()
    {
        Kills++;
        GuardsAlive--;
        Debug.Log("Guard eliminated. Kills: " + Kills + "/" + TotalGuards);
        CheckWin();
    }

    public void PlayerDied()
    {
        EndGame("Game Over!");
    }

    private void CheckWin()
    {
        if (!GameEnded && AllTreasuresCollected && GuardsAlive <= 0)
        {
            EndGame("You Win!");
        }
    }

    private void EndGame(string message)
    {
        GameEnded = true;
        EndMessage = message;
        Debug.Log(message);
        Time.timeScale = 0f;              // freeze the world
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
