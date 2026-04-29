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

    [Header("Hotbar")]
    [SerializeField] private HotbarController hotbar;
    [SerializeField] private int requiredHotbarSlot = 0;

    [Header("Input")]
    public KeyCode grappleKey = KeyCode.Q;
    public float holdThreshold = 0.18f;

    [Header("Pull Grapple")]
    public float maxGrappleDistance = 30f;
    public float grappleDelayTime = 0.1f;
    public float overshootYAxis = 2f;
    public float grapplePullForce = 55f;
    public float grappleUpForce = 8f;
    public float grappleMaxSpeed = 32f;
    public float grappleReleaseDistance = 3f;
    public float maxGrapplePullTime = 1.1f;

    [Header("Swing")]
    public float swingMaxDistance = 25f;
    public float horizontalThrustForce = 32f;
    public float forwardThrustForce = 38f;
    public float extendCableSpeed = 8f;
    public float reelCableSpeed = 10f;
    public float swingAutoReleaseSpeed = 4f;
    public float lowMomentumGraceTime = 0.45f;
    public float swingReleaseBoost = 4f;

    [Header("Swing Entry Assist")]
    public float swingEntryPullForce = 38f;
    public float swingEntryTangentBoost = 11f;
    public float swingEntryUpBoost = 4f;
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
    private float grapplePullTimer;

    private void Start()
    {
        pm = GetComponent<PlayerMovement>();

        if (rb == null) rb = GetComponent<Rigidbody>();
        if (player == null) player = transform;
        if (orientation == null) orientation = transform;
        if (hotbar == null) hotbar = FindObjectOfType<HotbarController>();
    }

    private void Update()
    {
        if (!IsEquipped())
        {
            HidePrediction();
            CancelActiveGrapple();
            return;
        }

        if (grapplingCdTimer > 0f)
            grapplingCdTimer -= Time.deltaTime;

        UpdatePrediction();
        HandleInput();
        HandleSwingAutoRelease();
    }

    private void FixedUpdate()
    {
        if (!IsEquipped())
            return;

        if (grappling)
            GrapplePullMovement();

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
        pm.activeGrapple = true;
        if (pm.cam != null)
            pm.cam.DoFov(pm.grappleFov);
        grapplePullTimer = 0f;

        CancelInvoke(nameof(ExecuteGrapple));
        CancelInvoke(nameof(StopGrapple));
        Invoke(nameof(ExecuteGrapple), grappleDelayTime);
    }

    private void ExecuteGrapple()
    {
        CancelInvoke(nameof(StopGrapple));
        Invoke(nameof(StopGrapple), maxGrapplePullTime);
    }

    private void GrapplePullMovement()
    {
        if (grapplePoint == Vector3.zero) return;

        grapplePullTimer += Time.fixedDeltaTime;

        Vector3 directionToPoint = (grapplePoint - transform.position).normalized;
        rb.AddForce(directionToPoint * grapplePullForce, ForceMode.Acceleration);
        rb.AddForce(Vector3.up * grappleUpForce, ForceMode.Acceleration);
        LimitVelocity(grappleMaxSpeed);

        if (Vector3.Distance(transform.position, grapplePoint) <= grappleReleaseDistance || grapplePullTimer >= maxGrapplePullTime)
            StopGrapple();
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
        if (pm.cam != null)
            pm.cam.DoFov(pm.grappleFov);
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
        joint.minDistance = clampedDistance * 0.15f;
        joint.spring = 6f;
        joint.damper = 2.25f;
        joint.massScale = 2.5f;

        Vector3 directionToPoint = (grapplePoint - player.position).normalized;
        Vector3 tangentDirection = Vector3.ProjectOnPlane(orientation.forward, directionToPoint).normalized;
        if (tangentDirection.sqrMagnitude < 0.01f)
            tangentDirection = Vector3.ProjectOnPlane(cam.forward, directionToPoint).normalized;

        rb.AddForce(directionToPoint * swingEntryPullForce, ForceMode.VelocityChange);
        rb.AddForce(tangentDirection * swingEntryTangentBoost, ForceMode.VelocityChange);
        rb.AddForce(Vector3.up * swingEntryUpBoost, ForceMode.VelocityChange);
        LimitVelocity(grappleMaxSpeed);
    }

    private void SwingMovement()
    {
        if (joint == null) return;

        HandleSwingEntryAssist();

        if (Input.GetKey(KeyCode.D))
            rb.AddForce(orientation.right * horizontalThrustForce, ForceMode.Acceleration);

        if (Input.GetKey(KeyCode.A))
            rb.AddForce(-orientation.right * horizontalThrustForce, ForceMode.Acceleration);

        if (Input.GetKey(KeyCode.W))
            rb.AddForce(orientation.forward * horizontalThrustForce, ForceMode.Acceleration);

        if (Input.GetKey(KeyCode.Space))
        {
            Vector3 directionToPoint = grapplePoint - transform.position;
            rb.AddForce(directionToPoint.normalized * forwardThrustForce, ForceMode.Acceleration);

            float distanceFromPoint = Vector3.Distance(transform.position, grapplePoint);
            float reeledDistance = Mathf.Max(distanceFromPoint - reelCableSpeed * Time.fixedDeltaTime, joint.minDistance);
            joint.maxDistance = reeledDistance * 0.8f;
            joint.minDistance = distanceFromPoint * 0.25f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            float extendedDistance = Vector3.Distance(transform.position, grapplePoint) + extendCableSpeed * Time.fixedDeltaTime;
            joint.maxDistance = extendedDistance * 0.8f;
            joint.minDistance = extendedDistance * 0.25f;
        }

        LimitVelocity(grappleMaxSpeed);
    }

    private void HandleSwingEntryAssist()
    {
        if (!swingEntryAssistActive) return;

        swingEntryAssistTimer += Time.fixedDeltaTime;

        Vector3 directionToPoint = (grapplePoint - transform.position).normalized;
        Vector3 planarVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        float heightGain = player.position.y - swingStartHeight;

        rb.AddForce(directionToPoint * swingEntryPullForce, ForceMode.Acceleration);

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
            HidePrediction();
            return;
        }

        if (TryGetTetherPoint(out Vector3 predictedPoint))
        {
            predictionPoint.gameObject.SetActive(true);
            predictionPoint.position = predictedPoint;
        }
        else
        {
            HidePrediction();
        }
    }

    private void HidePrediction()
    {
        if (predictionPoint != null)
            predictionPoint.gameObject.SetActive(false);
    }

    private bool IsEquipped()
    {
        return hotbar == null || hotbar.SelectedSlotIndex == requiredHotbarSlot;
    }

    private void CancelActiveGrapple()
    {
        if (!grappling && !swingActive && !inputBuffered)
            return;

        StopGrapple();
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
        if (swingActive && rb != null)
            rb.AddForce(rb.velocity.normalized * swingReleaseBoost, ForceMode.VelocityChange);

        swingActive = false;
        swingEntryAssistActive = false;
        inputBuffered = false;
        lowMomentumTimer = 0f;
        swingEntryAssistTimer = 0f;
        pm.swinging = false;
        pm.ResetRestrictions();

        if (joint != null)
            Destroy(joint);

        StartCooldown();
    }

    public void StopGrapple()
    {
        CancelInvoke(nameof(ExecuteGrapple));
        CancelInvoke(nameof(StopGrapple));

        pm.freeze = false;
        pm.ResetRestrictions();
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

    private void LimitVelocity(float maxSpeed)
    {
        if (rb.velocity.magnitude <= maxSpeed)
            return;

        rb.velocity = rb.velocity.normalized * maxSpeed;
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
