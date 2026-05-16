using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionPanel : MonoBehaviour
{
    [Header("Slots")]
    public GameObject playerSlot1;
    public TextMeshProUGUI playerSlot1Owner;
    public TextMeshProUGUI playerSlot1Stats;
    public RawImage playerSlot1Image;
    public GameObject playerSlot2;
    public TextMeshProUGUI playerSlot2Owner;
    public TextMeshProUGUI playerSlot2Stats;
    public RawImage playerSlot2Image;

    [Header("Buttons")]
    public Button attackButton;
    public Button shuffleButton;
    public TextMeshProUGUI shuffleLabel;

    [Header("Status")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI instructionText;

    [Header("Message")]
    public GameObject messageRoot;
    public TextMeshProUGUI messageText;

    [Header("HP Summary")]
    public TextMeshProUGUI playerHpLabel;
    public TextMeshProUGUI npcHpLabel;

    [Header("Game Over")]
    public GameObject gameOverRoot;
    public TextMeshProUGUI gameOverLabel;
    public Button restartButton;

    void Start()
    {
        Transform c = transform;

        if (turnText == null) turnText = c.Find("TurnText")?.GetComponent<TextMeshProUGUI>();
        if (instructionText == null) instructionText = c.Find("InstructionText")?.GetComponent<TextMeshProUGUI>();
        if (playerHpLabel == null) playerHpLabel = c.Find("PlayerHp")?.GetComponent<TextMeshProUGUI>();
        if (npcHpLabel == null) npcHpLabel = c.Find("NpcHp")?.GetComponent<TextMeshProUGUI>();

        if (playerSlot1 == null) playerSlot1 = c.Find("PlayerSlotPanel1")?.gameObject;
        if (playerSlot1Owner == null) playerSlot1Owner = c.Find("PlayerSlotPanel1/OwnerLabel")?.GetComponent<TextMeshProUGUI>();
        if (playerSlot1Stats == null) playerSlot1Stats = c.Find("PlayerSlotPanel1/StatsLabel")?.GetComponent<TextMeshProUGUI>();
        if (playerSlot1Image == null) playerSlot1Image = c.Find("PlayerSlotPanel1/Image")?.GetComponent<RawImage>();
        if (playerSlot2 == null) playerSlot2 = c.Find("PlayerSlotPanel2")?.gameObject;
        if (playerSlot2Owner == null) playerSlot2Owner = c.Find("PlayerSlotPanel2/OwnerLabel")?.GetComponent<TextMeshProUGUI>();
        if (playerSlot2Stats == null) playerSlot2Stats = c.Find("PlayerSlotPanel2/StatsLabel")?.GetComponent<TextMeshProUGUI>();
        if (playerSlot2Image == null) playerSlot2Image = c.Find("PlayerSlotPanel2/Image")?.GetComponent<RawImage>();

        if (attackButton == null) attackButton = c.Find("AttackButton")?.GetComponent<Button>();
        if (shuffleButton == null) shuffleButton = c.Find("ShuffleButton")?.GetComponent<Button>();
        if (shuffleLabel == null) shuffleLabel = c.Find("ShuffleButton/Label")?.GetComponent<TextMeshProUGUI>();

        if (messageRoot == null) messageRoot = c.Find("MessageRoot")?.gameObject;
        if (messageText == null) messageText = c.Find("MessageRoot/MessageText")?.GetComponent<TextMeshProUGUI>();

        if (gameOverRoot == null) gameOverRoot = c.Find("GameOverRoot")?.gameObject;
        if (gameOverLabel == null) gameOverLabel = c.Find("GameOverRoot/GameOverLabel")?.GetComponent<TextMeshProUGUI>();
        if (restartButton == null) restartButton = c.Find("GameOverRoot/RestartButton")?.GetComponent<Button>();

        ClearSlots();
        ShowAttackButton(false);
        ShowShuffleButton(false);
        if (messageRoot != null) messageRoot.SetActive(false);
        if (gameOverRoot != null) gameOverRoot.SetActive(false);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
    }

    static string ColorTag(Color c)
    {
        return $"#{(byte)(c.r * 255):X2}{(byte)(c.g * 255):X2}{(byte)(c.b * 255):X2}";
    }

    static void FillSlot(GameObject panel, TextMeshProUGUI ownerLabel, TextMeshProUGUI statsLabel, Reel reel)
    {
        panel.SetActive(true);
        Color c = reel.owner == Owner.Player ? reel.playerColor : reel.npcColor;
        ownerLabel.text = $"<color={ColorTag(c)}>{reel.owner}</color>";
        statsLabel.text = $"HP {reel.stats.currentHP}/{reel.stats.maxHP}  ATK {reel.stats.atk}  DEF {reel.stats.def}";
    }

    public void ShowAttackerSlot(Reel reel)
    {
        FillSlot(playerSlot1, playerSlot1Owner, playerSlot1Stats, reel);
        SetSlotImage(playerSlot1Image, reel);
    }

    public void ShowTargetSlot(Reel target, Reel attacker)
    {
        FillSlot(playerSlot2, playerSlot2Owner, playerSlot2Stats, target);
        SetSlotImage(playerSlot2Image, target);
    }

    void SetSlotImage(RawImage target, Reel reel)
    {
        if (target == null || reel?.memeData == null) return;
        // Set static image preview (video will be routed by MemePlayer if available)
        if (reel.memeData.memeImage != null)
            target.texture = reel.memeData.memeImage;
    }

    public void ClearSlots()
    {
        if (playerSlot1 != null) playerSlot1.SetActive(false);
        if (playerSlot2 != null) playerSlot2.SetActive(false);
        if (playerSlot1Image != null) playerSlot1Image.texture = null;
        if (playerSlot2Image != null) playerSlot2Image.texture = null;
    }

    public void ShowAttackButton(bool show)
    {
        if (attackButton != null) attackButton.gameObject.SetActive(show);
    }

    public void ShowShuffleButton(bool show)
    {
        if (shuffleButton != null) shuffleButton.gameObject.SetActive(show);
    }

    public void SetShuffleCharges(int charges)
    {
        if (shuffleLabel != null) shuffleLabel.text = $"Shuffle ({charges})";
        if (shuffleButton != null) shuffleButton.interactable = charges > 0;
    }

    public void SetTurnText(string text)
    {
        if (turnText != null) turnText.text = text;
    }

    public void SetInstructionText(string text)
    {
        if (instructionText != null) instructionText.text = text;
    }

    public void ShowMessage(string msg)
    {
        if (messageRoot != null) messageRoot.SetActive(true);
        if (messageText != null) messageText.text = msg;
        CancelInvoke(nameof(HideMessage));
        Invoke(nameof(HideMessage), 2.5f);
    }

    void HideMessage()
    {
        if (messageRoot != null) messageRoot.SetActive(false);
    }

    public void UpdateHpBars()
    {
        var board = GameManager.Instance.board;
        int pAlive = board.AliveCount(Owner.Player);
        int nAlive = board.AliveCount(Owner.NPC);
        playerHpLabel.text = $"Your reels: {pAlive}/6";
        npcHpLabel.text = $"NPC reels: {nAlive}/6";
    }

    public void ShowGameOver(string text)
    {
        if (gameOverRoot != null) gameOverRoot.SetActive(true);
        if (gameOverLabel != null) gameOverLabel.text = text;
    }

    void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
