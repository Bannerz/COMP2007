using UnityEngine;

public class Grappling : MonoBehaviour
{
    [Header("Shared References")]
    private PlayerMovement pm;
    public Rigidbody rb;
    public Transform cam;
    public Transform gunTip;
    public Transform player;
    public Transform orientation;
    public LayerMask whatIsGrappleable;

    [Header("Input")]
    public KeyCode grappleKey = KeyCode.Q;
    public float holdThreshold = 0.18f;

    [Header("Pull Grapple")]
    public float maxGrappleDistance = 30f;
    public float grappleDelayTime = 0.1f;
    public float overshootYAxis = 2f;

    [Header("Swing")]
    public float swingMaxDistance = 25f;
    public float horizontalThrustForce = 120f;
    public float forwardThrustForce = 140f;
    public float extendCableSpeed = 1f;
    public float swingAutoReleaseSpeed = 4f;
    public float lowMomentumGraceTime = 0.45f;

    [Header("Swing Entry Assist")]
    public float swingEntryPullForce = 180f;
    public float swingEntryMinPlanarSpeed = 9f;
    public float swingEntryMinHeightGain = 2.5f;
    public float swingEntryReelSpeed = 8f;
    public float maxSwingEntryAssistTime = 1.2f;

    [Header("Prediction")]
    public float predictionSphereCastRadius = 1f;
    public Transform predictionPoint;

    [Header("Cooldown")]
    public float grapplingCd = 1f;
    private float grapplingCdTimer;

    private Vector3 grapplePoint;
    private SpringJoint joint;
    private bool grappling;
    private bool swingActive;
    private bool swingEntryAssistActive;
    private bool inputBuffered;
    private float holdTimer;
    private float lowMomentumTimer;
    private float swingEntryAssistTimer;
    private float swingStartHeight;

    private void Start()
    {
        pm = GetComponent<PlayerMovement>();

        if (rb == null) rb = GetComponent<Rigidbody>();
        if (player == null) player = transform;
        if (orientation == null) orientation = transform;
    }

    private void Update()
    {
        if (grapplingCdTimer > 0f)
            grapplingCdTimer -= Time.deltaTime;

        UpdatePrediction();
        HandleInput();
        HandleSwingAutoRelease();
    }

    private void FixedUpdate()
    {
        if (swingActive)
            SwingMovement();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(grappleKey))
            BeginTetherInput();

        if (!inputBuffered)
        {
            if (swingActive && Input.GetKeyUp(grappleKey))
                StopSwing();

            return;
        }

        if (Input.GetKey(grappleKey))
        {
            holdTimer += Time.deltaTime;

            if (!swingActive && holdTimer >= holdThreshold)
                StartSwing();
        }

