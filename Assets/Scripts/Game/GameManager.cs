using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameMode { VsNPC, VsPlayer }

    [Header("References")]
    public Board board;
    public DOTweenManager dotweenManager;
    public ActionPanel actionPanel;
    public ReelHoverPopup hoverPopup;
    public MemePlayer memePlayer;

    [Header("Mode")]
    public GameMode gameMode = GameMode.VsNPC;

    [Header("State")]
    public TurnPhase currentPhase = TurnPhase.PlayerSelectFirst;

    Reel _firstSelected;
    Reel _secondSelected;
    Reel _hoveredReel;
    int _playerShuffleCharges = 2;
    int _playerTwoShuffleCharges = 2;
    readonly System.Collections.Generic.List<Reel> _revealedReels = new();
    Owner _currentPlayer = Owner.Player;
    Owner Opponent => _currentPlayer == Owner.Player ? Owner.NPC : Owner.Player;
    bool _resultFromNpcTurn;
    bool _attackResolved;
    bool _firstPickWasInvalid;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        board.Initialize();
        actionPanel.gameObject.SetActive(true);
        hoverPopup.gameObject.SetActive(true);
        hoverPopup.Hide();
        WireButtons();
        ShowCurrentPlayerButtons();
        RefreshUI();
    }

    void WireButtons()
    {
        if (actionPanel.shuffleButton != null)
        {
            actionPanel.shuffleButton.onClick.RemoveAllListeners();
            actionPanel.shuffleButton.onClick.AddListener(OnShuffleClicked);
        }
        if (actionPanel.shuffleButtonP2 != null)
        {
            actionPanel.shuffleButtonP2.onClick.RemoveAllListeners();
            actionPanel.shuffleButtonP2.onClick.AddListener(OnShuffleClicked);
        }
    }

    void ShowCurrentPlayerButtons()
    {
        if (gameMode == GameMode.VsPlayer)
        {
            bool isP1 = _currentPlayer == Owner.Player;
            actionPanel.ShowShuffleButton(isP1);
            actionPanel.ShowShuffleButtonP2(!isP1);
            if (isP1)
                actionPanel.ShowTurnPanelP1(actionPanel.turnLabelP1Turn);
            else
                actionPanel.ShowTurnPanelP2NPC(actionPanel.turnLabelP2Turn);
        }
        else
        {
            actionPanel.ShowShuffleButton(true);
            actionPanel.ShowShuffleButtonP2(false);
            actionPanel.ShowTurnPanelP1(actionPanel.turnLabelYourTurn);
        }
        actionPanel.ShowAttackButton(false);
    }

    void HideAllShuffleButtons()
    {
        actionPanel.ShowShuffleButton(false);
        actionPanel.ShowShuffleButtonP2(false);
    }

    void Update()
    {
        bool clicked = Mouse.current.leftButton.wasPressedThisFrame;

        Camera cam = Camera.main;
        if (cam == null) return;

        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        Reel hitReel = null;
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit);
        if (hitSomething)
            hitReel = hit.collider.GetComponentInParent<Reel>();

        // Hover enter/exit
        if (hitReel != _hoveredReel)
        {
            if (_hoveredReel != null)
            {
                _hoveredReel.OnHoverExit();
                hoverPopup.Hide();
                memePlayer?.StopHover();
            }
            _hoveredReel = hitReel;
            if (_hoveredReel != null)
            {
                _hoveredReel.OnHoverEnter();
                hoverPopup.Show(_hoveredReel);
                memePlayer?.PlayHover(hoverPopup.previewImage, _hoveredReel);
            }
        }

        // Click
        if (clicked)
        {
            string objName = hitSomething ? hit.collider.name : "nothing";
            bool isReel = hitReel != null;
            bool faceDown = isReel && hitReel.isFaceDown;
            bool destroyed = isReel && hitReel.isDestroyed;
            Debug.Log($"[Input] Click — overUI:{overUI} | hit:{objName} | isReel:{isReel} | faceDown:{faceDown} | destroyed:{destroyed} | phase:{currentPhase}");
        }

        if (clicked && hitReel != null)
            hitReel.OnClick();

        // ShowResult — click outside board to dismiss
        if (currentPhase == TurnPhase.ShowResult && clicked && hitReel == null && !overUI)
        {
            if (gameMode == GameMode.VsPlayer)
                FinishCurrentTurn();
            else if (_resultFromNpcTurn)
                FinishNPCTurn();
            else
                FinishPlayerTurn();
        }

        // Dynamic mouse scaling on board
        if (hitSomething)
        {
            board.UpdateDynamicScales(hit.point);
        }
        else
        {
            Plane boardPlane = new(Vector3.up, board.transform.position);
            if (boardPlane.Raycast(ray, out float enter))
            {
                Vector3 pt = ray.GetPoint(enter);
                float halfW = board.gridSize.y * board.cellSpacing * 0.5f;
                float halfD = board.gridSize.x * board.cellSpacing * 0.5f;
                Vector3 local = pt - board.transform.position;
                if (Mathf.Abs(local.x) <= halfW && Mathf.Abs(local.z) <= halfD)
                    board.UpdateDynamicScales(pt);
                else
                    board.ResetDynamicScales();
            }
            else
            {
                board.ResetDynamicScales();
            }
        }
    }

    public void OnReelClicked(Reel reel)
    {
        // Replay in slot when clicking an already-flipped selected reel (always allowed)
        if (!reel.isFaceDown && !reel.isDestroyed)
        {
            if (reel == _firstSelected)
            {
                int si = reel.owner == Owner.Player ? 0 : 1;
                var img = si == 0 ? actionPanel.playerSlot1Image : actionPanel.playerSlot2Image;
                memePlayer?.PlaySlot(si, img, reel, true);
                return;
            }
            if (reel == _secondSelected)
            {
                int si = reel.owner == Owner.Player ? 0 : 1;
                var img = si == 0 ? actionPanel.playerSlot1Image : actionPanel.playerSlot2Image;
                memePlayer?.PlaySlot(si, img, reel, true);
                return;
            }
        }

        // Don't process selection clicks while showing result
        if (currentPhase == TurnPhase.ShowResult) return;

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

        Debug.Log($"[GM] Selected first: {reel.name} at {reel.boardPosition}");
        reel.FlipUp();
        RefreshHoverForReel(reel);
        _firstSelected = reel;

        if (reel.owner != _currentPlayer)
        {
            // Opponent's reel — reveal and use as invalid attacker, advance to pick 2
            _firstPickWasInvalid = true;
            if (!_revealedReels.Contains(reel))
                _revealedReels.Add(reel);
            currentPhase = TurnPhase.PlayerSelectSecond;
            actionPanel.SetMessageText(string.Format(actionPanel.msgInvalidAttackerPick, OwnerDisplayName(reel.owner)));
            return;
        }

        // Valid: own reel selected as attacker
        _firstPickWasInvalid = false;
        currentPhase = TurnPhase.PlayerSelectSecond;
        HideAllShuffleButtons();
        RefreshUI();
        int slotIdx = reel.owner == Owner.Player ? 0 : 1;
        ShowSlotAfterAnimation(_firstSelected, slotIdx);
        actionPanel.SetMessageText(actionPanel.instructionSelectTarget);
    }

    void HandleSelectSecond(Reel reel)
    {
        if (!reel.isFaceDown || reel == _firstSelected) return;

        Debug.Log($"[GM] Selected second (target): {reel.name} at {reel.boardPosition}");
        _secondSelected = reel;
        _secondSelected.FlipUp();
        RefreshHoverForReel(reel);

        // Show target in its owner's slot with fly animation (for info even if invalid)
        int tgtSlotIdx = _secondSelected.owner == Owner.Player ? 0 : 1;
        ShowSlotAfterAnimation(_secondSelected, tgtSlotIdx);

        if (!_firstPickWasInvalid && _firstSelected.owner == _currentPlayer && _secondSelected.owner == Opponent)
        {
            currentPhase = TurnPhase.Resolving;
            HideAllShuffleButtons();
            StartCoroutine(PlayerAttackSequence());
        }
        else
        {
            _attackResolved = false;
            actionPanel.SetMessageText(actionPanel.msgNoAttack + "\n" + actionPanel.instructionClickOutside);
            currentPhase = TurnPhase.ShowResult;
        }
    }

    void RefreshHoverForReel(Reel reel)
    {
        hoverPopup.Show(reel);
        memePlayer?.PlayHover(hoverPopup.previewImage, reel);
    }

    public void OnShuffleClicked()
    {
        if (currentPhase != TurnPhase.PlayerSelectFirst) return;

        if (gameMode == GameMode.VsPlayer)
        {
            int charges = _currentPlayer == Owner.Player ? _playerShuffleCharges : _playerTwoShuffleCharges;
            if (charges <= 0) return;

            if (_currentPlayer == Owner.Player) _playerShuffleCharges--;
            else _playerTwoShuffleCharges--;

            if (_firstSelected != null && !_firstSelected.isDestroyed)
                _firstSelected.FlipDown();
            _firstSelected = null;

            board.ShuffleAllFaceDown();
            RefreshUI();
            HideAllShuffleButtons();

            // Switch to next player
            _currentPlayer = Opponent;
            currentPhase = TurnPhase.PlayerSelectFirst;
            actionPanel.SetMessageText($"Shuffle! {charges - 1} charges left\n" + actionPanel.instructionPickReel);
            ShowCurrentPlayerButtons();
            return;
        }

        // VsNPC mode
        if (_playerShuffleCharges <= 0) return;

        _playerShuffleCharges--;

        if (_firstSelected != null && !_firstSelected.isDestroyed)
            _firstSelected.FlipDown();
        _firstSelected = null;

        board.ShuffleAllFaceDown();
        RefreshUI();
        actionPanel.SetMessageText($"Shuffle! {_playerShuffleCharges} charges left");
        HideAllShuffleButtons();

        StartNPCTurn();
    }

    void ResolveAttack(Reel attacker, Reel target)
    {
        _attackResolved = true;
        int damage = Mathf.Max(1, attacker.stats.atk);
        target.stats.currentHP -= damage;

        string attackerName = ReelDisplayName(attacker);
        string targetName = ReelDisplayName(target);
        string attackerOwner = OwnerDisplayName(attacker.owner);
        string targetOwner = OwnerDisplayName(target.owner);

        string msg = $"{attackerOwner}'s {attackerName} dealt {damage} damage to {targetOwner}'s {targetName}";
        if (target.stats.currentHP <= 0)
        {
            target.DestroyReel();
            msg = $"{attackerOwner}'s {attackerName} dealt {damage} damage — {targetOwner}'s {targetName} DESTROYED!";
        }

        actionPanel.SetMessageText(msg + "\n" + actionPanel.instructionClickOutside);
        actionPanel.UpdateHpBars();

        CheckWinCondition();
    }

    void CheckWinCondition()
    {
        if (!board.HasAliveReels(Owner.NPC))
        {
            currentPhase = TurnPhase.GameOver;
            actionPanel.ShowGameOver(gameMode == GameMode.VsPlayer ? actionPanel.gameOverP1Wins : actionPanel.gameOverYouWin);
            return;
        }

        if (!board.HasAliveReels(Owner.Player))
        {
            currentPhase = TurnPhase.GameOver;
            actionPanel.ShowGameOver(gameMode == GameMode.VsPlayer ? actionPanel.gameOverP2Wins : actionPanel.gameOverNPCWins);
            return;
        }

        currentPhase = TurnPhase.ShowResult;
    }

    /// <summary>
    /// Shows the reel slot after the fly animation. Falls back to immediate show if no scene pos is configured.
    /// </summary>
    void ShowSlotAfterAnimation(Reel reel, int slotIndex, System.Action onSlotShown = null)
    {
        Transform slotScenePos = slotIndex == 0 ? actionPanel.slot1ScenePos : actionPanel.slot2ScenePos;
        System.Action showSlot = () =>
        {
            if (slotIndex == 0)
            {
                actionPanel.ShowP1Slot(reel);
                memePlayer?.PlaySlot(0, actionPanel.playerSlot1Image, reel, true);
            }
            else
            {
                actionPanel.ShowP2Slot(reel);
                memePlayer?.PlaySlot(1, actionPanel.playerSlot2Image, reel, true);
            }
            onSlotShown?.Invoke();
        };

        if (slotScenePos == null)
        {
            showSlot();
            return;
        }

        dotweenManager.AnimateReelFly(reel, slotScenePos, slotIndex, showSlot);
    }

    IEnumerator PlayerAttackSequence()
    {
        // Wait for slot fly animations to complete
        while (dotweenManager.IsAnySlotAnimating)
            yield return null;
        yield return new WaitForSeconds(0.3f);
        var atkSlot = _firstSelected.owner == Owner.Player ? actionPanel.playerSlot1 : actionPanel.playerSlot2;
        var tgtSlot = _secondSelected.owner == Owner.Player ? actionPanel.playerSlot1 : actionPanel.playerSlot2;
        yield return dotweenManager.DashAndBack(_firstSelected, _secondSelected, atkSlot?.GetComponent<RectTransform>());
        yield return dotweenManager.Jitter(_secondSelected, tgtSlot?.GetComponent<RectTransform>());
        ResolveAttack(_firstSelected, _secondSelected);
    }

    void FinishPlayerTurn()
    {
        if (currentPhase != TurnPhase.ShowResult) return;
        _resultFromNpcTurn = false;
        FlipBackSelections();

        if (!_attackResolved)
        {
            if (_firstPickWasInvalid)
            {
                // Two invalid picks — pass turn to NPC
                currentPhase = TurnPhase.NPCTurn;
                RefreshUI();
                StartNPCTurn();
                return;
            }
            currentPhase = TurnPhase.PlayerSelectFirst;
            RefreshUI();
            ShowCurrentPlayerButtons();
            return;
        }

        currentPhase = TurnPhase.NPCTurn;
        RefreshUI();
        StartNPCTurn();
    }

    void FinishCurrentTurn()
    {
        if (currentPhase != TurnPhase.ShowResult) return;
        FlipBackSelections();

        if (!_attackResolved)
        {
            _currentPlayer = Opponent;
            currentPhase = TurnPhase.PlayerSelectFirst;
            RefreshUI();
            ShowCurrentPlayerButtons();
            return;
        }

        if (!board.HasAliveReels(Owner.NPC))
        {
            currentPhase = TurnPhase.GameOver;
            actionPanel.ShowGameOver(actionPanel.gameOverP1Wins);
            return;
        }
        if (!board.HasAliveReels(Owner.Player))
        {
            currentPhase = TurnPhase.GameOver;
            actionPanel.ShowGameOver(actionPanel.gameOverP2Wins);
            return;
        }

        _currentPlayer = Opponent;
        currentPhase = TurnPhase.PlayerSelectFirst;
        ShowCurrentPlayerButtons();
        RefreshUI();
    }

    void StartNPCTurn()
    {
        currentPhase = TurnPhase.NPCTurn;
        actionPanel.ShowTurnPanelP2NPC(actionPanel.turnLabelNPCTurn);
        actionPanel.ShowAttackButton(false);
        HideAllShuffleButtons();
        Invoke(nameof(ExecuteNPCTurn), 0.8f);
    }

    void ExecuteNPCTurn()
    {
        var npcAttackers = board.GetAliveFaceDown(Owner.NPC);
        var allTargets = board.GetAliveFaceDown(Owner.Player);

        if (npcAttackers.Count == 0 || allTargets.Count == 0)
        {
            CheckWinCondition();
            return;
        }

        Reel npcPick = npcAttackers[Random.Range(0, npcAttackers.Count)];

        // Exclude the attacker from possible targets
        allTargets.Remove(npcPick);
        if (allTargets.Count == 0)
        {
            CheckWinCondition();
            return;
        }

        Reel targetPick = allTargets[Random.Range(0, allTargets.Count)];

        _firstSelected = npcPick;
        _secondSelected = targetPick;

        _firstSelected.FlipUp();
        // NPC attacker (P2) → Slot2 with fly animation
        int slotIdx = _firstSelected.owner == Owner.Player ? 0 : 1;
        ShowSlotAfterAnimation(_firstSelected, slotIdx, () => Invoke(nameof(NPCSecondFlip), 0.4f));
    }

    void NPCSecondFlip()
    {
        _secondSelected.FlipUp();
        // Player target (P1) → Slot1 with fly animation
        int slotIdx = _secondSelected.owner == Owner.Player ? 0 : 1;
        ShowSlotAfterAnimation(_secondSelected, slotIdx, () => Invoke(nameof(NPCResolve), 0.3f));
    }

    void NPCResolve()
    {
        StartCoroutine(NPCAttackSequence());
    }

    IEnumerator NPCAttackSequence()
    {
        // Wait for slot fly animations to complete
        while (dotweenManager.IsAnySlotAnimating)
            yield return null;
        yield return new WaitForSeconds(0.3f);
        var atkSlot = _firstSelected.owner == Owner.Player ? actionPanel.playerSlot1 : actionPanel.playerSlot2;
        var tgtSlot = _secondSelected.owner == Owner.Player ? actionPanel.playerSlot1 : actionPanel.playerSlot2;
        yield return dotweenManager.DashAndBack(_firstSelected, _secondSelected, atkSlot?.GetComponent<RectTransform>());
        yield return dotweenManager.Jitter(_secondSelected, tgtSlot?.GetComponent<RectTransform>());

        int damage = Mathf.Max(1, _firstSelected.stats.atk);
        _secondSelected.stats.currentHP -= damage;

        string npcName = ReelDisplayName(_firstSelected);
        string targetName = ReelDisplayName(_secondSelected);
        string targetOwner = OwnerDisplayName(_secondSelected.owner);
        string msg = $"NPC's {npcName} dealt {damage} damage to {targetOwner}'s {targetName}";
        if (_secondSelected.stats.currentHP <= 0)
        {
            _secondSelected.DestroyReel();
            msg = $"NPC's {npcName} dealt {damage} damage — {targetOwner}'s {targetName} DESTROYED!";
        }

        actionPanel.SetMessageText(msg + "\n" + actionPanel.instructionClickOutside);
        actionPanel.UpdateHpBars();

        _attackResolved = true;
        _resultFromNpcTurn = true;
        currentPhase = TurnPhase.ShowResult;
    }

    void FinishNPCTurn()
    {
        _resultFromNpcTurn = false;
        FlipBackSelections();

        if (!board.HasAliveReels(Owner.Player))
        {
            currentPhase = TurnPhase.GameOver;
            actionPanel.ShowGameOver(actionPanel.gameOverNPCWins);
            return;
        }

        currentPhase = TurnPhase.PlayerSelectFirst;
        ShowCurrentPlayerButtons();
        RefreshUI();
    }

    void FlipBackSelections()
    {
        memePlayer?.Stop();
        hoverPopup.Unpin();
        if (_firstSelected != null && !_firstSelected.isDestroyed)
            _firstSelected.FlipDown();
        if (_secondSelected != null && !_secondSelected.isDestroyed)
            _secondSelected.FlipDown();
        foreach (var reel in _revealedReels)
        {
            if (reel != null && !reel.isDestroyed)
                reel.FlipDown();
        }
        _revealedReels.Clear();
        _firstSelected = null;
        _secondSelected = null;
        hoverPopup.Hide();
    }

    int CurrentShuffleCharges => gameMode == GameMode.VsPlayer
        ? (_currentPlayer == Owner.Player ? _playerShuffleCharges : _playerTwoShuffleCharges)
        : _playerShuffleCharges;

    public static string OwnerDisplayName(Owner owner)
    {
        if (Instance == null || Instance.gameMode == GameMode.VsNPC)
            return owner == Owner.Player ? "Player" : "NPC";
        return owner == Owner.Player ? "Player 1" : "Player 2";
    }

    static string ReelDisplayName(Reel reel)
    {
        if (reel.memeData != null && !string.IsNullOrEmpty(reel.memeData.memeName))
            return reel.memeData.memeName;
        return reel.name;
    }

    void RefreshUI()
    {
        actionPanel.ClearSlots();
        actionPanel.SetShuffleCharges(CurrentShuffleCharges);
        actionPanel.SetShuffleChargesP2(_playerTwoShuffleCharges);
        actionPanel.UpdateHpBars();
        actionPanel.SetMessageText(currentPhase == TurnPhase.PlayerSelectFirst ? actionPanel.instructionPickReel : "");
    }

    public int PlayerShuffleCharges => _playerShuffleCharges;
    public Reel FirstSelected => _firstSelected;
    public Reel SecondSelected => _secondSelected;
}

public enum TurnPhase
{
    PlayerSelectFirst,
    PlayerSelectSecond,
    Resolving,
    ShowResult,
    NPCTurn,
    GameOver
}
