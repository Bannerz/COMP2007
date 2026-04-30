using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    [Header("References")]
    public Transform cameraPosition;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Rigidbody playerRb;

    [Header("Head Bob")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private float walkBobFrequency = 8f;
    [SerializeField] private float walkBobVerticalAmount = 0.035f;
    [SerializeField] private float walkBobHorizontalAmount = 0.018f;
    [SerializeField] private float runBobFrequency = 11f;
    [SerializeField] private float runBobVerticalAmount = 0.055f;
    [SerializeField] private float runBobHorizontalAmount = 0.028f;
    [SerializeField] private float bobSmoothSpeed = 12f;
    [SerializeField] private float minBobSpeed = 0.2f;
    private float bobTimer;
    private Vector3 currentBobOffset;

    private void Start()
    {
        if (playerMovement == null && cameraPosition != null)
            playerMovement = cameraPosition.GetComponentInParent<PlayerMovement>();

        if (playerRb == null && playerMovement != null)
            playerRb = playerMovement.GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (cameraPosition == null)
            return;

        transform.position = cameraPosition.position + GetHeadBobOffset();
    }

    private Vector3 GetHeadBobOffset()
    {
        if (!enableHeadBob || playerMovement == null || playerRb == null)
        {
            currentBobOffset = Vector3.Lerp(currentBobOffset, Vector3.zero, Time.deltaTime * bobSmoothSpeed);
            return currentBobOffset;
        }

        Vector3 flatVelocity = new Vector3(playerRb.velocity.x, 0f, playerRb.velocity.z);
        bool canBob = flatVelocity.magnitude > minBobSpeed
            && !playerMovement.sliding
            && !playerMovement.activeGrapple
            && !playerMovement.swinging
            && !playerMovement.climbing
            && (playerMovement.grounded
                || playerMovement.state == PlayerMovement.MovementState.wallRunning);

        if (!canBob)
        {
            bobTimer = 0f;
            currentBobOffset = Vector3.Lerp(currentBobOffset, Vector3.zero, Time.deltaTime * bobSmoothSpeed);
            return currentBobOffset;
        }

        bool running = playerMovement.state == PlayerMovement.MovementState.sprinting
            || playerMovement.state == PlayerMovement.MovementState.wallRunning;

        float frequency = running ? runBobFrequency : walkBobFrequency;
        float verticalAmount = running ? runBobVerticalAmount : walkBobVerticalAmount;
        float horizontalAmount = running ? runBobHorizontalAmount : walkBobHorizontalAmount;

        bobTimer += Time.deltaTime * frequency;
        Vector3 targetOffset = Vector3.up * (Mathf.Sin(bobTimer) * verticalAmount)
            + transform.right * (Mathf.Cos(bobTimer * 0.5f) * horizontalAmount);

        currentBobOffset = Vector3.Lerp(currentBobOffset, targetOffset, Time.deltaTime * bobSmoothSpeed);
        return currentBobOffset;
    }
}