        if (Input.GetKeyUp(grappleKey))
        {
            if (swingActive)
                StopSwing();
            else
                ExecuteTapGrapple();
        }
    }

    private void BeginTetherInput()
    {
        if (grapplingCdTimer > 0f) return;
        if (grappling || swingActive || pm.activeGrapple) return;

        if (!TryGetTetherPoint(out grapplePoint))
            return;

        inputBuffered = true;
        holdTimer = 0f;
        lowMomentumTimer = 0f;
    }

    private void ExecuteTapGrapple()
    {
        inputBuffered = false;

        if (grapplePoint == Vector3.zero)
            return;

        grappling = true;
        pm.freeze = true;

        CancelInvoke(nameof(ExecuteGrapple));
        CancelInvoke(nameof(StopGrapple));
        Invoke(nameof(ExecuteGrapple), grappleDelayTime);
    }

    private void ExecuteGrapple()
    {
        pm.freeze = false;

        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);
        float grapplePointRelativeYPos = grapplePoint.y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

        if (grapplePointRelativeYPos < 0f)
            highestPointOnArc = overshootYAxis;

        pm.JumpToPosition(grapplePoint, highestPointOnArc);

        CancelInvoke(nameof(StopGrapple));
        Invoke(nameof(StopGrapple), 1f);
    }

    private void StartSwing()
    {
        inputBuffered = false;
        grappling = false;

        if (grapplePoint == Vector3.zero)
            return;

        if (joint != null)
            Destroy(joint);

        pm.ResetRestrictions();
        pm.swinging = true;
        swingActive = true;
        swingEntryAssistActive = true;
        lowMomentumTimer = 0f;
        swingEntryAssistTimer = 0f;
        swingStartHeight = player.position.y;

        joint = player.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = grapplePoint;

        float distanceFromPoint = Vector3.Distance(player.position, grapplePoint);
        float clampedDistance = Mathf.Min(distanceFromPoint, swingMaxDistance);

        joint.maxDistance = clampedDistance * 0.8f;
        joint.minDistance = clampedDistance * 0.25f;
        joint.spring = 4.5f;
        joint.damper = 7f;
        joint.massScale = 4.5f;
    }

    private void SwingMovement()
    {
        if (joint == null) return;

        HandleSwingEntryAssist();

        if (Input.GetKey(KeyCode.D))
            rb.AddForce(orientation.right * horizontalThrustForce * Time.fixedDeltaTime, ForceMode.Acceleration);

        if (Input.GetKey(KeyCode.A))
            rb.AddForce(-orientation.right * horizontalThrustForce * Time.fixedDeltaTime, ForceMode.Acceleration);

        if (Input.GetKey(KeyCode.W))
            rb.AddForce(orientation.forward * horizontalThrustForce * Time.fixedDeltaTime, ForceMode.Acceleration);

        if (Input.GetKey(KeyCode.Space))
        {
            Vector3 directionToPoint = grapplePoint - transform.position;
            rb.AddForce(directionToPoint.normalized * forwardThrustForce * Time.fixedDeltaTime, ForceMode.Acceleration);

            float distanceFromPoint = Vector3.Distance(transform.position, grapplePoint);
            joint.maxDistance = distanceFromPoint * 0.8f;
            joint.minDistance = distanceFromPoint * 0.25f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            float extendedDistance = Vector3.Distance(transform.position, grapplePoint) + extendCableSpeed;
            joint.maxDistance = extendedDistance * 0.8f;
            joint.minDistance = extendedDistance * 0.25f;
        }
    }

    private void HandleSwingEntryAssist()
    {
        if (!swingEntryAssistActive) return;

        swingEntryAssistTimer += Time.fixedDeltaTime;

        Vector3 directionToPoint = (grapplePoint - transform.position).normalized;
        Vector3 planarVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        float heightGain = player.position.y - swingStartHeight;

        rb.AddForce(directionToPoint * swingEntryPullForce * Time.fixedDeltaTime, ForceMode.Acceleration);

        float distanceToPoint = Vector3.Distance(transform.position, grapplePoint);
        float assistedDistance = Mathf.Max(distanceToPoint - swingEntryReelSpeed * Time.fixedDeltaTime, joint.minDistance);
        joint.maxDistance = Mathf.Min(joint.maxDistance, assistedDistance);

        bool hasEnoughMomentum = planarVelocity.magnitude >= swingEntryMinPlanarSpeed;
        bool hasEnoughHeight = heightGain >= swingEntryMinHeightGain;
        bool assistTimedOut = swingEntryAssistTimer >= maxSwingEntryAssistTime;

        if ((hasEnoughMomentum && hasEnoughHeight) || assistTimedOut)
            swingEntryAssistActive = false;
    }

    private void HandleSwingAutoRelease()
    {
        if (!swingActive || joint == null) return;
        if (swingEntryAssistActive)
        {
            lowMomentumTimer = 0f;
            return;
        }

        Vector3 planarVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if (planarVelocity.magnitude <= swingAutoReleaseSpeed)
        {
            lowMomentumTimer += Time.deltaTime;

            if (lowMomentumTimer >= lowMomentumGraceTime)
                StopSwing();
        }
        else
        {
            lowMomentumTimer = 0f;
        }
    }

    private void UpdatePrediction()
    {
        if (predictionPoint == null || cam == null) return;

        if (grappling || swingActive || inputBuffered)
        {
            predictionPoint.gameObject.SetActive(false);
            return;
        }

        if (TryGetTetherPoint(out Vector3 predictedPoint))
        {
            predictionPoint.gameObject.SetActive(true);
            predictionPoint.position = predictedPoint;
        }
        else
        {
            predictionPoint.gameObject.SetActive(false);
        }
    }

    private bool TryGetTetherPoint(out Vector3 point)
    {
        point = Vector3.zero;

        if (cam == null) return false;

        float castDistance = Mathf.Max(maxGrappleDistance, swingMaxDistance);

        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit raycastHit, castDistance, whatIsGrappleable))
        {
            point = raycastHit.point;
            return true;
        }

        if (Physics.SphereCast(cam.position, predictionSphereCastRadius, cam.forward, out RaycastHit sphereCastHit, castDistance, whatIsGrappleable))
        {
            point = sphereCastHit.point;
            return true;
        }

        return false;
    }

    public void StopSwing()
    {
        swingActive = false;
        swingEntryAssistActive = false;
        inputBuffered = false;
        lowMomentumTimer = 0f;
        swingEntryAssistTimer = 0f;
        pm.swinging = false;

        if (joint != null)
            Destroy(joint);

        StartCooldown();
    }

    public void StopGrapple()
    {
        CancelInvoke(nameof(ExecuteGrapple));
        CancelInvoke(nameof(StopGrapple));

        pm.freeze = false;
        grappling = false;
        inputBuffered = false;

        if (swingActive)
            StopSwing();
        else
            StartCooldown();
    }

    private void StartCooldown()
    {
        grapplingCdTimer = grapplingCd;
    }

    public bool IsGrappling()
    {
        return grappling;
    }

    public bool IsSwinging()
    {
        return swingActive;
    }

    public Vector3 GetGrapplePoint()
    {
        return grapplePoint;
    }

    public Vector3 GetSwingPoint()
    {
        return grapplePoint;
    }
}
