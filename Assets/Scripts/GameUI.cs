using UnityEngine;
using UnityEngine.UI;

// Builds the HUD (Unity UI / uGUI) at startup and keeps it updated. Lives on the "GameManager" empty object.
// Shows: lives, weapons collected, bullets left, kills, treasures, a crosshair and the win/lose message.
public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

    private Text statusText;
    private Text messageText;
    private Image damageFlash;
    private PlayerController player;
    private PlayerWeapons weapons;
    private float flashAlpha = 0f;

    void Awake()
    {
        Instance = this;
        BuildUI();
    }

    void Start()
    {
        player = GameManager.Instance.player;
        weapons = player.GetComponent<PlayerWeapons>();
    }

    void Update()
    {
        if (player == null || weapons == null) return;
        GameManager gm = GameManager.Instance;

        string equipped = weapons.Current == WeaponType.Gun ? "Gun"
                        : weapons.Current == WeaponType.Grenade ? "Grenade" : "None";
        string owned = (weapons.HasGun ? "Gun " : "") + (weapons.Grenades > 0 ? "Grenade x" + weapons.Grenades : "");
        if (owned == "") owned = "(none yet)";

        statusText.text =
            "Lives: " + player.lives + "/" + player.maxLives + "\n" +
            "Weapons: " + owned + "\n" +
            "Equipped: " + equipped + "  (Q/E to switch)\n" +
            "Bullets: " + weapons.Bullets + "\n" +
            "Kills: " + gm.Kills + "/" + gm.TotalGuards + "\n" +
            "Treasures: " + gm.TreasuresCollected + "/" + gm.TotalTreasures;

        messageText.text = gm.GameEnded ? gm.EndMessage + "\nPress R to restart" : "";

        // Fade the red damage flash.
        flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.unscaledDeltaTime * 1.5f);
        damageFlash.color = new Color(1f, 0f, 0f, flashAlpha);
    }

    // Called by PlayerController when the player is hit.
    public void FlashDamage()
    {
        flashAlpha = 0.4f;
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("HUD Canvas");
        canvasGO.transform.SetParent(transform);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Full-screen red flash (behind the text).
        GameObject flashGO = new GameObject("DamageFlash");
        flashGO.transform.SetParent(canvasGO.transform, false);
        damageFlash = flashGO.AddComponent<Image>();
        damageFlash.color = new Color(1f, 0f, 0f, 0f);
        damageFlash.raycastTarget = false;
        RectTransform fr = damageFlash.rectTransform;
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fr.offsetMin = Vector2.zero;
        fr.offsetMax = Vector2.zero;

        // Status text, top-left.
        statusText = CreateText(canvasGO.transform, font, 34, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(25f, -25f), new Vector2(900f, 400f));

        // Win / game-over message, centre.
        messageText = CreateText(canvasGO.transform, font, 90, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 400f));
        messageText.color = Color.yellow;

        // Crosshair.
        Text cross = CreateText(canvasGO.transform, font, 48, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f));
        cross.text = "+";
    }

    private Text CreateText(Transform parent, Font font, int size, TextAnchor align,
                            Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 sizeDelta)
    {
        GameObject go = new GameObject("Text");
        go.transform.SetParent(parent, false);

        Text t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rt = t.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = Color.black;
        return t;
    }
}
