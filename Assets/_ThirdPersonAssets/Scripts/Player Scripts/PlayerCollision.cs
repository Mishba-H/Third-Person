using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class PlayerCollision : MonoBehaviour
{
    public event EventHandler<OnPlayerGroundedEventArgs> OnPlayerGrounded;
    public class OnPlayerGroundedEventArgs : EventArgs
    {
        public float lastYVelocity;
    }

    [SerializeField] internal LayerMask collisionLayer;
    [SerializeField] private float groundCheckerHeight;
    [SerializeField] private float groundCheckerShort;
    [SerializeField] private float groundCheckerLong;
    public bool isGrounded;
    private bool lastIsGrounded = true;
    internal bool isCloseToGround;
    internal bool isSlopeSteep;
    internal float groundAngle;
    private RaycastHit groundHit;
    private Vector3 groundCheckerPosition;

    [SerializeField] private float maxSlopeAngle;

    /*[SerializeField] internal float playerRadius;
    [SerializeField] private float playerHeight;
    internal bool isCollidingMoveDir;
    internal bool isCollidingVelocity;
    private RaycastHit collisionHitMoveDir;
    private RaycastHit collisionHitVelocity;
    private RaycastHit wallHit;*/

    [SerializeField] internal LayerMask actionLayer;
    [SerializeField] internal LayerMask aimLayer;
    [SerializeField] private float maxAimDistance;
    private RaycastHit aimHit;

    /*[SerializeField] private LayerMask climbLayer;
    [SerializeField] private Vector3 climbCheckerOffset;
    private Vector3 climbCheckerPosition;
    private RaycastHit climbHit;

    [SerializeField] private LayerMask ledgeLayer;
    [SerializeField] private Vector3 ledgeCheckerOffset;
    [SerializeField] private float ledgeCheckerLength;
    private RaycastHit ledgeHit;
    private Vector3 ledgeCheckerPosition;*/

    Player player;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Update()
    {
        CheckGround();
        //DetectCollision();
        //GetClimbPoint();
        //GetLedgeHitInfo();
    }

    private void CheckGround()
    {
        groundCheckerPosition = transform.position + Vector3.up * groundCheckerHeight;
        isGrounded = Physics.Raycast(groundCheckerPosition, Vector3.down, groundCheckerShort, collisionLayer);
        isCloseToGround = Physics.Raycast(groundCheckerPosition, Vector3.down, out groundHit, groundCheckerLong, collisionLayer);

        if (!lastIsGrounded && isGrounded)
        {
            OnPlayerGrounded?.Invoke(this, new OnPlayerGroundedEventArgs
            {
                lastYVelocity = player.rb.linearVelocity.y
            });
        }
        lastIsGrounded = isGrounded;

        var normal = groundHit.normal;
        float normalXZ = Mathf.Sqrt(normal.x * normal.x + normal.z * normal.z);
        groundAngle = Mathf.Atan2(normalXZ, normal.y) * Mathf.Rad2Deg;
        if (groundAngle > maxSlopeAngle)
            isSlopeSteep = true;
        else
            isSlopeSteep = false;
    }

    /*private void DetectCollision()
    {
        var start = transform.position + Vector3.up * (groundCheckerHeight + playerRadius);
        var end = transform.position + Vector3.up * (playerHeight - playerRadius);

        if (Physics.Raycast(groundCheckerPosition, player.playerMovement.moveDir, out RaycastHit moveHit, float.MaxValue, collisionLayer))
        {
            var castDir = new Vector3(-moveHit.normal.x, 0f, -moveHit.normal.z);
            if (Physics.Raycast(groundCheckerPosition, castDir, out collisionHitMoveDir, playerRadius, collisionLayer))
            {
                var slope = Vector3.Angle(collisionHitMoveDir.normal, Vector3.up);
                if (slope > maxSlopeAngle)
                    isCollidingMoveDir = true;
            }
            else if (Physics.CapsuleCast(start, end, playerRadius / 2, castDir, out collisionHitMoveDir, playerRadius / 2, collisionLayer))
            {
                var slope = Vector3.Angle(collisionHitMoveDir.normal, Vector3.up);
                if (slope > maxSlopeAngle)
                    isCollidingMoveDir = true;
            }
            else
            {
                isCollidingMoveDir = false;
            }
        }

        if (Physics.Raycast(groundCheckerPosition, player.rb.velocity, out RaycastHit velocityHit, float.MaxValue, collisionLayer))
        {
            var castDir = new Vector3(-velocityHit.normal.x, 0f, -velocityHit.normal.z);
            if (Physics.Raycast(groundCheckerPosition, castDir, out collisionHitVelocity, playerRadius, collisionLayer))
            {
                var slope = Vector3.Angle(collisionHitVelocity.normal, Vector3.up);
                if (slope > maxSlopeAngle)
                    isCollidingVelocity = true;
            }
            else if (Physics.CapsuleCast(start, end, playerRadius / 2, castDir, out collisionHitVelocity, playerRadius / 2, collisionLayer))
            {
                var slope = Vector3.Angle(collisionHitVelocity.normal, Vector3.up);
                if (slope > maxSlopeAngle)
                    isCollidingVelocity = true;
            }
            else
            {
                isCollidingVelocity = false;
            }
        }
    }*/

    public RaycastHit GetGroundInfo()
    {
        return groundHit;
    }

    /*public RaycastHit GetCollisionMoveDirInfo()
    {
        return collisionHitMoveDir;
    }

    public RaycastHit GetCollisionVelocityInfo()
    {
        return collisionHitVelocity;
    }

    public RaycastHit GetWallHitInfo()
    {
        Physics.Raycast(groundCheckerPosition, transform.forward, out wallHit, float.MaxValue, collisionLayer);
        return wallHit;
    }*/

    /*public Vector3 GetClimbPoint()
    {
        climbCheckerPosition = transform.position + transform.forward * climbCheckerOffset.z + Vector3.up * climbCheckerOffset.y;
        if (Physics.Raycast(climbCheckerPosition, Vector3.down, out climbHit, climbCheckerOffset.y - 0.1f, climbLayer))
            return climbHit.point;
        else
            return climbHit.point;
    }*/

    /*public RaycastHit GetLedgeHitInfo()
    {
        if (!player.playerMovement.isOnLedge)
        {
            ledgeCheckerPosition = transform.position + transform.forward * playerRadius * 0.5f + Vector3.up * playerHeight;
            Physics.Raycast(ledgeCheckerPosition, Vector3.down, out ledgeHit, ledgeCheckerLength, ledgeLayer);
            return ledgeHit;
        }
        else
        {
            ledgeCheckerPosition = transform.position + transform.forward * ledgeCheckerOffset.z + transform.up * ledgeCheckerOffset.y;
            var rayDir = transform.right * player.playerMovement.ledgeMoveDir.x + transform.up * player.playerMovement.ledgeMoveDir.y;
            Physics.Raycast(ledgeCheckerPosition, rayDir, out ledgeHit, ledgeCheckerLength, ledgeLayer);
            return ledgeHit;
        }
    }*/

    public Vector3 GetAimPoint()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        Ray ray = CameraScript.instance.mainCamera.ScreenPointToRay(screenCenter);
        if (Physics.Raycast(ray, out aimHit, maxAimDistance, aimLayer))
        {
            return aimHit.point;
        }
        else
        {
            return ray.GetPoint(maxAimDistance);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawCube(groundHit.point, 0.05f * Vector3.one);
        Gizmos.DrawLine(groundCheckerPosition, groundCheckerPosition + Vector3.down * groundCheckerLong);
        if (Application.isPlaying)
        {
            if (groundAngle != 90)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, groundHit.normal);
            }
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(GetAimPoint(), 0.1f);
            Gizmos.color = Color.green;
            /*var rayDir = transform.right * player.playerMovement.ledgeMoveDir.x + transform.up * player.playerMovement.ledgeMoveDir.y;
            Gizmos.DrawCube(ledgeHit.point, Vector3.one * 0.1f);
            Gizmos.DrawRay(ledgeCheckerPosition, rayDir.normalized * ledgeCheckerLength);*/
        }
        /*Gizmos.color = Color.blue;
        Gizmos.DrawLine(climbCheckerPosition, climbCheckerPosition + Vector3.down * climbCheckerOffset.y);
        Gizmos.DrawCube(climbHit.point, Vector3.one * 0.3f);*/
    }
}
