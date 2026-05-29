using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionPanel : MonoBehaviour
{
    [Header("Scene Slot Positions")]
    [Tooltip("World-space transform that the flying reel clone targets for Slot1")]
    public Transform slot1ScenePos;
    [Tooltip("World-space transform that the flying reel clone targets for Slot2")]
    public Transform slot2ScenePos;

    [Header("Slots")]
    public GameObject playerSlot1;
    public TextMeshProUGUI playerSlot1Owner;
    public TextMeshProUGUI playerSlot1HP;
    public TextMeshProUGUI playerSlot1ATK;
    public RawImage playerSlot1Image;
    public Scrollbar playerSlot1HPBar;
    public Image playerSlot1OwnerColour;
    public GameObject playerSlot2;
    public TextMeshProUGUI playerSlot2Owner;
    public TextMeshProUGUI playerSlot2HP;
    public TextMeshProUGUI playerSlot2ATK;
    public RawImage playerSlot2Image;
    public Scrollbar playerSlot2HPBar;
    public Image playerSlot2OwnerColour;

    [Header("Buttons")]
    public Button attackButton;
    public Button shuffleButton;
    public TextMeshProUGUI shuffleLabel;
    public Button shuffleButtonP2;
    public TextMeshProUGUI shuffleLabelP2;

    [Header("Turn Panels")]
    public GameObject turnPanelP1;
    public GameObject turnPanelP2NPC;
    public TextMeshProUGUI turnTextP1;
    public TextMeshProUGUI turnTextP2NPC;
    public string turnLabelYourTurn = "Your Turn";
    public string turnLabelP1Turn = "Player 1's Turn";
    public string turnLabelP2Turn = "Player 2's Turn";
    public string turnLabelNPCTurn = "NPC Turn";

    [Header("Slot Labels")]
    public TextMeshProUGUI slot1Label;
    public TextMeshProUGUI slot2Label;
    public string slotLabelAttacker = "Attacker";
    public string slotLabelTarget = "Target";

    [Header("Status")]
    [System.Obsolete("Use turnPanelP1/turnPanelP2NPC instead")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI messagePanel;

    [Header("Instruction Messages")]
    public string instructionPickReel = "Pick any reel";
    public string instructionSelectTarget = "Pick a second reel";
    public string instructionClickOutside = "Click anywhere outside the board to proceed";
    public string msgNoAttack = "same team — no attack happens";
    public string msgInvalidAttackerPick = "{0}'s reel revealed — can't assign to attacker slot. Pick another reel to explore the board, or shuffle all reels.";

    [Header("HP Summary")]
    public TextMeshProUGUI playerHpLabel;
    public TextMeshProUGUI npcHpLabel;

    [Header("Game Over")]
    public GameObject gameOverRoot;
    public TextMeshProUGUI gameOverLabel;
    public Button restartButton;
    public string gameOverYouWin = "You Win!";
    public string gameOverNPCWins = "NPC Wins!";
    public string gameOverP1Wins = "Player 1 Wins!";
    public string gameOverP2Wins = "Player 2 Wins!";

    void Awake()
    {
        Transform c = transform;

        if (turnText == null) turnText = c.Find("TurnText")?.GetComponent<TextMeshProUGUI>();
        if (messagePanel == null) messagePanel = c.Find("MessagePanel/MessageText")?.GetComponent<TextMeshProUGUI>();
        if (playerHpLabel == null) playerHpLabel = c.Find("PlayerHp")?.GetComponent<TextMeshProUGUI>();
        if (npcHpLabel == null) npcHpLabel = c.Find("NpcHp")?.GetComponent<TextMeshProUGUI>();

        if (playerSlot1 == null) playerSlot1 = c.Find("PlayerSlotPanel1")?.gameObject;
        if (playerSlot1Owner == null) playerSlot1Owner = c.Find("PlayerSlotPanel1/MemesName")?.GetComponent<TextMeshProUGUI>();
        if (playerSlot1HP == null) playerSlot1HP = c.Find("PlayerSlotPanel1/HP")?.GetComponent<TextMeshProUGUI>();
        if (playerSlot1ATK == null) playerSlot1ATK = c.Find("PlayerSlotPanel1/ATK")?.GetComponent<TextMeshProUGUI>();
        if (playerSlot1Image == null) playerSlot1Image = c.Find("PlayerSlotPanel1/Image")?.GetComponent<RawImage>();
        if (playerSlot1HPBar == null) playerSlot1HPBar = c.Find("PlayerSlotPanel1/HPBar")?.GetComponent<Scrollbar>();
        if (playerSlot1OwnerColour == null) playerSlot1OwnerColour = c.Find("PlayerSlotPanel1/OwnerColour")?.GetComponent<Image>();
        if (playerSlot2 == null) playerSlot2 = c.Find("PlayerSlotPanel2")?.gameObject;
        if (playerSlot2Owner == null) playerSlot2Owner = c.Find("PlayerSlotPanel2/MemesName")?.GetComponent<TextMeshProUGUI>();
        if (playerSlot2HP == null) playerSlot2HP = c.Find("PlayerSlotPanel2/HP")?.GetComponent<TextMeshProUGUI>();
        if (playerSlot2ATK == null) playerSlot2ATK = c.Find("PlayerSlotPanel2/ATK")?.GetComponent<TextMeshProUGUI>();
        if (playerSlot2Image == null) playerSlot2Image = c.Find("PlayerSlotPanel2/Image")?.GetComponent<RawImage>();
        if (playerSlot2HPBar == null) playerSlot2HPBar = c.Find("PlayerSlotPanel2/HPBar")?.GetComponent<Scrollbar>();
        if (playerSlot2OwnerColour == null) playerSlot2OwnerColour = c.Find("PlayerSlotPanel2/OwnerColour")?.GetComponent<Image>();

        if (attackButton == null) attackButton = c.Find("AttackButton")?.GetComponent<Button>();
        if (shuffleButton == null) shuffleButton = c.Find("ShuffleButton")?.GetComponent<Button>();
        if (shuffleLabel == null) shuffleLabel = c.Find("ShuffleButton/Label")?.GetComponent<TextMeshProUGUI>();
        if (shuffleButtonP2 == null) shuffleButtonP2 = c.Find("ShuffleButtonP2")?.GetComponent<Button>();
        if (shuffleLabelP2 == null) shuffleLabelP2 = c.Find("ShuffleButtonP2/Label")?.GetComponent<TextMeshProUGUI>();

        if (gameOverRoot == null) gameOverRoot = c.Find("GameOverRoot")?.gameObject;
        if (gameOverLabel == null) gameOverLabel = c.Find("GameOverRoot/GameOverLabel")?.GetComponent<TextMeshProUGUI>();
        if (restartButton == null) restartButton = c.Find("GameOverRoot/RestartButton")?.GetComponent<Button>();

        if (turnPanelP1 == null) turnPanelP1 = c.Find("TurnPrompt P1")?.gameObject;
        if (turnPanelP2NPC == null) turnPanelP2NPC = c.Find("TurnPrompt P2NPC")?.gameObject;
        if (turnTextP1 == null && turnPanelP1 != null) turnTextP1 = turnPanelP1.GetComponentInChildren<TextMeshProUGUI>();
        if (turnTextP2NPC == null && turnPanelP2NPC != null) turnTextP2NPC = turnPanelP2NPC.GetComponentInChildren<TextMeshProUGUI>();

        if (slot1Label == null) slot1Label = c.Find("Slot1Box/Slot1Label")?.GetComponent<TextMeshProUGUI>();
        if (slot2Label == null) slot2Label = c.Find("Slot2Box/Slot2Label")?.GetComponent<TextMeshProUGUI>();

        CacheHandleColors();
    }

    void Start()
    {
        ClearSlots();
        ShowAttackButton(false);
        // Shuffle visibility managed by GameManager
        if (gameOverRoot != null) gameOverRoot.SetActive(false);
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }
    }

    static string ColorTag(Color c)
    {
        return $"#{(byte)(c.r * 255):X2}{(byte)(c.g * 255):X2}{(byte)(c.b * 255):X2}";
    }

    static string MemeName(Reel reel)
    {
        var md = reel?.memeData;
        if (md == null) return "???";
        string name;
        if (md.memeVideo != null) name = md.memeVideo.name;
        else if (md.memeImage != null) name = md.memeImage.name;
        else return "Unknown";
        return $"{name}";
    }

    static void FillSlot(GameObject panel, TextMeshProUGUI ownerLabel, TextMeshProUGUI hpLabel, TextMeshProUGUI atkLabel, Reel reel, Scrollbar hpBar, Image ownerColour)
    {
        panel.SetActive(true);
        ownerLabel.text = MemeName(reel);
        if (hpLabel != null) hpLabel.text = $"{reel.stats.currentHP}/{reel.stats.maxHP}";
        if (atkLabel != null) atkLabel.text = $"ATK: {reel.stats.atk}";
        if (hpBar != null) hpBar.size = (float)reel.stats.currentHP / reel.stats.maxHP;
        //Color c = reel.owner == Owner.Player ? reel.playerColor : reel.npcColor;
        //if (ownerColour != null) ownerColour.color = c;
    }

    public void ShowP1Slot(Reel reel)
    {
        FillSlot(playerSlot1, playerSlot1Owner, playerSlot1HP, playerSlot1ATK, reel, playerSlot1HPBar, null); // owner colour disabled
        SetSlotImage(playerSlot1Image, reel);
    }

    public void ShowP2Slot(Reel reel)
    {
        FillSlot(playerSlot2, playerSlot2Owner, playerSlot2HP, playerSlot2ATK, reel, playerSlot2HPBar, null); // owner colour disabled
        SetSlotImage(playerSlot2Image, reel);
    }

    void SetSlotImage(RawImage target, Reel reel)
    {
        if (target == null || reel?.memeData == null) return;
        // Only set static image for image-only memes — video memes are handled by PlaySlot
        if (reel.memeData.memeVideo != null) return;
        if (reel.memeData.memeImage != null)
            target.texture = reel.memeData.memeImage;
    }

    public void ClearSlots()
    {
        if (playerSlot1 != null) playerSlot1.SetActive(false);
        if (playerSlot2 != null) playerSlot2.SetActive(false);
        if (playerSlot1Image != null) playerSlot1Image.texture = null;
        if (playerSlot2Image != null) playerSlot2Image.texture = null;
        if (playerSlot1HPBar != null) playerSlot1HPBar.size = 0f;
        if (playerSlot2HPBar != null) playerSlot2HPBar.size = 0f;
        //if (playerSlot1OwnerColour != null) playerSlot1OwnerColour.color = Color.gray;
        //if (playerSlot2OwnerColour != null) playerSlot2OwnerColour.color = Color.gray;
        if (playerSlot1HP != null) playerSlot1HP.text = "";
        if (playerSlot1ATK != null) playerSlot1ATK.text = "";
        if (playerSlot2HP != null) playerSlot2HP.text = "";
        if (playerSlot2ATK != null) playerSlot2ATK.text = "";
        // Restore handle colours (green etc.) for next slot display
        SetHandleColor(playerSlot1HPBar, _handleOrgColor1);
        SetHandleColor(playerSlot2HPBar, _handleOrgColor2);
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

    public void ShowShuffleButtonP2(bool show)
    {
        if (shuffleButtonP2 != null) shuffleButtonP2.gameObject.SetActive(show);
    }

    public void SetShuffleChargesP2(int charges)
    {
        if (shuffleLabelP2 != null) shuffleLabelP2.text = $"Shuffle ({charges})";
        if (shuffleButtonP2 != null) shuffleButtonP2.interactable = charges > 0;
    }

    public void SetTurnText(string text)
    {
        if (turnText != null) turnText.text = text;
    }

    public void ShowTurnPanelP1(string text)
    {
        if (turnPanelP1 != null) turnPanelP1.SetActive(true);
        if (turnPanelP2NPC != null) turnPanelP2NPC.SetActive(false);
        if (turnTextP1 != null) turnTextP1.text = text;
    }

    public void ShowTurnPanelP2NPC(string text)
    {
        if (turnPanelP1 != null) turnPanelP1.SetActive(false);
        if (turnPanelP2NPC != null) turnPanelP2NPC.SetActive(true);
        if (turnTextP2NPC != null) turnTextP2NPC.text = text;
    }

    public void SetMessageText(string text)
    {
        if (messagePanel != null) messagePanel.text = text;
    }

    public void UpdateSlotLabels(Owner currentPlayer)
    {
        if (slot1Label != null)
            slot1Label.text = currentPlayer == Owner.Player ? slotLabelAttacker : slotLabelTarget;
        if (slot2Label != null)
            slot2Label.text = currentPlayer == Owner.Player ? slotLabelTarget : slotLabelAttacker;
    }

    public static string GetNoAttackMessage()
    {
        return "same team — no attack";
    }

    public void UpdateHpBars()
    {
        var board = GameManager.Instance.board;
        int pAlive = board.AliveCount(Owner.Player);
        int nAlive = board.AliveCount(Owner.NPC);
        bool isVsPlayer = GameManager.Instance.gameMode == GameManager.GameMode.VsPlayer;
        playerHpLabel.text = isVsPlayer ? $"P1 reels: {pAlive}/6" : $"Your reels: {pAlive}/6";
        npcHpLabel.text = isVsPlayer ? $"P2 reels: {nAlive}/6" : $"NPC reels: {nAlive}/6";

        // Resolve which reel belongs in each slot by owner (matches ShowSlotAfterAnimation logic)
        var fs = GameManager.Instance.FirstSelected;
        var ss = GameManager.Instance.SecondSelected;
        Reel slot1Reel = fs != null && fs.owner == Owner.Player ? fs : (ss != null && ss.owner == Owner.Player ? ss : null);
        Reel slot2Reel = fs != null && fs.owner != Owner.Player ? fs : (ss != null && ss.owner != Owner.Player ? ss : null);

        UpdateSlotDisplay(playerSlot1HPBar, playerSlot1HP, playerSlot1ATK, slot1Reel);
        UpdateSlotDisplay(playerSlot2HPBar, playerSlot2HP, playerSlot2ATK, slot2Reel);
    }

    Color _handleOrgColor1, _handleOrgColor2;
    Color _barBgColor1, _barBgColor2;

    void CacheHandleColors()
    {
        _handleOrgColor1 = GetHandleColor(playerSlot1HPBar);
        _handleOrgColor2 = GetHandleColor(playerSlot2HPBar);
        _barBgColor1 = GetBarBgColor(playerSlot1HPBar);
        _barBgColor2 = GetBarBgColor(playerSlot2HPBar);
    }

    static Color GetHandleColor(Scrollbar bar)
    {
        if (bar == null || bar.handleRect == null) return Color.white;
        var img = bar.handleRect.GetComponent<Image>();
        return img != null ? img.color : Color.white;
    }

    static Color GetBarBgColor(Scrollbar bar)
    {
        if (bar == null) return Color.gray;
        // The Scrollbar's own Image is the background track
        var bg = bar.GetComponent<Image>();
        return bg != null ? bg.color : Color.gray;
    }

    static void SetHandleColor(Scrollbar bar, Color c)
    {
        if (bar == null || bar.handleRect == null) return;
        var img = bar.handleRect.GetComponent<Image>();
        if (img != null) img.color = c;
    }

    static void UpdateSlotDisplay(Scrollbar bar, TextMeshProUGUI hpLabel, TextMeshProUGUI atkLabel, Reel reel)
    {
        if (reel == null || reel.isDestroyed)
        {
            if (bar != null) bar.size = 0f;
            if (hpLabel != null) hpLabel.text = reel != null ? $"0/{reel.stats.maxHP}" : "0/0";
            if (reel != null && reel.isDestroyed)
                SetHandleColor(bar, GetBarBgColor(bar));
            return;
        }
        if (bar != null) bar.size = (float)reel.stats.currentHP / reel.stats.maxHP;
        if (hpLabel != null) hpLabel.text = $"{reel.stats.currentHP}/{reel.stats.maxHP}";
        if (atkLabel != null) atkLabel.text = $"ATK: {reel.stats.atk}";
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
