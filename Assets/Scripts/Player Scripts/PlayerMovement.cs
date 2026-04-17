using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float slideSpeed;
    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;

    public float wallRunSpeed;

    public float climbSpeed;

    public float speedIncreaseMultiplier;
    public float slopeIncreaseMultiplier;
    public float groundDrag;

    [Header("Responsiveness")]
    public float groundAcceleration = 90f;
    public float groundDeceleration = 110f;
    public float airAcceleration = 30f;
    public float airDeceleration = 18f;
    [Range(0f, 1f)] public float airControl = 0.45f;
    public float groundedStickForce = 8f;
    public float fallGravityMultiplier = 2.4f;
    public float lowJumpGravityMultiplier = 2f;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;

    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;

    public bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    [Header("References")]
    public Climbing climbingScript;

    public Transform orientation;

    float horizontalInput;
    float verticalInput;    

    Vector3 moveDirection;

    Rigidbody rb;

    public MovementState state;
    public enum MovementState
    {
        freeze,
        unlimited,
        walking,
        crouching,
        sprinting,
        climbing,
        sliding,
        wallRunning,
        grappling,
        swinging,
        air
    }

    public bool sliding;
    public bool wallRunning;
    public bool crouching;
    public bool climbing;

    public bool freeze;
    public bool unlimited;

    public bool restricted;

    [Header("Grappling / Swinging")]
    public float swingSpeed = 15f; // used if you add a swing script later
    public bool activeGrapple;
    public bool swinging;

    [Header("Camera Effects")]
    public PlayerCam cam;
    public float grappleFov = 95f;

    private bool enableMovementOnNextTouch;
    private Vector3 velocityToSet;

    void Start()
    {
        moveSpeed = walkSpeed;
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        readyToJump = true;

        startYScale = transform.localScale.y;
    }

    // Update is called once per frame
    void Update()
    {
        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        MyInput();
        StateHandler();

        // handle drag
        if (grounded && !activeGrapple)
            rb.drag = groundDrag;
        else
            rb.drag = 0;
    }

    void FixedUpdate()
    {
        MovePlayer();
        ApplyExtraGravity();
        SpeedControl();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // when to jump
        if (Input.GetKeyDown(jumpKey) && readyToJump && grounded)
        {            readyToJump = false;         
            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        // start crouch
        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        // stop crouch
        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }
    }

    bool keepMomentum;
    private void StateHandler()
    {

        //Mode - Freeze
        if (freeze)
        {
            state = MovementState.freeze;
            moveSpeed = 0f;
            rb.velocity = Vector3.zero;
        }
        //Mode - Unlimited
        else if (unlimited)
        {
            state = MovementState.unlimited;
            moveSpeed = 999f;
            return;
        }
        // Mode - Grappling
        else if (activeGrapple)
        {
            state = MovementState.grappling;
            desiredMoveSpeed = sprintSpeed;
        }

        // Mode - Swinging
        else if (swinging)
        {
            state = MovementState.swinging;
            desiredMoveSpeed = swingSpeed;
        }

        //Mode - Climbing
        else if (climbing)
        {
            state = MovementState.climbing;
            desiredMoveSpeed = climbSpeed;
        }
        //Mode - Wall Running
        else if (wallRunning)
        {
            state = MovementState.wallRunning;
            desiredMoveSpeed = wallRunSpeed;
        }
        //Mode - Sliding
        else if (sliding)
        {
            state = MovementState.sliding;

            if (OnSlope() && rb.velocity.y < 0.1f)
            {
                desiredMoveSpeed = slideSpeed;
                keepMomentum = true;
            }
            else
                desiredMoveSpeed = sprintSpeed;
        }
        // Mode - Crouching
        else if (Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            desiredMoveSpeed = crouchSpeed;
            return;
        }

        // Mode - Sprinting
        else if (Input.GetKey(sprintKey) && grounded)
        {
            state = MovementState.sprinting;
            desiredMoveSpeed = sprintSpeed;
        }
        // Mode - Walking
        else if (grounded)
        {
            state = MovementState.walking;
            desiredMoveSpeed = walkSpeed;
        }
        // Mode - Air
        else
        {
            state = MovementState.air;
        }

        //check if desired move speed has changed drastically

        bool desiredMoveSpeedHasChanged = desiredMoveSpeed != lastDesiredMoveSpeed;

        if (desiredMoveSpeedHasChanged)
        {
            if (keepMomentum)
            {
                StopAllCoroutines();
                StartCoroutine(SmoothlyLerpMoveSpeed());
            }
            else
            {
                moveSpeed = desiredMoveSpeed;
            }
            
        }
            
        lastDesiredMoveSpeed = desiredMoveSpeed;

        if (Mathf.Abs(desiredMoveSpeed - moveSpeed) < 0.1f)
            keepMomentum = false;
    }

    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        while (time < difference)
        {
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);
            if (OnSlope()) {
                float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime * speedIncreaseMultiplier * slopeIncreaseMultiplier * slopeAngleIncrease;
            }
            else
                time += Time.deltaTime * speedIncreaseMultiplier;

            yield return null;
        }

        moveSpeed = desiredMoveSpeed;
    }

    private void MovePlayer()
    {

        if (activeGrapple) return;
        if (swinging) return;
        if (restricted) return;
        if (climbing) return;
        if (wallRunning) return;

        if (climbingScript.exitingWall) return;

        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        Vector3 inputDirection = moveDirection.normalized;
        Vector3 targetDirection = inputDirection;
        float acceleration = grounded ? groundAcceleration : airAcceleration * airMultiplier;
        float deceleration = grounded ? groundDeceleration : airDeceleration * Mathf.Max(airMultiplier, 0.1f);

        if (!grounded)
        {
            targetDirection *= airControl;
        }

        // on slope
        if (OnSlope() && !exitingSlope)
        {
            Vector3 slopeDirection = GetSlopeMoveDirection(targetDirection);
            Vector3 currentSlopeVelocity = Vector3.ProjectOnPlane(rb.velocity, slopeHit.normal);
            Vector3 targetSlopeVelocity = slopeDirection * moveSpeed;
            float slopeChangeRate = targetDirection.sqrMagnitude > 0f ? acceleration : deceleration;

            Vector3 newSlopeVelocity = Vector3.MoveTowards(currentSlopeVelocity, targetSlopeVelocity, slopeChangeRate * Time.fixedDeltaTime);
            Vector3 verticalVelocity = Vector3.Project(rb.velocity, slopeHit.normal);
            rb.velocity = newSlopeVelocity + verticalVelocity;

            if (grounded && rb.velocity.y <= 0f)
                rb.AddForce(Vector3.down * groundedStickForce, ForceMode.Acceleration);

            rb.useGravity = false;
            return;
        }

        Vector3 flatVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        Vector3 targetFlatVelocity = targetDirection * moveSpeed;
        float changeRate = targetDirection.sqrMagnitude > 0f ? acceleration : deceleration;
        Vector3 newFlatVelocity = Vector3.MoveTowards(flatVelocity, targetFlatVelocity, changeRate * Time.fixedDeltaTime);

        rb.velocity = new Vector3(newFlatVelocity.x, rb.velocity.y, newFlatVelocity.z);

        if (grounded && rb.velocity.y <= 0f)
            rb.AddForce(Vector3.down * groundedStickForce, ForceMode.Acceleration);

        rb.useGravity = true;
    }

    private void SpeedControl()
    {

        if (activeGrapple) return;
        if (climbing || wallRunning) return;
        // limit speed on slope
        if (OnSlope() && !exitingSlope) {
            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;
        }

        // limit speed on ground or in air
        else {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            // limit velocity if needed
            if (flatVel.magnitude > moveSpeed)            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        exitingSlope = true;
        // reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ApplyExtraGravity()
    {
        if (activeGrapple || swinging || climbing || wallRunning) return;
        if (grounded || !rb.useGravity) return;

        if (rb.velocity.y < 0f)
        {
            rb.AddForce(Physics.gravity * (fallGravityMultiplier - 1f), ForceMode.Acceleration);
        }
        else if (rb.velocity.y > 0f && !Input.GetKey(jumpKey))
        {
            rb.AddForce(Physics.gravity * (lowJumpGravityMultiplier - 1f), ForceMode.Acceleration);
        }
    }

    private void ResetJump()
    {
        readyToJump = true;

        exitingSlope = false;
    }

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(slopeHit.normal, Vector3.up);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }   

    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
    {
        activeGrapple = true;

        velocityToSet = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight);
        Invoke(nameof(SetVelocity), 0.1f);

        // Safety reset in case we never collide / release properly
        Invoke(nameof(ResetRestrictions), 3f);
    }

    private void SetVelocity()
    {
        enableMovementOnNextTouch = true;
        rb.velocity = velocityToSet;

        cam.DoFov(grappleFov);
    }

    public void ResetRestrictions()
    {
        activeGrapple = false;
        cam.DoFov(60f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enableMovementOnNextTouch) return;

        enableMovementOnNextTouch = false;
        ResetRestrictions();

        // If we hit something early, ensure the grapple releases cleanly
        Grappling g = GetComponent<Grappling>();
        if (g != null) g.StopGrapple();
    }

    public Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        float gravity = Physics.gravity.y;
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2f * gravity * trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2f * trajectoryHeight / gravity)
            + Mathf.Sqrt(2f * (displacementY - trajectoryHeight) / gravity));

        return velocityXZ + velocityY;
    }

}
