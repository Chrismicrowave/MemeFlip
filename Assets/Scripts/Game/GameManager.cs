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
    Owner _currentPlayer = Owner.Player;
    Owner Opponent => _currentPlayer == Owner.Player ? Owner.NPC : Owner.Player;
    bool _resultFromNpcTurn;
    bool _attackResolved;

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
                actionPanel.ShowTurnPanelP1("Player 1's Turn");
            else
                actionPanel.ShowTurnPanelP2NPC("Player 2's Turn");
        }
        else
        {
            actionPanel.ShowShuffleButton(true);
            actionPanel.ShowShuffleButtonP2(false);
            actionPanel.ShowTurnPanelP1("Your Turn");
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
        // Replay in slot when clicking an already-flipped selected reel
        if (!reel.isFaceDown && !reel.isDestroyed)
        {
            if (reel == _firstSelected)
            {
                memePlayer?.PlaySlot(actionPanel.playerSlot1Image, reel, true);
                return;
            }
            if (reel == _secondSelected)
            {
                memePlayer?.PlaySlot(actionPanel.playerSlot2Image, reel, true);
                return;
            }
            // Face-down unselected reels fall through to selection logic
        }

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
        _firstSelected = reel;
        _firstSelected.FlipUp();
        currentPhase = TurnPhase.PlayerSelectSecond;
        HideAllShuffleButtons();
        RefreshUI();
        if (reel.owner == _currentPlayer)
            actionPanel.ShowAttackerSlot(_firstSelected, reel.owner);
        memePlayer?.PlaySlot(actionPanel.playerSlot1Image, _firstSelected, true);
        actionPanel.SetMessageText(actionPanel.instructionSelectTarget);
        RefreshHoverForReel(reel);
    }

    void HandleSelectSecond(Reel reel)
    {
        if (!reel.isFaceDown || reel == _firstSelected) return;

        Debug.Log($"[GM] Selected second (target): {reel.name} at {reel.boardPosition}");
        _secondSelected = reel;
        _secondSelected.FlipUp();
        memePlayer?.PlaySlot(actionPanel.playerSlot2Image, _secondSelected, true);
        RefreshHoverForReel(reel);

        if (_firstSelected.owner == _currentPlayer && _secondSelected.owner == Opponent)
        {
            actionPanel.ShowAttackerSlot(_firstSelected, _firstSelected.owner);
            actionPanel.ShowTargetSlot(_secondSelected, _secondSelected.owner);
            currentPhase = TurnPhase.Resolving;
            HideAllShuffleButtons();
            StartCoroutine(PlayerAttackSequence());
        }
        else
        {
            _attackResolved = false;
            actionPanel.SetMessageText(actionPanel.msgInvalidAttack + "\n" + actionPanel.instructionClickOutside);
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
            actionPanel.ShowGameOver(gameMode == GameMode.VsPlayer ? "Player 1 Wins!" : "You Win!");
            return;
        }

        if (!board.HasAliveReels(Owner.Player))
        {
            currentPhase = TurnPhase.GameOver;
            actionPanel.ShowGameOver(gameMode == GameMode.VsPlayer ? "Player 2 Wins!" : "NPC Wins!");
            return;
        }

        currentPhase = TurnPhase.ShowResult;
    }

    IEnumerator PlayerAttackSequence()
    {
        yield return new WaitForSeconds(0.3f);
        var atkSlot = _firstSelected.owner == Owner.Player ? actionPanel.playerSlot1 : actionPanel.playerSlot2;
        var tgtSlot = _secondSelected.owner == Owner.Player ? actionPanel.playerSlot1 : actionPanel.playerSlot2;
        yield return DashAndBack(_firstSelected, _secondSelected, atkSlot?.GetComponent<RectTransform>());
        yield return Jitter(_secondSelected, tgtSlot?.GetComponent<RectTransform>());
        ResolveAttack(_firstSelected, _secondSelected);
    }

    IEnumerator DashAndBack(Reel reel, Reel target, RectTransform slotRt)
    {
        float width = reel.GetComponent<Renderer>().bounds.size.x;
        float distance = width * reel.attackDashDistancePercent;
        Vector3 dir = (target.transform.position - reel.transform.position).normalized;
        float halfDuration = reel.attackDashDuration * 0.5f;
        Vector3 startPos = reel.transform.position;
        Vector3 targetPos = startPos + dir * distance;

        // Slot
        Vector2 slotOrig = slotRt != null ? slotRt.anchoredPosition : Vector2.zero;
        float slotDist = slotRt != null ? slotRt.rect.width * reel.attackDashDistancePercent : 0f;
        Vector2 slotTarget = slotOrig + Vector2.right * slotDist * Mathf.Sign(dir.x);

        // Dash out
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            float t = elapsed / halfDuration;
            reel.transform.position = Vector3.Lerp(startPos, targetPos, t);
            if (slotRt != null) slotRt.anchoredPosition = Vector2.Lerp(slotOrig, slotTarget, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        reel.transform.position = targetPos;
        if (slotRt != null) slotRt.anchoredPosition = slotTarget;

        // Dash back
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            float t = elapsed / halfDuration;
            reel.transform.position = Vector3.Lerp(targetPos, startPos, t);
            if (slotRt != null) slotRt.anchoredPosition = Vector2.Lerp(slotTarget, slotOrig, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        reel.transform.position = startPos;
        if (slotRt != null) slotRt.anchoredPosition = slotOrig;
    }

    IEnumerator Jitter(Reel reel, RectTransform slotRt)
    {
        Vector3 origPos = reel.transform.position;
        Vector2 slotOrig = slotRt != null ? slotRt.anchoredPosition : Vector2.zero;
        float elapsed = 0f;
        while (elapsed < reel.jitterDuration)
        {
            float x = Mathf.Sin(elapsed * reel.jitterSpeed) * reel.jitterIntensity;
            float z = Mathf.Cos(elapsed * reel.jitterSpeed * 0.7f) * reel.jitterIntensity;
            reel.transform.position = origPos + new Vector3(x, 0f, z);
            if (slotRt != null)
                slotRt.anchoredPosition = slotOrig + new Vector2(
                    Mathf.Sin(elapsed * reel.jitterSpeed) * reel.jitterSlotIntensity, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        reel.transform.position = origPos;
        if (slotRt != null) slotRt.anchoredPosition = slotOrig;
    }

    void FinishPlayerTurn()
    {
        if (currentPhase != TurnPhase.ShowResult) return;
        _resultFromNpcTurn = false;
        FlipBackSelections();

        if (!_attackResolved)
        {
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
            actionPanel.ShowGameOver("Player 1 Wins!");
            return;
        }
        if (!board.HasAliveReels(Owner.Player))
        {
            currentPhase = TurnPhase.GameOver;
            actionPanel.ShowGameOver("Player 2 Wins!");
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
        actionPanel.ShowTurnPanelP2NPC("NPC Turn");
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
        actionPanel.ShowAttackerSlot(_firstSelected, _firstSelected.owner);
        memePlayer?.PlaySlot(actionPanel.playerSlot1Image, _firstSelected, true);
        Invoke(nameof(NPCSecondFlip), 0.8f);
    }

    void NPCSecondFlip()
    {
        _secondSelected.FlipUp();
        actionPanel.ShowTargetSlot(_secondSelected, _secondSelected.owner);
        memePlayer?.PlaySlot(actionPanel.playerSlot2Image, _secondSelected, true);
        Invoke(nameof(NPCResolve), 0.6f);
    }

    void NPCResolve()
    {
        StartCoroutine(NPCAttackSequence());
    }

    IEnumerator NPCAttackSequence()
    {
        yield return new WaitForSeconds(0.3f);
        var atkSlot = _firstSelected.owner == Owner.Player ? actionPanel.playerSlot1 : actionPanel.playerSlot2;
        var tgtSlot = _secondSelected.owner == Owner.Player ? actionPanel.playerSlot1 : actionPanel.playerSlot2;
        yield return DashAndBack(_firstSelected, _secondSelected, atkSlot?.GetComponent<RectTransform>());
        yield return Jitter(_secondSelected, tgtSlot?.GetComponent<RectTransform>());

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
            actionPanel.ShowGameOver("NPC Wins!");
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
