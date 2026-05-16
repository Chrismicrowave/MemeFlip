using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionPanel : MonoBehaviour
{
    [Header("Player Slots")]
    public GameObject playerSlot1;
    public TextMeshProUGUI playerSlot1Owner;
    public TextMeshProUGUI playerSlot1Stats;

    [Header("NPC Slots")]
    public GameObject npcSlot1;
    public TextMeshProUGUI npcSlot1Owner;
    public TextMeshProUGUI npcSlot1Stats;

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

        if (npcSlot1 == null) npcSlot1 = c.Find("NPCSlotPanel1")?.gameObject;
        if (npcSlot1Owner == null) npcSlot1Owner = c.Find("NPCSlotPanel1/OwnerLabel")?.GetComponent<TextMeshProUGUI>();
        if (npcSlot1Stats == null) npcSlot1Stats = c.Find("NPCSlotPanel1/StatsLabel")?.GetComponent<TextMeshProUGUI>();

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
        if (reel.owner == Owner.Player)
            FillSlot(playerSlot1, playerSlot1Owner, playerSlot1Stats, reel);
        else
            FillSlot(npcSlot1, npcSlot1Owner, npcSlot1Stats, reel);
    }

    public void ShowTargetSlot(Reel target, Reel attacker)
    {
        if (target.owner == Owner.Player)
            FillSlot(playerSlot1, playerSlot1Owner, playerSlot1Stats, target);
        else
            FillSlot(npcSlot1, npcSlot1Owner, npcSlot1Stats, target);
    }

    public void ClearSlots()
    {
        if (playerSlot1 != null) playerSlot1.SetActive(false);
        if (npcSlot1 != null) npcSlot1.SetActive(false);
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
