using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Climbing : MonoBehaviour
{
   [Header("References")]
    public Transform orientation;

    public Rigidbody rb;

    public LedgeGrabbing lg;
    public LayerMask whatIsWall;

    public PlayerMovement playerMovement;

    [Header("Climbing")]
    public float climbSpeed;
    public PlayerController playerController;
    public float mantleForwardCheck = 1.1f;
    public float mantleUpCheck = 1.4f;
    public float mantleDownCheck = 2.4f;
    public float mantleStandHeight = 1.05f;
    public float mantleDuration = 0.25f;
    public float mantleClearanceRadius = 0.45f;
    public float mantleWallClearanceDistance = 0.75f;
    public LayerMask mantleObstructionMask;

    private bool climbing;
    private bool mantling;

    [Header("Input")]
    public KeyCode climbKey = KeyCode.E;

    [Header("ClimbJumping")]
    public float climbJumpUpForce;
    public float climbJumpBackForce;

    public KeyCode jumpKey = KeyCode.Space;
    public int climbJumps;
    private int climbJumpsLeft;

    [Header("Detection")]
    public float detectionLength;
    public float sphereCastRadius;
    public float maxWallLookAngle;
    private float wallLookAngle;

    private RaycastHit frontWallhit;
    private bool wallFront;

    private Transform lastWall;
    private Vector3 lastWallNormal;
    public float minWallNormalAngleChange;

    [Header("Exiting")]
    public float exitWallTime;
    public bool exitingWall;
    private float exitWallTimer;


    public void Start()
    {
        lg = GetComponent<LedgeGrabbing>();
        if (playerController == null)
            playerController = GetComponentInChildren<PlayerController>();
    }

    private void Update()
    {
        WallCheck();

        if (climbing && !exitingWall && !mantling && CanMantle(out Vector3 mantlePosition))
        {
            StartCoroutine(MantleToPosition(mantlePosition));
            return;
        }

        StateMachine();

        if (climbing && !exitingWall && !mantling)
            ClimbingMovement();
        
    }

    private void StateMachine()
    {

        //Mode - Ledge Grabbing
        if (lg.holding)
        {
            if (climbing)
                StopClimbing();
        }
        //Mode - Climbing
        else if (CanClimb())
        {
            if (!climbing)
                StartClimbing();
        }

        else if (exitingWall)
        {
            if (climbing)
                StopClimbing();

            if (exitWallTimer > 0)
                exitWallTimer -= Time.deltaTime;
            if (exitWallTimer <= 0)
                exitingWall = false;
        }
        else
        {
                if (climbing)
                    StopClimbing();
        }

        if(wallFront && Input.GetKeyDown(jumpKey) && climbJumpsLeft > 0)
        {
            ClimbJump();
        }
    }
    private void WallCheck()
    {
        wallFront = Physics.SphereCast(transform.position, sphereCastRadius, orientation.forward, out frontWallhit, detectionLength, whatIsWall);

        wallLookAngle = Vector3.Angle(orientation.forward, -frontWallhit.normal);

        bool newWall = frontWallhit.transform != lastWall || Mathf.Abs(Vector3.Angle(lastWallNormal, frontWallhit.normal)) > minWallNormalAngleChange;

        if (wallLookAngle > maxWallLookAngle)
            wallFront = false;

        if (wallFront && newWall || playerMovement.grounded)
        {
            climbJumpsLeft = climbJumps;
        }
    }

    private bool CanClimb()
    {
        bool hasStamina = playerController == null || playerController.CanClimb();
        return wallFront && Input.GetKey(climbKey) && wallLookAngle < maxWallLookAngle && !exitingWall && hasStamina;
    }

    private bool CanMantle(out Vector3 mantlePosition)
    {
        mantlePosition = Vector3.zero;

        Vector3 wallClearanceOrigin = transform.position + Vector3.up * mantleUpCheck;
        if (Physics.SphereCast(wallClearanceOrigin, mantleClearanceRadius, orientation.forward, out _, mantleWallClearanceDistance, mantleObstructionMask))
            return false;

        Vector3 checkOrigin = transform.position + Vector3.up * mantleUpCheck + orientation.forward * mantleForwardCheck;
        if (Physics.Raycast(checkOrigin, Vector3.down, out RaycastHit groundHit, mantleDownCheck, playerMovement.whatIsGround))
        {
            mantlePosition = groundHit.point + Vector3.up * mantleStandHeight + orientation.forward * 0.35f;
            return HasMantleClearance(mantlePosition);
        }

        return false;
    }

    private bool HasMantleClearance(Vector3 mantlePosition)
    {
        Vector3 bottom = mantlePosition + Vector3.up * mantleClearanceRadius;
        Vector3 top = mantlePosition + Vector3.up * (mantleStandHeight + mantleClearanceRadius);
        Collider[] overlaps = Physics.OverlapCapsule(bottom, top, mantleClearanceRadius, mantleObstructionMask);

        foreach (Collider overlap in overlaps)
        {
            if (overlap != null && !overlap.transform.IsChildOf(transform))
                return false;
        }

        return true;
    }

    private IEnumerator MantleToPosition(Vector3 targetPosition)
    {
        mantling = true;
        StopClimbing();

        playerMovement.restricted = true;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;

        Vector3 startPosition = transform.position;
        Vector3 upPosition = new Vector3(startPosition.x, targetPosition.y, startPosition.z);
        float elapsed = 0f;

        while (elapsed < mantleDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / (mantleDuration * 0.5f));
            rb.MovePosition(Vector3.Lerp(startPosition, upPosition, t));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < mantleDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / (mantleDuration * 0.5f));
            rb.MovePosition(Vector3.Lerp(upPosition, targetPosition, t));
            yield return null;
        }

        rb.MovePosition(targetPosition);
        rb.useGravity = true;
        playerMovement.restricted = false;
        mantling = false;
    }

    private void StartClimbing()
    {
        climbing = true;
        playerMovement.climbing = true;
        playerController?.SetClimbing(true);

        lastWall = frontWallhit.transform;
        lastWallNormal = frontWallhit.normal;
    }

    private void ClimbingMovement()
    {
        rb.velocity = new Vector3(rb.velocity.x, climbSpeed, rb.velocity.z);
    }

    private void StopClimbing()
    {
        climbing = false;
        playerMovement.climbing = false;
        playerController?.SetClimbing(false);
    }

    public void ClimbJump()
    {

        if(lg.holding || lg.exitingLedge) return;

        StopClimbing();

        exitingWall = true;
        exitWallTimer = exitWallTime;
        climbJumpsLeft--;

        Vector3 forceToApply = frontWallhit.normal * climbJumpBackForce + Vector3.up * climbJumpUpForce;

        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);
    }
}
