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

    [Header("Slide Audio")]
    [SerializeField] private AudioSource slideAudioSource;
    [SerializeField] private AudioSource slideLoopAudioSource;
    [SerializeField] private AudioSource slideTickAudioSource;
    [SerializeField] private AudioClip slideStartClip;
    [SerializeField] private AudioClip slideLoopClip;
    [SerializeField] private AudioClip slideEndClip;
    [SerializeField] private AudioClip[] slideTickClips;
    [SerializeField] private float slideStartVolume = 0.8f;
    [SerializeField] private float slideLoopVolume = 0.65f;
    [SerializeField] private float slideEndVolume = 0.8f;
    [SerializeField] private float slideTickVolume = 0.55f;
    [SerializeField] private Vector2 slideTickIntervalRange = new Vector2(0.12f, 0.24f);
    [SerializeField] private Vector2 slideTickSpeedRange = new Vector2(2f, 18f);
    [Range(0f, 0.5f)]
    [SerializeField] private float slideTickIntervalJitter = 0.2f;
    [SerializeField] private Vector2 slidePitchRange = new Vector2(0.95f, 1.05f);
    private float slideTickTimer;

    [Header("Input")]
    public KeyCode slideKey = KeyCode.C;
    private float horizontalInput;
    private float verticalInput;    


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        if (slideAudioSource == null)
        {
            slideAudioSource = gameObject.AddComponent<AudioSource>();
            slideAudioSource.playOnAwake = false;
        }
        if (slideLoopAudioSource == null)
        {
            slideLoopAudioSource = gameObject.AddComponent<AudioSource>();
            slideLoopAudioSource.playOnAwake = false;
        }
        if (slideTickAudioSource == null)
        {
            slideTickAudioSource = gameObject.AddComponent<AudioSource>();
            slideTickAudioSource.playOnAwake = false;
        }

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
        {
            SlideingMovement();
            UpdateSlideTicks();
        }
    }

    void StartSlide()
    {
        playerMovement.sliding = true;
        playerObj.localScale = new Vector3(playerObj.localScale.x, playerMovement.crouchYScale, playerObj.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        rb.AddForce(orientation.forward * slideStartBoost, ForceMode.VelocityChange);
        slideStartTime = Time.time;
        slideTickTimer = 0f;
        PlaySlideStartAudio();
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
        if (!playerMovement.sliding)
            return;

        playerMovement.sliding = false;
        playerObj.localScale = new Vector3(playerObj.localScale.x, startYScale, playerObj.localScale.z);
        PlaySlideEndAudio();
    }

    private void PlaySlideStartAudio()
    {
        if (slideAudioSource != null && slideStartClip != null)
        {
            slideAudioSource.pitch = Random.Range(slidePitchRange.x, slidePitchRange.y);
            slideAudioSource.volume = 1f;
            slideAudioSource.PlayOneShot(slideStartClip, slideStartVolume);
        }

        if (slideLoopAudioSource == null || slideLoopClip == null)
            return;

        slideLoopAudioSource.pitch = Random.Range(slidePitchRange.x, slidePitchRange.y);
        slideLoopAudioSource.clip = slideLoopClip;
        slideLoopAudioSource.loop = true;
        slideLoopAudioSource.volume = slideLoopVolume;
        slideLoopAudioSource.Play();
    }

    private void UpdateSlideTicks()
    {
        if (slideTickAudioSource == null || slideTickClips == null || slideTickClips.Length == 0)
            return;

        slideTickTimer -= Time.fixedDeltaTime;

        if (slideTickTimer > 0f)
            return;

        AudioClip tickClip = slideTickClips[Random.Range(0, slideTickClips.Length)];
        if (tickClip != null)
        {
            slideTickAudioSource.pitch = Random.Range(slidePitchRange.x, slidePitchRange.y);
            slideTickAudioSource.volume = 1f;
            slideTickAudioSource.PlayOneShot(tickClip, slideTickVolume);
        }

        slideTickTimer = GetNextSlideTickInterval();
    }

    private float GetNextSlideTickInterval()
    {
        float minInterval = Mathf.Min(slideTickIntervalRange.x, slideTickIntervalRange.y);
        float maxInterval = Mathf.Max(slideTickIntervalRange.x, slideTickIntervalRange.y);
        float minSpeed = Mathf.Min(slideTickSpeedRange.x, slideTickSpeedRange.y);
        float maxSpeed = Mathf.Max(slideTickSpeedRange.x, slideTickSpeedRange.y);
        float flatSpeed = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;
        float speedPercent = Mathf.InverseLerp(minSpeed, maxSpeed, flatSpeed);

        float speedInterval = Mathf.Lerp(maxInterval, minInterval, speedPercent);
        float jitter = speedInterval * slideTickIntervalJitter;

        return Mathf.Max(0.05f, Random.Range(speedInterval - jitter, speedInterval + jitter));
    }

    private void PlaySlideEndAudio()
    {
        if (slideLoopAudioSource != null && slideLoopAudioSource.clip == slideLoopClip)
            slideLoopAudioSource.Stop();

        if (slideLoopAudioSource != null)
            slideLoopAudioSource.loop = false;

        if (slideAudioSource != null && slideEndClip != null)
        {
            slideAudioSource.pitch = Random.Range(slidePitchRange.x, slidePitchRange.y);
            slideAudioSource.volume = 1f;
            slideAudioSource.PlayOneShot(slideEndClip, slideEndVolume);
        }
    }
}
