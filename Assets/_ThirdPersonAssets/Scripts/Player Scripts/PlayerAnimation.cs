using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    #region ANIMATION_PARAMETER_IDS
    int moveDirXID;
    int moveDirZID;
    #endregion

    #region ANIMATOR_LAYER_INDICES
    int baseLayerIndex;
    int swordAndShieldLayerIndex;
    int axeLayerIndex;
    int pistolLayerIndex;
    int rifleLayerIndex;
    #endregion

    #region ANIMATION_CLIP_IDS
    int idleID;
    int strafeWalkID;
    int strafeRunID;
    int walkID;
    int runID;
    int sprintID;
    int walkToStopID;
    int runToStopID;
    int rollID;
    int standingJumpID;
    int runningJumpID;
    int fallingLoopID;
    int softLandingID;
    int hardLandingID;
    int climbLowID;
    int climbHighID;
    int bracedHangIdleID;
    int bracedHangShimmyLeftID;
    int bracedHangShimmyRightID;
    int bracedHangHopUpID;
    int bracedHangHopDownID;
    int bracedHangHopLeftID;
    int bracedHangHopRightID;

    #endregion

    #region ANIMATION_CLIP_LENGTHS
    float walkToStopLength;
    float runToStopLength;
    float standingJumpLength;
    float runningJumpLength;
    float softLandingLength;
    float hardLandingLength;
    internal float climbLowLength;
    internal float climbHighLength;
    internal float bracedHangHopUpLength;
    internal float bracedHangHopDownLength;
    internal float bracedHangHopRightLength;
    internal float bracedHangHopLeftLength;
    #endregion

    [SerializeField] private float animationTransitionTime;
    [SerializeField] private float lerpSpeed;

    private float error = 0.01f;
    private float lastYVelocity;
    private AttackSO currentAttackSO;

    Player player;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Start()
    {
        moveDirXID = Animator.StringToHash("MoveDirX");
        moveDirZID = Animator.StringToHash("MoveDirZ");

        baseLayerIndex = player.anim.GetLayerIndex("Base");
        swordAndShieldLayerIndex = player.anim.GetLayerIndex("SwordAndShield");
        axeLayerIndex = player.anim.GetLayerIndex("Axe");
        pistolLayerIndex = player.anim.GetLayerIndex("Pistol");
        rifleLayerIndex = player.anim.GetLayerIndex("Rifle");

        idleID = Animator.StringToHash("Idle");
        strafeWalkID = Animator.StringToHash("StrafeWalk");
        strafeRunID = Animator.StringToHash("StrafeRun");
        walkID = Animator.StringToHash("Walk");
        runID = Animator.StringToHash("Run");
        sprintID = Animator.StringToHash("Sprint");
        walkToStopID = Animator.StringToHash("WalkToStop");
        runToStopID = Animator.StringToHash("RunToStop");
        rollID = Animator.StringToHash("Roll");
        standingJumpID = Animator.StringToHash("StandingJump");
        runningJumpID = Animator.StringToHash("RunningJump");
        fallingLoopID = Animator.StringToHash("FallingLoop");
        softLandingID = Animator.StringToHash("SoftLanding");
        hardLandingID = Animator.StringToHash("HardLanding");
        climbLowID = Animator.StringToHash("ClimbLow");
        climbHighID = Animator.StringToHash("ClimbHigh");
        bracedHangIdleID = Animator.StringToHash("Braced Hang Idle");
        bracedHangShimmyLeftID = Animator.StringToHash("Braced Hang Shimmy Left");
        bracedHangShimmyRightID = Animator.StringToHash("Braced Hang Shimmy Right");
        bracedHangHopUpID = Animator.StringToHash("Braced Hang Hop Up");
        bracedHangHopDownID = Animator.StringToHash("Braced Hang Drop");
        bracedHangHopLeftID = Animator.StringToHash("Braced Hang Hop Left");
        bracedHangHopRightID = Animator.StringToHash("Braced Hang Hop Right");

        player.OnPlayerStateChanged += Player_OnPlayerStateChanged;
        player.OnPlayerStanceChanged += Player_OnPlayerStanceChanged;
        player.OnPlayerActionStateChanged += Player_OnPlayerActionStateChanged;
        player.playerCollision.OnPlayerGrounded += PlayerCollision_OnPlayerGrounded;
        player.meleeCombat.OnPlayerAttack += MeleeCombat_OnPlayerAttack;
        player.playerStats.OnTakeDamage += PlayerStats_OnTakeDamage;
        player.playerStats.OnZeroHealth += PlayerStats_OnZeroHealth;

        GetAnimationClipLengths();
    }

    private void Update()
    {
        SetAnimatorParameters();
        HandleAnimatorLayers();
    }

    private void GetAnimationClipLengths()
    {
        AnimationClip[] clips = player.anim.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            int clipID = Animator.StringToHash(clip.name);
            if (clipID == walkToStopID) walkToStopLength = clip.length;
            else if (clipID == runToStopID) runToStopLength = clip.length;
            else if (clipID == standingJumpID) standingJumpLength = clip.length;
            else if (clipID == runningJumpID) runningJumpLength = clip.length;
            else if (clipID == softLandingID) softLandingLength = clip.length;
            else if (clipID == hardLandingID) hardLandingLength = clip.length;
            else if (clipID == climbLowID) climbLowLength = clip.length;
            else if (clipID == climbHighID) climbHighLength = clip.length;
            else if (clipID == bracedHangHopUpID) bracedHangHopUpLength = clip.length;
            else if (clipID == bracedHangHopDownID) bracedHangHopDownLength= clip.length;
            else if (clipID == bracedHangHopRightID) bracedHangHopRightLength = clip.length;
            else if (clipID == bracedHangHopLeftID) bracedHangHopLeftLength = clip.length;
        }
    }

    private void SetAnimatorParameters()
    {
        Vector2 moveDir = GameInput.instance.GetMoveDirection();
        float moveDirX = moveDir.x < -error ? -1 : moveDir.x > error ? 1 : 0;
        float moveDirZ = moveDir.y < -error ? -1 : moveDir.y > error ? 1 : 0;

        float currentMovDirX = player.anim.GetFloat(moveDirXID);
        currentMovDirX = Mathf.Abs(moveDirX - currentMovDirX) < error ?
            moveDirX : Mathf.Lerp(currentMovDirX, moveDirX, lerpSpeed * Time.deltaTime);
        float currentMovDirZ = player.anim.GetFloat(moveDirZID);
        currentMovDirZ = Mathf.Abs(moveDirZ - currentMovDirZ) < error ?
            moveDirZ : Mathf.Lerp(currentMovDirZ, moveDirZ, lerpSpeed * Time.deltaTime);

        player.anim.SetFloat(moveDirXID, currentMovDirX);
        player.anim.SetFloat(moveDirZID, currentMovDirZ);
    }

    private void HandleAnimatorLayers()
    {
        int currentLayerIndex = player.anim.GetLayerIndex(player.currentStance.ToString());

        float currentLayerWeight = player.anim.GetLayerWeight(currentLayerIndex);
        currentLayerWeight = Mathf.Abs(1 - currentLayerWeight) < error ?
            1 : Mathf.Lerp(currentLayerWeight, 1, lerpSpeed * Time.deltaTime);
        player.anim.SetLayerWeight(currentLayerIndex, currentLayerWeight);

        if (currentLayerIndex != swordAndShieldLayerIndex)
        {
            float layerWeight = player.anim.GetLayerWeight(swordAndShieldLayerIndex);
            layerWeight = Mathf.Abs(0 - layerWeight) < error ?
                0 : Mathf.Lerp(layerWeight, 0, lerpSpeed * Time.deltaTime);
            player.anim.SetLayerWeight(swordAndShieldLayerIndex, layerWeight);
        }
        if (currentLayerIndex != axeLayerIndex)
        {
            float layerWeight = player.anim.GetLayerWeight(axeLayerIndex);
            layerWeight = Mathf.Abs(0 - layerWeight) < error ?
                0 : Mathf.Lerp(layerWeight, 0, lerpSpeed * Time.deltaTime);
            player.anim.SetLayerWeight(axeLayerIndex, layerWeight);
        }
        if (currentLayerIndex != pistolLayerIndex)
        {
            float layerWeight = player.anim.GetLayerWeight(pistolLayerIndex);
            layerWeight = Mathf.Abs(0 - layerWeight) < error ?
                0 : Mathf.Lerp(layerWeight, 0, lerpSpeed * Time.deltaTime);
            player.anim.SetLayerWeight(pistolLayerIndex, layerWeight);
        }
        if (currentLayerIndex != rifleLayerIndex)
        {
            float layerWeight = player.anim.GetLayerWeight(rifleLayerIndex);
            layerWeight = Mathf.Abs(0 - layerWeight) < error ?
                0 : Mathf.Lerp(layerWeight, 0, lerpSpeed * Time.deltaTime);
            player.anim.SetLayerWeight(rifleLayerIndex, layerWeight);
        }
    }

    private void Player_OnPlayerStateChanged(object sender, EventArgs e)
    {
        if (player.currentAction == Player.Action.Attacking)
        {
            return;
        }

        switch (player.currentState)
        {
            case Player.State.Idle:
                HandleIdleStateAnimations();
                break;
            case Player.State.Walk:
                HandleWalkStateAnimations();
                break;
            case Player.State.Run:
                HandleRunStateAnimations();
                break;
            case Player.State.Sprint:
                player.anim.CrossFadeInFixedTime(sprintID, animationTransitionTime);
                break;
            case Player.State.Jump:
                HandleJumpStateAnimations();
                break;
            case Player.State.Roll:
                player.anim.CrossFadeInFixedTime(rollID, animationTransitionTime);
                break;
            /*case Player.State.Climb:
                HandleClimbStateAnimations();
                break;
            case Player.State.LedgeGrab:
                HandleLedgeGrabStateAnimations();
                break;*/
        }
    }

    private void Player_OnPlayerStanceChanged(object sender, EventArgs e)
    {
        if (player.currentAction == Player.Action.Attacking)
        {
            return;
        }

        switch (player.currentState)
        {
            case Player.State.Idle:
                HandleIdleStateAnimations();
                break;
            case Player.State.Walk:
                HandleWalkStateAnimations();
                break;
            case Player.State.Run:
                HandleRunStateAnimations();
                break;
            case Player.State.Sprint:
                player.anim.CrossFadeInFixedTime(sprintID, animationTransitionTime);
                break;
            case Player.State.Jump:
                HandleJumpStateAnimations();
                break;
            case Player.State.Roll:
                player.anim.CrossFadeInFixedTime(rollID, animationTransitionTime);
                break;
        }
    }
    private void Player_OnPlayerActionStateChanged(object sender, EventArgs e)
    {
        if (player.currentAction == Player.Action.Blocking)
        {
            player.anim.CrossFadeInFixedTime("SASBlockingIdle", animationTransitionTime, 7);
        }
        else
        {
            player.anim.CrossFadeInFixedTime("Empty", animationTransitionTime, 7);
        }

        if (player.currentAction == Player.Action.Attacking)
        {
            return;
        }

        switch (player.currentState)
        {
            case Player.State.Idle:
                HandleIdleStateAnimations();
                break;
            case Player.State.Walk:
                HandleWalkStateAnimations();
                break;
            case Player.State.Run:
                HandleRunStateAnimations();
                break;
            case Player.State.Sprint:
                player.anim.CrossFadeInFixedTime(sprintID, animationTransitionTime);
                break;
            case Player.State.Jump:
                HandleJumpStateAnimations();
                break;
            case Player.State.Roll:
                player.anim.CrossFadeInFixedTime(rollID, animationTransitionTime);
                break;
            /*case Player.State.Climb:
                HandleClimbStateAnimations();
                break;
            case Player.State.LedgeGrab:
                HandleLedgeGrabStateAnimations();
                break;*/
        }
    }

    private void PlayerCollision_OnPlayerGrounded(object sender, PlayerCollision.OnPlayerGroundedEventArgs e)
    {
        lastYVelocity = e.lastYVelocity;
    }

    private void MeleeCombat_OnPlayerAttack(object sender, MeleeCombat.OnPlayerAttackEventArgs e)
    {
        currentAttackSO = e.attackSO;
        HandleAttackStateAnimations();
    }

    private void HandleIdleStateAnimations()
    {
        if (player.lastState == Player.State.Jump)
        {
            if (Mathf.Abs(player.rb.linearVelocity.y - player.playerMovement.terminalVelocity) < 0.1f)
            {
                player.anim.CrossFadeInFixedTime(hardLandingID, animationTransitionTime);
                StartCoroutine(AnyToIdleCoroutine(hardLandingLength));
            }
            else if (player.rb.linearVelocity.y < 0f)
            {
                player.anim.CrossFadeInFixedTime(softLandingID, animationTransitionTime);
                StartCoroutine(AnyToIdleCoroutine(softLandingLength));
            }
            else
            {
                player.anim.CrossFadeInFixedTime(idleID, animationTransitionTime);
            }
        }
        else if (player.lastState == Player.State.Run && player.currentStance == Player.Stance.Base)
        {
            player.anim.CrossFadeInFixedTime(walkToStopID, animationTransitionTime);
            StartCoroutine(AnyToIdleCoroutine(walkToStopLength));
        }
        else if (player.lastState == Player.State.Sprint)
        {
            player.anim.CrossFadeInFixedTime(runToStopID, animationTransitionTime);
            StartCoroutine(AnyToIdleCoroutine(runToStopLength));
        }
        else
        {
            player.anim.CrossFadeInFixedTime(idleID, animationTransitionTime);
        }
    }

    private IEnumerator AnyToIdleCoroutine(float time)
    {
        yield return new WaitForSeconds(time);
        if (player.currentState == Player.State.Idle && player.currentAction == Player.Action.Idle)
            player.anim.CrossFadeInFixedTime(idleID, animationTransitionTime);
    }
    private void HandleWalkStateAnimations()
    {
        if (player.rangedCombat.isAiming || player.rangedCombat.isFiring || player.currentAction == Player.Action.Blocking)
            player.anim.CrossFadeInFixedTime(strafeWalkID, animationTransitionTime);
        else
            player.anim.CrossFadeInFixedTime(walkID, animationTransitionTime);
    }

    private void HandleRunStateAnimations()
    {
        if (player.rangedCombat.isAiming || player.rangedCombat.isFiring)
            player.anim.CrossFadeInFixedTime(strafeRunID, animationTransitionTime);
        else
            player.anim.CrossFadeInFixedTime(runID, animationTransitionTime);
    }

    private void HandleJumpStateAnimations()
    {
        if (player.playerMovement.jumpDurationCounter <= 0f)
        {
            player.anim.CrossFadeInFixedTime(fallingLoopID, animationTransitionTime);
        }
        else if (player.playerMovement.moveDir == Vector3.zero)
        {
            player.anim.CrossFadeInFixedTime(standingJumpID, animationTransitionTime);
            StartCoroutine(JumpingToFallingCoroutine(standingJumpLength));
        }
        else if (player.playerMovement.moveDir != Vector3.zero)
        {
            player.anim.CrossFadeInFixedTime(runningJumpID, animationTransitionTime);
            StartCoroutine(JumpingToFallingCoroutine(runningJumpLength));
        }
    }

    /*private void HandleClimbStateAnimations()
    {
        if (player.playerMovement.climbDuration == climbLowLength)
        {
            player.anim.CrossFadeInFixedTime(climbLowID, animationTransitionTime);
        }
        else if (player.playerMovement.climbDuration == climbHighLength)
        {
            player.anim.CrossFadeInFixedTime(climbHighID, animationTransitionTime);
        }
    }*/

    /*private void HandleLedgeGrabStateAnimations()
    {
        Vector2 hopDir = player.playerMovement.ledgeHopDir;

        if (player.currentAction == Player.Action.Hopping)
        {
            if (hopDir == Vector2.up)
                player.anim.CrossFadeInFixedTime(bracedHangHopUpID, animationTransitionTime);
            else if (hopDir == Vector2.down)
                player.anim.CrossFadeInFixedTime(bracedHangHopDownID, animationTransitionTime);
            else if (hopDir == Vector2.right)
                player.anim.CrossFadeInFixedTime(bracedHangHopRightID, animationTransitionTime);
            else if (hopDir == Vector2.left)
                player.anim.CrossFadeInFixedTime(bracedHangHopLeftID, animationTransitionTime);
        }
        else if (player.currentAction == Player.Action.Shimmying)
        {
            if (player.playerMovement.ledgeMoveDir.x == 1)
            {
                player.anim.CrossFadeInFixedTime(bracedHangShimmyRightID, animationTransitionTime);
            }
            else if (player.playerMovement.ledgeMoveDir.x == -1)
            {
                player.anim.CrossFadeInFixedTime(bracedHangShimmyLeftID, animationTransitionTime);
            }
        }
        else if (player.currentAction == Player.Action.Idle)
        {
            player.anim.CrossFadeInFixedTime(bracedHangIdleID, animationTransitionTime);
        }
    }*/

    private IEnumerator JumpingToFallingCoroutine(float time)
    {
        yield return new WaitForSeconds(time);
        if (player.currentState == Player.State.Jump)
            player.anim.CrossFadeInFixedTime(fallingLoopID, animationTransitionTime);
    }

    private void HandleAttackStateAnimations()
    {
        player.anim.CrossFadeInFixedTime(currentAttackSO.name, animationTransitionTime);
    }

    private void PlayerStats_OnTakeDamage(object sender, EventArgs e)
    {
        int index = UnityEngine.Random.Range(0, 3);
        switch (index)
        {
            case 0:
                player.anim.Play("Hit01");
                break;
            case 1:
                player.anim.Play("Hit02");
                break;
            case 2:
                player.anim.Play("Hit03");
                break;
        }
    }

    private void PlayerStats_OnZeroHealth(object sender, EventArgs e)
    {
        player.anim.CrossFadeInFixedTime("Death", animationTransitionTime);
    }
}
