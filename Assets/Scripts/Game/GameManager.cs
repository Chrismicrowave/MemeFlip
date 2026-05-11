using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public Board board;
    public ActionPanel actionPanel;
    public ReelHoverPopup hoverPopup;

    [Header("State")]
    public TurnPhase currentPhase = TurnPhase.PlayerSelectFirst;

    Reel _firstSelected;
    Reel _secondSelected;
    int _playerShuffleCharges = 2;

    void Awake() => Instance = this;

    void Start()
    {
        board.Initialize();
        actionPanel.gameObject.SetActive(true);
        hoverPopup.gameObject.SetActive(true);
        hoverPopup.Hide();
        RefreshUI();
        actionPanel.SetTurnText("Your Turn");
    }

    public void OnReelClicked(Reel reel)
    {
        switch (currentPhase)
        {
            case TurnPhase.PlayerSelectFirst:
                HandleSelectFirst(reel);
                break;
            case TurnPhase.PlayerSelectSecond:
                HandleSelectSecond(reel);
                break;
        }
    }

    void HandleSelectFirst(Reel reel)
    {
        if (!reel.isFaceDown) return;

        if (reel.owner != Owner.Player)
        {
            actionPanel.ShowMessage("Pick one of your own reels first!");
            return;
        }

        _firstSelected = reel;
        _firstSelected.FlipUp();
        currentPhase = TurnPhase.PlayerActionChoice;
        RefreshUI();
        actionPanel.ShowAttackButton(true);
        actionPanel.ShowShuffleButton(true);
        actionPanel.SetInstructionText("Attack or Shuffle?");
    }

    void HandleSelectSecond(Reel reel)
    {
        if (!reel.isFaceDown || reel == _firstSelected) return;

        if (reel.owner != Owner.NPC)
        {
            actionPanel.ShowMessage("Pick an opponent's reel as target!");
            return;
        }

        _secondSelected = reel;
        _secondSelected.FlipUp();
        currentPhase = TurnPhase.Resolving;
        actionPanel.ShowAttackButton(false);
        actionPanel.ShowShuffleButton(false);
        actionPanel.ShowSecondSlot(_secondSelected);
        ResolveAttack(_firstSelected, _secondSelected);
    }

    public void OnAttackClicked()
    {
        if (currentPhase != TurnPhase.PlayerActionChoice) return;
        currentPhase = TurnPhase.PlayerSelectSecond;
        actionPanel.ShowAttackButton(false);
        actionPanel.ShowShuffleButton(false);
        actionPanel.SetInstructionText("Select an opponent's reel as target");
    }

    public void OnShuffleClicked()
    {
        if (currentPhase != TurnPhase.PlayerActionChoice || _playerShuffleCharges <= 0) return;

        _playerShuffleCharges--;

        if (_firstSelected != null && !_firstSelected.isDestroyed)
            _firstSelected.FlipDown();
        _firstSelected = null;

        board.ShuffleOwnerReels(Owner.Player);
        RefreshUI();
        actionPanel.ShowMessage($"Shuffle! {_playerShuffleCharges} charges left");
        actionPanel.ShowAttackButton(false);
        actionPanel.ShowShuffleButton(false);

        StartNPCTurn();
    }

    void ResolveAttack(Reel attacker, Reel target)
    {
        int damage = Mathf.Max(1, attacker.stats.atk - target.stats.def);
        target.stats.currentHP -= damage;

        string msg = $"{attacker.owner} ATK({attacker.stats.atk}) → {target.owner} DEF({target.stats.def}) = {damage} dmg!";
        if (target.stats.currentHP <= 0)
        {
            target.DestroyReel();
            msg = $"{damage} dmg — DESTROYED!";
        }

        actionPanel.ShowMessage(msg);
        actionPanel.UpdateHpBars();

        CheckWinCondition();
    }

    void CheckWinCondition()
    {
        if (!board.HasAliveReels(Owner.NPC))
        {
            currentPhase = TurnPhase.GameOver;
            actionPanel.ShowGameOver("You Win!");
            return;
        }

        if (!board.HasAliveReels(Owner.Player))
        {
            currentPhase = TurnPhase.GameOver;
            actionPanel.ShowGameOver("NPC Wins!");
            return;
        }

        Invoke(nameof(FinishPlayerTurn), 1.5f);
    }

    void FinishPlayerTurn()
    {
        FlipBackSelections();
        currentPhase = TurnPhase.NPCTurn;
        RefreshUI();
        StartNPCTurn();
    }

    void StartNPCTurn()
    {
        currentPhase = TurnPhase.NPCTurn;
        actionPanel.SetTurnText("NPC Turn");
        actionPanel.ShowAttackButton(false);
        actionPanel.ShowShuffleButton(false);
        Invoke(nameof(ExecuteNPCTurn), 0.8f);
    }

    void ExecuteNPCTurn()
    {
        var npcAttackers = board.GetAliveFaceDown(Owner.NPC);
        var playerTargets = board.GetAliveFaceDown(Owner.Player);

        if (npcAttackers.Count == 0 || playerTargets.Count == 0)
        {
            CheckWinCondition();
            return;
        }

        Reel npcPick = npcAttackers[Random.Range(0, npcAttackers.Count)];
        Reel targetPick = playerTargets[Random.Range(0, playerTargets.Count)];

        _firstSelected = npcPick;
        _secondSelected = targetPick;

        _firstSelected.FlipUp();
        actionPanel.ShowFirstSlot(_firstSelected);
        Invoke(nameof(NPCSecondFlip), 0.8f);
    }

    void NPCSecondFlip()
    {
        _secondSelected.FlipUp();
        actionPanel.ShowSecondSlot(_secondSelected);
        Invoke(nameof(NPCResolve), 0.6f);
    }

    void NPCResolve()
    {
        int damage = Mathf.Max(1, _firstSelected.stats.atk - _secondSelected.stats.def);
        _secondSelected.stats.currentHP -= damage;

        string msg = $"NPC ATK({_firstSelected.stats.atk}) → {damage} dmg!";
        if (_secondSelected.stats.currentHP <= 0)
        {
            _secondSelected.DestroyReel();
            msg = $"NPC {damage} dmg — Your reel DESTROYED!";
        }

        actionPanel.ShowMessage(msg);
        actionPanel.UpdateHpBars();

        Invoke(nameof(FinishNPCTurn), 1.5f);
    }

    void FinishNPCTurn()
    {
        FlipBackSelections();

        if (!board.HasAliveReels(Owner.Player))
        {
            currentPhase = TurnPhase.GameOver;
            actionPanel.ShowGameOver("NPC Wins!");
            return;
        }

        currentPhase = TurnPhase.PlayerSelectFirst;
        actionPanel.SetTurnText("Your Turn");
        actionPanel.ShowShuffleButton(true);
        RefreshUI();
    }

    void FlipBackSelections()
    {
        if (_firstSelected != null && !_firstSelected.isDestroyed)
            _firstSelected.FlipDown();
        if (_secondSelected != null && !_secondSelected.isDestroyed)
            _secondSelected.FlipDown();
        _firstSelected = null;
        _secondSelected = null;
    }

    void RefreshUI()
    {
        actionPanel.ClearSlots();
        actionPanel.SetShuffleCharges(_playerShuffleCharges);
        actionPanel.UpdateHpBars();
        actionPanel.SetInstructionText("");
    }

    public int PlayerShuffleCharges => _playerShuffleCharges;
    public Reel FirstSelected => _firstSelected;
    public Reel SecondSelected => _secondSelected;
}

public enum TurnPhase
{
    PlayerSelectFirst,
    PlayerActionChoice,
    PlayerSelectSecond,
    Resolving,
    NPCTurn,
    GameOver
}
