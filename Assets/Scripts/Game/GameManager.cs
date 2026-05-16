using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public Board board;
    public ActionPanel actionPanel;
    public ReelHoverPopup hoverPopup;
    public MemePlayer memePlayer;

    [Header("State")]
    public TurnPhase currentPhase = TurnPhase.PlayerSelectFirst;

    Reel _firstSelected;
    Reel _secondSelected;
    Reel _hoveredReel;
    int _playerShuffleCharges = 2;
    bool _resultFromNpcTurn;

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
        RefreshUI();
        actionPanel.SetTurnText("Your Turn");
    }

    void WireButtons()
    {
        var shfBtn = UnityEngine.GameObject.Find("GameCanvas/ShuffleButton")?.GetComponent<UnityEngine.UI.Button>();
        if (shfBtn != null)
        {
            shfBtn.onClick.RemoveAllListeners();
            shfBtn.onClick.AddListener(OnShuffleClicked);
        }
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
            if (_resultFromNpcTurn)
                FinishNPCTurn();
            else
                FinishPlayerTurn();
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
        if (!reel.isFaceDown) { Debug.Log($"[GM] Reject — not faceDown"); return; }

        Debug.Log($"[GM] Selected first: {reel.name} at {reel.boardPosition}");
        _firstSelected = reel;
        _firstSelected.FlipUp();
        currentPhase = TurnPhase.PlayerSelectSecond;
        RefreshUI();
        actionPanel.ShowAttackerSlot(_firstSelected);
        memePlayer?.PlaySlot(actionPanel.playerSlot1Image, _firstSelected, true);
        actionPanel.SetInstructionText("Select a face-down reel as target");
        RefreshHoverForReel(reel);
    }

    void HandleSelectSecond(Reel reel)
    {
        if (!reel.isFaceDown || reel == _firstSelected) { Debug.Log($"[GM] Reject target — faceDown:{reel.isFaceDown} sameAsFirst:{reel == _firstSelected}"); return; }

        Debug.Log($"[GM] Selected second (target): {reel.name} at {reel.boardPosition}");
        _secondSelected = reel;
        _secondSelected.FlipUp();
        currentPhase = TurnPhase.Resolving;
        actionPanel.ShowShuffleButton(false);
        actionPanel.ShowTargetSlot(_secondSelected, _firstSelected);
        memePlayer?.PlaySlot(actionPanel.playerSlot2Image, _secondSelected, true);
        RefreshHoverForReel(reel);
        StartCoroutine(PlayerAttackSequence());
    }

    void RefreshHoverForReel(Reel reel)
    {
        hoverPopup.Show(reel);
        memePlayer?.PlayHover(hoverPopup.previewImage, reel);
    }

    public void OnShuffleClicked()
    {
        if (currentPhase != TurnPhase.PlayerSelectFirst || _playerShuffleCharges <= 0) return;

        _playerShuffleCharges--;

        if (_firstSelected != null && !_firstSelected.isDestroyed)
            _firstSelected.FlipDown();
        _firstSelected = null;

        board.ShuffleOwnerReels(Owner.Player);
        RefreshUI();
        actionPanel.ShowMessage($"Shuffle! {_playerShuffleCharges} charges left");
        actionPanel.ShowShuffleButton(false);

        StartNPCTurn();
    }

    void ResolveAttack(Reel attacker, Reel target)
    {
        int damage = Mathf.Max(1, attacker.stats.atk);
        target.stats.currentHP -= damage;

        string msg = $"{attacker.owner} ATK({attacker.stats.atk}) = {damage} dmg!";
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

        currentPhase = TurnPhase.ShowResult;
        actionPanel.SetInstructionText("Click anywhere outside the board to proceed");
    }

    IEnumerator PlayerAttackSequence()
    {
        yield return new WaitForSeconds(0.3f);
        var slot1Rt = actionPanel.playerSlot1?.GetComponent<RectTransform>();
        var slot2Rt = actionPanel.playerSlot2?.GetComponent<RectTransform>();
        yield return DashAndBack(_firstSelected, slot1Rt);
        yield return Jitter(_secondSelected, slot2Rt);
        ResolveAttack(_firstSelected, _secondSelected);
    }

    IEnumerator DashAndBack(Reel reel, RectTransform slotRt)
    {
        // Reel
        float width = reel.GetComponent<Renderer>().bounds.size.x;
        float distance = width * reel.attackDashDistancePercent;
        float halfDuration = reel.attackDashDuration * 0.5f;
        Vector3 startPos = reel.transform.position;
        Vector3 targetPos = startPos + Vector3.right * distance;

        // Slot
        Vector2 slotOrig = slotRt != null ? slotRt.anchoredPosition : Vector2.zero;
        float slotDist = slotRt != null ? slotRt.rect.width * reel.attackDashDistancePercent : 0f;
        Vector2 slotTarget = slotOrig + Vector2.right * slotDist;

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
                slotRt.anchoredPosition = slotOrig + new Vector2(x, 0f);
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
        var allTargets = board.GetAliveFaceDownAll();

        if (npcAttackers.Count == 0 || allTargets.Count == 0)
        {
            CheckWinCondition();
            return;
        }

        Reel npcPick = npcAttackers[Random.Range(0, npcAttackers.Count)];
        Reel targetPick = allTargets[Random.Range(0, allTargets.Count)];

        _firstSelected = npcPick;
        _secondSelected = targetPick;

        _firstSelected.FlipUp();
        actionPanel.ShowAttackerSlot(_firstSelected);
        memePlayer?.PlaySlot(actionPanel.playerSlot1Image, _firstSelected, true);
        Invoke(nameof(NPCSecondFlip), 0.8f);
    }

    void NPCSecondFlip()
    {
        _secondSelected.FlipUp();
        actionPanel.ShowTargetSlot(_secondSelected, _firstSelected);
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
        var slot1Rt = actionPanel.playerSlot1?.GetComponent<RectTransform>();
        var slot2Rt = actionPanel.playerSlot2?.GetComponent<RectTransform>();
        yield return DashAndBack(_firstSelected, slot1Rt);
        yield return Jitter(_secondSelected, slot2Rt);

        int damage = Mathf.Max(1, _firstSelected.stats.atk);
        _secondSelected.stats.currentHP -= damage;

        string msg = $"NPC ATK({_firstSelected.stats.atk}) → {damage} dmg!";
        if (_secondSelected.stats.currentHP <= 0)
        {
            _secondSelected.DestroyReel();
            msg = $"NPC {damage} dmg — Your reel DESTROYED!";
        }

        actionPanel.ShowMessage(msg);
        actionPanel.UpdateHpBars();

        _resultFromNpcTurn = true;
        currentPhase = TurnPhase.ShowResult;
        actionPanel.SetInstructionText("Click anywhere outside the board to proceed");
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
        actionPanel.SetTurnText("Your Turn");
        actionPanel.ShowShuffleButton(true);
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
    PlayerSelectSecond,
    Resolving,
    ShowResult,
    NPCTurn,
    GameOver
}
