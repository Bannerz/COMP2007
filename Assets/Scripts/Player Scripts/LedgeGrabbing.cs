using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LedgeGrabbing : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement playerMovement;
    public Transform orientation;
    public Rigidbody rb;
    public Transform playerCam;

    [Header("Ledge Grabbing")]
    public float moveToLedgeSpeed;
    public float maxLedgeGrabbingDistance;
    public float minTimeOnLedge = 0.5f;
    private float timeOnLedge;

    public bool holding;

    [Header("Ledge Jumping")]
    public KeyCode jumpKey = KeyCode.Space;
    public float ledgeJumpForwardForce;
    public float ledgeJumpUpForce;

    [Header("Ledge Detection")]
    public float ledgeDetectionLength;
    public float ledgeSphereCastRadius;
    public LayerMask whatIsLedge;

    private Transform lastLedge;
    private Transform currentLedge;

    private RaycastHit ledgeHit;

    [Header("Exiting")]
    public float exitLedgeTime;
    public bool exitingLedge;
    private float exitLedgeTimer;

    private void Update()
    {
        LedgeDetection();
        SubStateMachine();
    }

    private void SubStateMachine()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        bool anyKeyPressed = horizontalInput != 0 || verticalInput != 0;

        //Substate 1 - Holding Ledge
        if (holding)
        {
            FreezeRigidbodyOnLedge();

            timeOnLedge += Time.deltaTime;

            if (timeOnLedge > minTimeOnLedge && anyKeyPressed) ExitLedgeGrab();

            if (Input.GetKeyDown(jumpKey)) LedgeJump();
        }

        //substate 2 - Exiting Ledge
        else if (exitingLedge)
        {
            if (exitLedgeTimer > 0f) exitLedgeTimer -= Time.deltaTime;
            else exitingLedge = false;
                
        }
    }

    private void LedgeDetection()
    {
        bool ledgeDetected = Physics.SphereCast(transform.position, ledgeSphereCastRadius, playerCam.forward, out ledgeHit, ledgeDetectionLength, whatIsLedge);

        if (!ledgeDetected) return;

        float distanceToLedge = Vector3.Distance(transform.position, ledgeHit.point);

        if(ledgeHit.transform == lastLedge) return;

        if (distanceToLedge < maxLedgeGrabbingDistance && !holding)
        {
            EnterLedgeGrab();
        }
        
    }

    private void LedgeJump()
    {
        ExitLedgeGrab();

        Invoke(nameof(DelayedJumpForce), 0.05f);
    }

    private void DelayedJumpForce()
    {
        Vector3 forceToAdd = playerCam.forward * ledgeJumpForwardForce + orientation.up * ledgeJumpUpForce;
        rb.velocity = Vector3.zero;
        rb.AddForce(forceToAdd, ForceMode.Impulse);
    }

    private void EnterLedgeGrab()
    {
        holding = true;
        playerMovement.unlimited = true;
        playerMovement.restricted = true;

        currentLedge = ledgeHit.transform;
        lastLedge = currentLedge;

        rb.useGravity = false;
        rb.velocity = Vector3.zero;
    }

    private void FreezeRigidbodyOnLedge()
    {
        rb.useGravity = false;
        Vector3 directionToLedge = currentLedge.position - transform.position;
        float distanceToLedge = Vector3.Distance(transform.position, currentLedge.position);

        //move player to ledge
        if (distanceToLedge > 1f)
        {
            if(rb.velocity.magnitude < moveToLedgeSpeed)
                rb.AddForce(directionToLedge.normalized * moveToLedgeSpeed * 1000f * Time.deltaTime);
        }

        //hold onto ledge
        else
        {
            if (!playerMovement.freeze) playerMovement.freeze = true;
            if(playerMovement.unlimited) playerMovement.unlimited = false;
        }

        //exit ledge if something goes wrong
        if (distanceToLedge > maxLedgeGrabbingDistance)
        {
            ExitLedgeGrab();
        }
    }
    
    private void ExitLedgeGrab()
    {
        exitingLedge = true;
        exitLedgeTimer = exitLedgeTime;
        holding = false;
        timeOnLedge = 0f;
        playerMovement.restricted = false;
        playerMovement.freeze = false;
        playerMovement.unlimited = false; // ensure we reset unlimited mode when leaving ledge
        playerMovement.enabled = true;
        // re-enable gravity and ensure reasonable constraints
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Prevent immediate clipping into ground: clear vertical velocity and give a small upward bump
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // If player is very close to ground, nudge upward to avoid penetration
        RaycastHit groundHit;
        float groundCheckDist = 2f;
        float minClearance = 0.6f;
        if (Physics.Raycast(transform.position, Vector3.down, out groundHit, groundCheckDist))
        {
            if (groundHit.distance < minClearance)
            {
                transform.position += Vector3.up * (minClearance - groundHit.distance);
            }
        }

        // small instant upward velocity to separate from geometry
        rb.AddForce(Vector3.up * 2f, ForceMode.VelocityChange);

        Invoke(nameof(ResetLastLedge), 1f);
    }

    private void ResetLastLedge()
    {
        lastLedge = null;
    }
}
