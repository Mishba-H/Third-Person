using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;

public class Player : MonoBehaviour
{
    public static Player instance{ get; private set; }

    public event EventHandler OnPlayerStateChanged;

    public event EventHandler OnPlayerStanceChanged;

    public event EventHandler OnPlayerActionStateChanged;

    #region COMPONENTS
    internal Rigidbody rb;
    internal CapsuleCollider capsuleCollider;
    internal Animator anim;
    #endregion

    #region PLAYER_SCRIPTS
    internal PlayerStats playerStats;
    internal PlayerRig playerRig;
    internal PlayerAnimation playerAnimation;
    internal AnimationEventsHandler animationEventsHandler;
    internal PlayerCollision playerCollision;
    internal PlayerMovement playerMovement;
    internal MeleeCombat meleeCombat;
    internal RangedCombat rangedCombat;
    #endregion

    #region MECHANICS
    [Header("Mechanics")]
    public bool enableMovement = true;
    public bool enableJump = true;
    public bool enableRoll = true;
    public bool enableClimb = true;
    public bool enableLedgeGrab = true;
    public bool enableMeleeCombat = true;
    public bool enableRangedCombat = true;
    #endregion

    public enum State
    {
        Idle,
        Walk,
        Run,
        Sprint,
        Jump,
        Roll,
        Climb,
        LedgeGrab,
        Dead
    }

    public enum Stance
    {
        Base,
        SwordAndShield,
        Axe,
        Pistol,
        Rifle
    }

    public enum Action
    {
        Idle,
        Attacking,
        Blocking,
        Aiming,
        Firing,
        Reloading,
        Shimmying,
        Hopping
    }

    public State currentState;
    public State lastState;
    public Stance currentStance;
    public Action currentAction;
    private float stateCounter;
    private float actionCounter;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            Debug.LogError("Multiple instances of GameInput found");
        }
        instance = this;

        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        anim = GetComponentInChildren<Animator>();

        playerStats = GetComponent<PlayerStats>();
        playerRig = GetComponentInChildren<PlayerRig>();
        playerAnimation = GetComponent<PlayerAnimation>();
        animationEventsHandler = GetComponentInChildren<AnimationEventsHandler>();
        playerCollision = GetComponent<PlayerCollision>();
        playerMovement = GetComponent<PlayerMovement>();
        meleeCombat = GetComponent<MeleeCombat>();
        rangedCombat = GetComponent<RangedCombat>();
    }

    private void Start()
    {
        currentState = State.Idle;

        meleeCombat.OnPlayerAttack += MeleeCombat_OnPlayerAttack;
        playerStats.OnZeroHealth += PlayerStats_OnZeroHealth;
        UIManager.instance.OnWeaponWheelEnable += UiManager_OnWeaponWheelEnable;
        UIManager.instance.OnWeaponWheelDisable += UiManager_OnWeaponWheelDisable;
    }

    private void PlayerStats_OnZeroHealth(object sender, EventArgs e)
    {
        ChangeState(State.Dead);
    }

    private void Update()
    {
        if (currentState == State.Dead)
        {
            enableMovement = false;
            enableJump = false;
            enableRoll = false;
            enableMeleeCombat = false;
            enableRangedCombat = false;
            return;
        }

        if (stateCounter > 0)
        {
            stateCounter -= Time.deltaTime;
        }
        HandlePlayerState();

        if (actionCounter > 0)
        {
            actionCounter -= Time.deltaTime;
        }
        HandlePlayerActionState();
    }

    public void ChangeState(State newState, float lockFor = 0f)
    {
        if (newState == currentState)
        {
            stateCounter = lockFor;
            return;
        }
        if (stateCounter > 0) return;

        lastState = currentState;
        currentState = newState;
        stateCounter = lockFor;

        OnPlayerStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ChangeStance()
    {
        string buttonName = EventSystem.current.currentSelectedGameObject.name;
        Stance newStance = (Stance)Enum.Parse(typeof(Stance),buttonName);

        currentStance = newStance;

        OnPlayerStanceChanged?.Invoke(this, EventArgs.Empty);
    }
    public void ChangeAction(Action newAction, float lockFor = 0f)
    {
        if (newAction == currentAction)
        {
            actionCounter = lockFor;
            return;
        }
        if (actionCounter > 0) return;

        currentAction = newAction;
        actionCounter = lockFor;

        OnPlayerActionStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ForceChangeActionState(Action newActionState)
    {
        actionCounter = 0;
        ChangeAction(newActionState);
    }

    private void MeleeCombat_OnPlayerAttack(object sender, MeleeCombat.OnPlayerAttackEventArgs e)
    {
        var time = e.attackSO.clipLength;
        ChangeAction(Action.Attacking, time);
    }

    private void UiManager_OnWeaponWheelEnable(object sender, EventArgs e)
    {
        enableMeleeCombat = false;
    }

    private void UiManager_OnWeaponWheelDisable(object sender, EventArgs e)
    {
        enableMeleeCombat = true;
    }

    private void HandlePlayerState()
    {
        if (playerMovement.jumpDurationCounter > 0f)
        {
            ChangeState(State.Jump);
            return;
        }

        if (playerMovement.rollDurationTimer > 0f)
        {
            ChangeState(State.Roll);
            return;
        }
        
        /*if (playerMovement.climbDurationTimer > 0f)
        {
            ChangeState(State.Climb);
            return;
        }*/

        /*if (playerMovement.isOnLedge)
        {
            ChangeState(State.LedgeGrab);
            return;
        }*/

        if (playerCollision.isCloseToGround)
        {
            if (playerMovement.targetSpeed == 0f)
            {
                ChangeState(State.Idle);
            }
            if (Mathf.Abs(playerMovement.targetSpeed - playerMovement.walkSpeed * playerMovement.speedMultiplier) < 0.01)
            {
                ChangeState(State.Walk);
            }
            else if (Mathf.Abs(playerMovement.targetSpeed - playerMovement.runSpeed * playerMovement.speedMultiplier) < 0.01)
            {
                ChangeState(State.Run);
            }
            else if (Mathf.Abs(playerMovement.targetSpeed - playerMovement.sprintSpeed * playerMovement.speedMultiplier) < 0.01)
            {
                ChangeState(State.Sprint);
            }
        }
        else
        {
            ChangeState(State.Jump);
        }
    }

    private void HandlePlayerActionState()
    {
        /*if (playerMovement.isOnLedge)
        {
            if (playerMovement.ledgeHopDurationTimer > 0f)
            {
                ChangeAction(Action.Hopping);
            }
            else if (playerMovement.ledgeMoveDir.x != 0f)
            {
                ChangeAction(Action.Shimmying);
            }
            else
            {
                ChangeAction(Action.Idle);
            }
            return;
        }*/

        if (rangedCombat.isAiming)
        {
            ChangeAction(Action.Aiming);
        }
        else if (rangedCombat.isFiring)
        {
            ChangeAction(Action.Firing);
        }
        else if (meleeCombat.isBlocking)
        {
            ChangeAction(Action.Blocking);
        }
        else
        {
            ChangeAction(Action.Idle);    
        }
    }
}
