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
    public float maxClimbTime;
    private float climbTimer;

    private bool climbing;

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
    }

    private void Update()
    {
        WallCheck();
        StateMachine();

        if (climbing && !exitingWall)
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
        else if (wallFront && Input.GetKey(KeyCode.W) && wallLookAngle < maxWallLookAngle && !exitingWall)
        {
            if (!climbing && climbTimer > 0)
                StartClimbing();

            //Timer
            if (climbTimer > 0)
                climbTimer -= Time.deltaTime;
            if (climbTimer <= 0)
                StopClimbing();
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

        if(wallFront && newWall || playerMovement.grounded)
            climbTimer = maxClimbTime;
            climbJumpsLeft = climbJumps;
    }

    private void StartClimbing()
    {
        climbing = true;
        playerMovement.climbing = true;

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
