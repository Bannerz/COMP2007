using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sliding : MonoBehaviour
{

    [Header("References")]
    public Transform orientation;

    public Transform playerObj;

    private Rigidbody rb;

    private PlayerMovement playerMovement;

    [Header("Sliding")]
    public float slideForce;
    public float slideStartBoost = 10f;
    public float slideFriction = 2f;
    public float minSlideSpeed = 1.5f;
    public float slideMomentumGraceTime = 0.25f;
    private float slideStartTime;

    private float startYScale;

    [Header("Input")]
    public KeyCode slideKey = KeyCode.C;
    private float horizontalInput;
    private float verticalInput;    


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();

        startYScale = playerObj.localScale.y;
    }

    void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");  

        if (playerMovement.sliding && (Input.GetKeyDown(slideKey) || Input.GetKeyDown(playerMovement.jumpKey)))
        {
            StopSlide();
            return;
        }

        if (Input.GetKeyDown(slideKey) && playerMovement.state == PlayerMovement.MovementState.sprinting)
            StartSlide();
    }

    private void FixedUpdate()
    {
        if (playerMovement.sliding)
            SlideingMovement();
    }

    void StartSlide()
    {
        playerMovement.sliding = true;
        playerObj.localScale = new Vector3(playerObj.localScale.x, playerMovement.crouchYScale, playerObj.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        rb.AddForce(orientation.forward * slideStartBoost, ForceMode.VelocityChange);
        slideStartTime = Time.time;
    }

    void SlideingMovement()
    {
        Vector3 flatVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // sliding normal / uphill: bleed momentum over time
        if (!playerMovement.OnSlope() || rb.velocity.y > -0.1f)
        {
            Vector3 slowedVelocity = Vector3.MoveTowards(flatVelocity, Vector3.zero, slideFriction * Time.fixedDeltaTime);
            rb.velocity = new Vector3(slowedVelocity.x, rb.velocity.y, slowedVelocity.z);

        }

        // sliding downhill: gravity/force keeps the slide alive
        else
        {
            Vector3 downhillDirection = playerMovement.GetSlopeMoveDirection(Vector3.down);
            rb.AddForce(downhillDirection * slideForce, ForceMode.Force);
        }
        

       

       flatVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
       if(Time.time - slideStartTime > slideMomentumGraceTime && flatVelocity.magnitude <= minSlideSpeed)
            StopSlide();

    }

    void StopSlide()
    {
        playerMovement.sliding = false;
        playerObj.localScale = new Vector3(playerObj.localScale.x, startYScale, playerObj.localScale.z);
    }
}
