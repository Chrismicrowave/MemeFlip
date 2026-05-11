using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionPanel : MonoBehaviour
{
    [Header("Slot 1 (Attacker)")]
    public GameObject slot1Panel;
    public TextMeshProUGUI slot1Owner;
    public TextMeshProUGUI slot1Stats;

    [Header("Slot 2 (Target)")]
    public GameObject slot2Panel;
    public TextMeshProUGUI slot2Owner;
    public TextMeshProUGUI slot2Stats;

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
        ClearSlots();
        ShowAttackButton(false);
        ShowShuffleButton(false);
        gameOverRoot.SetActive(false);
        messageRoot.SetActive(false);
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
    }

    public void ShowFirstSlot(Reel reel)
    {
        slot1Panel.SetActive(true);
        Color c = reel.owner == Owner.Player ? reel.playerColor : reel.npcColor;
        slot1Owner.text = $"<color=#{(byte)(c.r*255):X2}{(byte)(c.g*255):X2}{(byte)(c.b*255):X2}>{reel.owner}</color>";
        slot1Stats.text = $"HP {reel.stats.currentHP}/{reel.stats.maxHP}  ATK {reel.stats.atk}  DEF {reel.stats.def}";
    }

    public void ShowSecondSlot(Reel reel)
    {
        slot2Panel.SetActive(true);
        Color c = reel.owner == Owner.Player ? reel.playerColor : reel.npcColor;
        slot2Owner.text = $"<color=#{(byte)(c.r*255):X2}{(byte)(c.g*255):X2}{(byte)(c.b*255):X2}>{reel.owner}</color>";
        slot2Stats.text = $"HP {reel.stats.currentHP}/{reel.stats.maxHP}  ATK {reel.stats.atk}  DEF {reel.stats.def}";
    }

    public void ClearSlots()
    {
        slot1Panel.SetActive(false);
        slot2Panel.SetActive(false);
    }

    public void ShowAttackButton(bool show) => attackButton.gameObject.SetActive(show);
    public void ShowShuffleButton(bool show) => shuffleButton.gameObject.SetActive(show);

    public void SetShuffleCharges(int charges)
    {
        shuffleLabel.text = $"Shuffle ({charges})";
        shuffleButton.interactable = charges > 0;
    }

    public void SetTurnText(string text) => turnText.text = text;
    public void SetInstructionText(string text) => instructionText.text = text;

    public void ShowMessage(string msg)
    {
        messageRoot.SetActive(true);
        messageText.text = msg;
        CancelInvoke(nameof(HideMessage));
        Invoke(nameof(HideMessage), 2.5f);
    }

    void HideMessage() => messageRoot.SetActive(false);

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
        gameOverRoot.SetActive(true);
        gameOverLabel.text = text;
    }

    void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
