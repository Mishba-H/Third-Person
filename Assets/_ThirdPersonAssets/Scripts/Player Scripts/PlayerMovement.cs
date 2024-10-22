using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Transform cameraOrientation;
    [SerializeField] private float rotateSpeed;

    #region FLAT_MOVEMENT_VARIABLES
    [Header("Flat Movement Variables")]
    [SerializeField] private float inputMagnitudeThreshold;
    [SerializeField] internal float speedThreshold;
    [SerializeField] internal float walkSpeed;
    [SerializeField] internal float runSpeed;
    [SerializeField] internal float sprintSpeed;
    [SerializeField] private float accelerationTime;
    [SerializeField] private float decelerationTime;
    [SerializeField] private float baseMultiplier;
    [SerializeField] private float swordAndShieldMultiplier;
    [SerializeField] private float axeMultiplier;
    [SerializeField] private float pistolMultiplier;
    [SerializeField] private float rifleMultiplier;

    internal Vector3 moveDir;
    internal float targetSpeed;
    internal float speedMultiplier;
    private float acceleration;
    private float deceleration;
    internal bool isSprinting;
    #endregion

    #region VERTICAL_MOVEMENT_VARIABLES
    [Header("Vertical Movement Variables")]
    [SerializeField] private float gravityValue;
    [SerializeField] private float jumpHeight;
    [SerializeField] private float jumpDistance;
    [SerializeField] internal float terminalVelocity;
    [SerializeField] internal float hardLandingDuration;
    private Vector3 jumpVelocity;
    private float timeOfFlight;
    internal float jumpDurationCounter;
    #endregion

    #region ROLL_VARIABLES
    [Header("Roll Variables")]
    [SerializeField] private float rollDistance;
    [SerializeField] private float rollDuration;
    [SerializeField] private float rollInterval;
    private Vector3 rollDir;
    private float rollIntervalTimer;
    internal float rollDurationTimer;
    #endregion

    /*#region CLIMB_VARIABLES
    [Header("Climb Variables")]
    [SerializeField] internal float climbLowHeight;
    [SerializeField] internal float climbHighHeight;
    [SerializeField] private Vector2 climbLowOffset;
    [SerializeField] private Vector2 climbHighOffset;
    internal float climbDuration;
    internal float climbDurationTimer;
    private Vector3 climbEndPoint;
    private Vector3 climbStartPoint;
    private Vector3 wallDir;
    #endregion*/

    /*#region LEDGE_VARIABLES
    [SerializeField] private float ledgeMoveSpeed;
    [SerializeField] private float ledgeHopLength;
    [SerializeField] private Vector2 ledgeOffset;
    [SerializeField] private float ledgeHopInterval;
    internal bool isOnLedge;
    internal Vector2 ledgeMoveDir;
    internal Vector2 ledgeHopDir;
    internal RaycastHit ledgeInfo;
    internal float ledgeHopDuration;
    internal float ledgeHopDurationTimer;
    private Vector3 hopStartPoint;
    private Vector3 hopEndPoint;
    private float ledgeHopIntervalTimer;
    #endregion*/

    Player player;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Start()
    {
        player.playerCollision.OnPlayerGrounded += PlayerCollision_OnPlayerGrounded;
        player.meleeCombat.OnPlayerAttack += MeleeCombat_OnPlayerAttack;
    }

    private void Update()
    {
        CalculateMoveDirection();
        RotatePlayer();
        HandleTargetSpeedMultiplier();
        CalculateTargetSpeed();
        HandleJump();
        //HandleClimb();
        //HandleLedgeGrab();
        HandleRoll();
    }

    private void FixedUpdate()
    {
        HandleFlatMovement();
        HandleVerticalMovement();
        //HandleCollisions();
    }

    private void PlayerCollision_OnPlayerGrounded(object sender, EventArgs e)
    {
        jumpDurationCounter = 0f;
        if (rollDurationTimer <= 0f)
        {
            var velocity = new Vector3(moveDir.x * targetSpeed, 0f, moveDir.z * targetSpeed);
            player.rb.linearVelocity = velocity;
        }
    }

    private void MeleeCombat_OnPlayerAttack(object sender, MeleeCombat.OnPlayerAttackEventArgs e)
    {
        player.rb.linearVelocity = Vector3.zero;
    }

    private void CalculateMoveDirection()
    {
        var inputDir = GameInput.instance.GetMoveDirection();

        moveDir = inputDir.x * new Vector3(cameraOrientation.right.x, 0f, cameraOrientation.right.z) +
            inputDir.y * new Vector3(cameraOrientation.forward.x, 0f, cameraOrientation.forward.z);
        moveDir = moveDir.normalized;

        float ledgeMoveDirX = Mathf.Abs(inputDir.x) > 0.3f ? inputDir.x : 0f;
        float ledgeMoveDirY = Mathf.Abs(inputDir.y) > 0.3f ? inputDir.y : 0f;
        /*if (Mathf.Abs(ledgeMoveDirX) > Mathf.Abs(ledgeMoveDirY))
            ledgeMoveDir = new Vector2(ledgeMoveDirX, 0f).normalized;
        else
            ledgeMoveDir = new Vector2(0f, ledgeMoveDirY).normalized;*/
    }

    private void RotatePlayer()
    {
        if (!player.enableMovement) return;

        if (player.currentAction == Player.Action.Attacking) return;

        if (rollDurationTimer > 0f)
        {
            var targetForward = rollDir;
            transform.forward = Vector3.Slerp(transform.forward, targetForward, rotateSpeed * Time.deltaTime);
            return;
        }

        /*if (climbDurationTimer > 0f || isOnLedge)
        {
            var targetForward = wallDir;
            transform.forward = Vector3.Slerp(transform.forward, targetForward, rotateSpeed * Time.deltaTime);
            return;
        }*/
        
        if (player.currentAction == Player.Action.Blocking)
        {
            //Player always faces in the direction of the camera forward
            var targetForward = new Vector3(cameraOrientation.forward.x, 0f, cameraOrientation.forward.z);
            transform.forward = Vector3.Slerp(transform.forward, targetForward, rotateSpeed * Time.deltaTime);
        }
        else if (player.currentAction == Player.Action.Aiming || player.currentAction == Player.Action.Firing)
        {
            //Player always faces in the direction of the aim point in the world
            var aimPoint = player.playerCollision.GetAimPoint();
            var targetForward = new Vector3(aimPoint.x - transform.position.x, 0f, aimPoint.z - transform.position.z);
            transform.forward = Vector3.Slerp(transform.forward, targetForward, rotateSpeed * Time.deltaTime);
        }
        else
        {
            //Player faces along the move direction
            var targetForward = moveDir == Vector3.zero ? transform.forward : moveDir;
            transform.forward = Vector3.Slerp(transform.forward, targetForward, rotateSpeed * Time.deltaTime);
        }
    }

    private void HandleTargetSpeedMultiplier()
    {
        switch (player.currentStance)
        {
            case Player.Stance.Base:
                speedMultiplier = baseMultiplier; break;
            case Player.Stance.SwordAndShield:
                speedMultiplier = swordAndShieldMultiplier; break;
            case Player.Stance.Axe:
                speedMultiplier = axeMultiplier; break;
            case Player.Stance.Pistol:
                speedMultiplier = pistolMultiplier; break;
            case Player.Stance.Rifle:
                speedMultiplier = rifleMultiplier; break;
        }
    }

    private void CalculateTargetSpeed()
    {
        float inputMagnitude = GameInput.instance.GetMoveDirection().magnitude;
        if (GameInput.instance.isSprintPressed)
        {
            isSprinting = true;
        }
        if (player.rb.linearVelocity == Vector3.zero)
        {
            isSprinting = false;
        }

        if (inputMagnitude == 0f)
        {
            targetSpeed = 0f;
        }
        else if (player.currentAction == Player.Action.Blocking || player.currentAction == Player.Action.Aiming)
        {
            targetSpeed = walkSpeed * speedMultiplier;
            CalculateAccelerations(targetSpeed);
        }
        else if (isSprinting)
        {
            targetSpeed = sprintSpeed * speedMultiplier;
            CalculateAccelerations(targetSpeed);
        }
        else if ((inputMagnitude > 0f && inputMagnitude < inputMagnitudeThreshold) || GameInput.instance.isWalkPressed)
        {
            if (targetSpeed == runSpeed * speedMultiplier) return;
            targetSpeed = walkSpeed * speedMultiplier;
            CalculateAccelerations(targetSpeed);
        }
        else if (!isSprinting || player.currentAction == Player.Action.Firing)
        {
            targetSpeed = runSpeed * speedMultiplier;
            CalculateAccelerations(targetSpeed);
        }
    }

    private void CalculateAccelerations(float speed)
    {
        acceleration = speed / accelerationTime;
        deceleration = speed / decelerationTime;
    }

    private void HandleFlatMovement()
    {
        if (!player.enableMovement) 
        { 
            player.rb.linearVelocity = Vector3.zero;
            return; 
        }

        if (player.currentAction == Player.Action.Attacking)
        {
            return;
        }

        if (rollDurationTimer > 0f || jumpDurationCounter > 0f) return;

        if (!player.playerCollision.isCloseToGround || player.playerCollision.isSlopeSteep) return;

        if (player.rb.linearVelocity.magnitude > targetSpeed)
        {
            if (player.rb.linearVelocity.magnitude < targetSpeed + speedThreshold)
            {
                player.rb.linearVelocity = targetSpeed * moveDir;
            }
            else
            {
                player.rb.AddForce(-player.rb.linearVelocity.normalized * deceleration * Time.fixedDeltaTime, ForceMode.VelocityChange);
            }
        }
        else
        {
            if(player.rb.linearVelocity.magnitude < targetSpeed + speedThreshold && player.rb.linearVelocity.magnitude > targetSpeed - speedThreshold)
            {
                player.rb.linearVelocity = targetSpeed * moveDir;
            }
            else
            {
                player.rb.AddForce(moveDir * acceleration * Time.fixedDeltaTime, ForceMode.VelocityChange);
            }
        }
    }

    private void HandleVerticalMovement()
    {
        if (player.playerCollision.isSlopeSteep)
        {
            if (player.rb.linearVelocity.y > terminalVelocity)
                player.rb.AddForce(gravityValue * Vector3.down * Time.fixedDeltaTime, ForceMode.VelocityChange);
            else
                player.rb.linearVelocity = new Vector3(player.rb.linearVelocity.x, terminalVelocity, player.rb.linearVelocity.z);

            Vector3 groundNormal = player.playerCollision.GetGroundInfo().normal;
            Vector3 velocityAlongSlope = Vector3.ProjectOnPlane(player.rb.linearVelocity, groundNormal);
            player.rb.linearVelocity = velocityAlongSlope;
        }
        else if (player.playerCollision.isGrounded)
        {
            Vector3 groundPoint = player.playerCollision.GetGroundInfo().point;
            transform.position = new Vector3(transform.position.x, groundPoint.y, transform.position.z);
            player.rb.linearVelocity = new Vector3(player.rb.linearVelocity.x, 0f, player.rb.linearVelocity.z);
        }
        else if (player.playerCollision.isCloseToGround && jumpDurationCounter <= 0f)
        {
            Vector3 groundPoint = player.playerCollision.GetGroundInfo().point;
            transform.position = new Vector3(transform.position.x, groundPoint.y, transform.position.z);
            player.rb.linearVelocity = new Vector3(player.rb.linearVelocity.x, 0f, player.rb.linearVelocity.z);
        }
        else
        {
            if (player.rb.linearVelocity.y > terminalVelocity)
                player.rb.AddForce(gravityValue * Vector3.down * Time.fixedDeltaTime, ForceMode.VelocityChange);
            else
                player.rb.linearVelocity = new Vector3(player.rb.linearVelocity.x, terminalVelocity, player.rb.linearVelocity.z);
        }
    }

    /*private void HandleCollisions()
    {
        if (isOnLedge) return;

        if (player.playerCollision.isCollidingMoveDir)
        {
            var collisionInfo = player.playerCollision.GetCollisionMoveDirInfo();
            player.transform.position = new Vector3(collisionInfo.point.x, transform.position.y, collisionInfo.point.z) +
                new Vector3(collisionInfo.normal.x, 0f, collisionInfo.normal.z).normalized * (player.playerCollision.playerRadius - 0.05f);
            player.rb.velocity -= Vector3.Project(player.rb.velocity, collisionInfo.normal);
        }
        if (player.playerCollision.isCollidingVelocity)
        {
            var collisionInfo = player.playerCollision.GetCollisionVelocityInfo();
            player.transform.position = new Vector3(collisionInfo.point.x, transform.position.y, collisionInfo.point.z) +
                new Vector3(collisionInfo.normal.x, 0f, collisionInfo.normal.z).normalized * (player.playerCollision.playerRadius - 0.05f);
            player.rb.velocity -= Vector3.Project(player.rb.velocity, collisionInfo.normal);
        }
    }*/

    private void HandleJump()
    {
        if (!player.enableJump) return;
        if (rollDurationTimer > 0f)
        {
            jumpDurationCounter = 0f;
            return;
        }

        if (player.playerCollision.isCloseToGround && GameInput.instance.isJumpPressed && jumpDurationCounter <= 0f)
        {
            CalculateJumpVelocity();
            jumpDurationCounter = timeOfFlight;
            player.rb.linearVelocity = Vector3.zero;
            player.rb.AddForce(jumpVelocity, ForceMode.VelocityChange);
        }
        if (jumpDurationCounter > 0f)
        {
            jumpDurationCounter -= Time.deltaTime;
        }
    }

    private void CalculateJumpVelocity()
    {
        float jumpVelocityY = Mathf.Sqrt(2 * gravityValue * jumpHeight);
        timeOfFlight = 2 * jumpVelocityY / gravityValue;
        float jumpVelocityXZ = jumpDistance / timeOfFlight;
        CalculateAccelerations(jumpVelocityXZ);
        jumpVelocity = new Vector3(moveDir.x * jumpVelocityXZ, jumpVelocityY, moveDir.z * jumpVelocityXZ);
    }

    /*private void HandleClimb()
    {
        if (!player.enableClimb) return;

        if (GameInput.instance.isJumpPressed && player.playerCollision.GetClimbPoint() != Vector3.zero && climbDurationTimer <= 0f)
        {
            var wallInfo = player.playerCollision.GetWallHitInfo();
            wallDir = -wallInfo.normal;
            var climbPoint = player.playerCollision.GetClimbPoint();
            if (climbPoint.y - transform.position.y < climbLowHeight)
            {
                climbStartPoint = new Vector3(wallInfo.point.x - climbLowOffset.x * wallDir.x, climbPoint.y - climbLowOffset.y,
                    wallInfo.point.z - climbLowOffset.x * wallDir.z);
                climbEndPoint = new Vector3(wallInfo.point.x, climbPoint.y, wallInfo.point.z) + wallDir * 0.3f;
                climbDuration = player.playerAnimation.climbLowLength;
            }
            else
            {
                climbStartPoint = new Vector3(wallInfo.point.x - climbHighOffset.x * wallDir.x, climbPoint.y - climbHighOffset.y,
                    wallInfo.point.z - climbHighOffset.x * wallDir.z);
                climbEndPoint = new Vector3(wallInfo.point.x, climbPoint.y, wallInfo.point.z) + wallDir * 0.3f;
                climbDuration = player.playerAnimation.climbHighLength;
            }
            climbDurationTimer = climbDuration;
            jumpDurationCounter = 0f;
        }
        if (climbDurationTimer > 0f)
        {
            transform.position = Vector3.Lerp(climbStartPoint, climbEndPoint, 1 - climbDurationTimer / climbDuration);
            climbDurationTimer -= Time.deltaTime;
        }
    }*/

    /*private void HandleLedgeGrab()
    {
        if (!player.enableLedgeGrab) return;

        RaycastHit wallInfo = player.playerCollision.GetWallHitInfo();
        if (GameInput.instance.isJumpPressed && player.playerCollision.GetLedgeHitInfo().point != Vector3.zero && !isOnLedge)
        {
            ledgeInfo = player.playerCollision.GetLedgeHitInfo();
            isOnLedge = true;
            wallDir = -wallInfo.normal;
            transform.position = new Vector3(wallInfo.point.x, ledgeInfo.transform.position.y, wallInfo.point.z) + 
                -wallDir * ledgeOffset.x + Vector3.down * ledgeOffset.y;
            player.rb.velocity = Vector3.zero;
            ledgeHopIntervalTimer = ledgeHopInterval;
        }
        if (GameInput.instance.isRollPressed && isOnLedge)
        {
            isOnLedge = false;
        }

        if (isOnLedge)
        {
            if(player.playerCollision.GetLedgeHitInfo().point != Vector3.zero && ledgeHopIntervalTimer <= 0f)
            {
                ledgeInfo = player.playerCollision.GetLedgeHitInfo();
                hopStartPoint = transform.position;
                hopEndPoint = new Vector3(wallInfo.point.x, ledgeInfo.transform.position.y, wallInfo.point.z) +
                    -wallDir * ledgeOffset.x + Vector3.down * ledgeOffset.y +
                    transform.right * ledgeMoveDir.x * ledgeHopLength;
                ledgeHopDir = ledgeMoveDir;
                if (ledgeMoveDir == Vector2.up)
                {
                    ledgeHopDuration = player.playerAnimation.bracedHangHopUpLength;
                }
                else if (ledgeMoveDir == Vector2.right)
                {
                    ledgeHopDuration = player.playerAnimation.bracedHangHopRightLength;
                }
                else if (ledgeMoveDir == Vector2.left)
                {
                    ledgeHopDuration = player.playerAnimation.bracedHangHopLeftLength;
                }
                else if (ledgeMoveDir == Vector2.down)
                {
                    ledgeHopDuration = player.playerAnimation.bracedHangHopDownLength;
                }
                ledgeHopDurationTimer = ledgeHopDuration;
                ledgeHopIntervalTimer = ledgeHopDuration + ledgeHopInterval;
            }

            if (ledgeHopDurationTimer > 0f)
            {
                transform.position = Vector3.Lerp(hopStartPoint, hopEndPoint, 1 - ledgeHopDurationTimer / ledgeHopDuration);
                ledgeHopDurationTimer -= Time.deltaTime;
            }
            else
                transform.position += ledgeMoveDir.x * transform.right * ledgeMoveSpeed * Time.deltaTime;

            if (ledgeHopIntervalTimer > 0f)
            {
                ledgeHopIntervalTimer -= Time.deltaTime;
            }
        }
    }*/

    private void HandleRoll()
    {
        if (GameInput.instance.isRollPressed && rollIntervalTimer <= 0f && player.playerCollision.isCloseToGround)
        {
            if (!player.enableRoll) return;

            rollIntervalTimer = rollDuration + rollInterval;
            rollDurationTimer = rollDuration;
            rollDir = moveDir == Vector3.zero ? transform.forward : moveDir;
            float rollSpeed = (float)(rollDistance / rollDuration);
            player.rb.linearVelocity = Vector3.zero;
            player.rb.AddForce(rollSpeed * rollDir, ForceMode.VelocityChange);
        }
        if (rollIntervalTimer > 0f)
        {
            rollIntervalTimer -= Time.deltaTime;
        }
        if (rollDurationTimer > 0f)
        {
            rollDurationTimer -= Time.deltaTime;
        }
        else if (rollDurationTimer != -1)
        {
            rollDurationTimer = -1;
            if (player.playerCollision.isCloseToGround)
            {
                var velocity = new Vector3(moveDir.x * targetSpeed, player.rb.linearVelocity.y, moveDir.z * targetSpeed);
                player.rb.linearVelocity = velocity;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, moveDir * 3f);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, player.rb.linearVelocity.normalized * 3f);
        }
    }
}
