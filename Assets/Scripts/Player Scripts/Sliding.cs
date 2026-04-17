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
    public float maxSlideTime;
     public float slideForce;
    private float slideTimer;

    public float slideYScale;
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

        if (Input.GetKeyDown(slideKey) && (horizontalInput != 0 || verticalInput != 0))
            StartSlide();

        if (Input.GetKeyUp(slideKey) && playerMovement.sliding)
            StopSlide();
    }

    private void FixedUpdate()
    {
        if (playerMovement.sliding)
            SlideingMovement();
    }

    void StartSlide()
    {
        playerMovement.sliding = true;
        playerObj.localScale = new Vector3(playerObj.localScale.x, slideYScale, playerObj.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        slideTimer = maxSlideTime;
    }

    void SlideingMovement()
    {
       Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

       //sliding normal
        if(!playerMovement.OnSlope() || rb.velocity.y > -0.1f)
        {
            rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);

            slideTimer -= Time.deltaTime;
        }

        //sliding on slope
        else
        {
            rb.AddForce(playerMovement.GetSlopeMoveDirection(inputDirection) * slideForce, ForceMode.Force);        }
        

       

       if(slideTimer <= 0)
            StopSlide();

    }

    void StopSlide()
    {
        playerMovement.sliding = false;
        playerObj.localScale = new Vector3(playerObj.localScale.x, startYScale, playerObj.localScale.z);
    }
}
