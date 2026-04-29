using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallRunning : MonoBehaviour
{
    [Header("Wall Running")]
    public LayerMask whatIsWall;
    public LayerMask whatIsGround;

    public float wallRunForce;
    public float wallJumpUpForce;
    public float wallJumpSideForce;
    public float wallClimbSpeed;
    [Range(0.1f, 1f)] public float sprintSpeedMultiplier = 0.9f;
    public float maxWallRunTime;
    private float wallRunTimer;

    [Header("Input")]
    public KeyCode upwardsRunKey = KeyCode.LeftShift;
    public KeyCode downwardsRunKey = KeyCode.LeftControl;

    public KeyCode jumpKey = KeyCode.Space;
    private bool upwardsRunning;
    private bool downwardsRunning;
    private float horizontalInput;
    private float verticalInput;

    [Header(("Detection"))]
    public float wallCheckDistance;
    public float minJumpHeight;
    private RaycastHit leftWallhit;
    private RaycastHit rightWallhit;
    private bool wallLeft;
    private bool wallRight; 

    [Header("Exiting")]
    public float exitWallTime;
    private bool exitingWall;
    private float exitWallTimer;

    [Header("Gravity")]
    public float gravityCounterForce;
    public bool useGravity;

    [Header("References")]
    public Transform orientation;
    public PlayerCam playerCam;
    private PlayerMovement playerMovement;
    private Rigidbody rb;
    public LedgeGrabbing lg;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        lg = GetComponent<LedgeGrabbing>();
    }

    private void Update()
    {
        CheckForWall();
        StateMachine();

        // accumulate wall run time while wall-running and enforce max duration
        if (playerMovement != null && playerMovement.wallRunning)
        {
            wallRunTimer += Time.deltaTime;
            if (wallRunTimer > maxWallRunTime)
                StopWallRun();
        }

    }

    private void FixedUpdate()
    {
        if (playerMovement.wallRunning)
            WallRunningMovement();
    }
    private void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallhit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallhit, wallCheckDistance, whatIsWall);
    }

    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
    }   

    private void StateMachine()
    {

        //get inputs
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        upwardsRunning = Input.GetKey(upwardsRunKey);
        downwardsRunning = Input.GetKey(downwardsRunKey);

        //State 1 - Wallrunning
        if ((wallLeft || wallRight) && verticalInput > 0 && AboveGround() && !exitingWall)
        {
            if (!playerMovement.wallRunning)
                StartWallRun();

            if (wallRunTimer > 0)
                wallRunTimer -= Time.deltaTime;

            if(wallRunTimer <= 0 && playerMovement.wallRunning) {
                exitingWall = true;
                exitWallTimer = exitWallTime;
            }

            //Wall Jump
            if (Input.GetKeyDown(jumpKey))
                WallJump();
        }

        //State 2 - Exiting
        else if (exitingWall)
        {
            if (playerMovement.wallRunning)
                StopWallRun();

            if (exitWallTimer > 0)
                exitWallTimer -= Time.deltaTime;

            if (exitWallTimer <= 0)
                exitingWall = false;
        }
        //State 3 - None
        else
        {
            if (playerMovement.wallRunning)
                StopWallRun();
        }
    }

    private void StartWallRun()
    {
        playerMovement.wallRunning = true;
        wallRunTimer = maxWallRunTime;

        rb.useGravity = false;

        if (wallLeft)
            rb.AddForce(orientation.forward * wallRunForce, ForceMode.Force);
        else if (wallRight)
            rb.AddForce(orientation.forward * wallRunForce, ForceMode.Force);

        
        
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

        //apply camera effects
        playerCam.DoFov(100);
        if(wallLeft)
            playerCam.DoTile(-5);
        else if (wallRight)
            playerCam.DoTile(5);

        // timer handled in Update()
    }

    private void StopWallRun()
    {
        playerMovement.wallRunning = false;
        rb.useGravity = true;
        wallRunTimer = 0f;

        playerCam.DoFov(60);
        playerCam.DoTile(0);
    }

    private void WallRunningMovement()
    {

        rb.useGravity = useGravity;

        Vector3 wallNormal = wallRight ? rightWallhit.normal : leftWallhit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
            wallForward = -wallForward;

        //forward force
        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);
        LimitWallRunSpeed(wallForward);

        //upwards/downwards force
        if (upwardsRunning)
            rb.velocity = new Vector3(rb.velocity.x, wallClimbSpeed, rb.velocity.z);
        else if (downwardsRunning)
            rb.velocity = new Vector3(rb.velocity.x, -wallClimbSpeed, rb.velocity.z);

        //push to wall
        if(!(wallLeft && horizontalInput > 0) && !(wallRight && horizontalInput < 0))
            rb.AddForce(-wallNormal * 100, ForceMode.Force);

        //weaken gravity
        if (useGravity)
            rb.AddForce(transform.up * gravityCounterForce, ForceMode.Force);
      
    }

    private void LimitWallRunSpeed(Vector3 wallForward)
    {
        float maxWallRunSpeed = playerMovement.sprintSpeed * sprintSpeedMultiplier;
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(rb.velocity, transform.up);

        if (horizontalVelocity.magnitude <= maxWallRunSpeed)
            return;

        Vector3 limitedVelocity = wallForward.normalized * maxWallRunSpeed;
        rb.velocity = new Vector3(limitedVelocity.x, rb.velocity.y, limitedVelocity.z);
    }

    public void WallJump()
    {

        if(lg.holding || lg.exitingLedge) return;
        //enter exiting wall state
        exitingWall = true;
        exitWallTimer = exitWallTime;

        Vector3 wallNormal = wallRight ? rightWallhit.normal : leftWallhit.normal;
        Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;

        // reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        //add force
        rb.AddForce(forceToApply, ForceMode.Impulse);
    }
}
