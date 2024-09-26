using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Cinemachine;

public class ShipController : GravityObject
{
    public bool GyroscopicStabilization = true;

    private Camera cam;

    public Transform moon;

    public ShipDirectionSetter Initial;
    public Rigidbody rb;

    public float thrustStrength = 10f;
    public float turnSpeed = 1f;

    private float rotationX;
    private float rotationY;

    void FixedUpdate()
    {
        // Gravity
        Vector3 gravity = NBodySimulation.CalculateAcceleration(rb.position);
        rb.AddForce(gravity, ForceMode.Acceleration);

        /*
        // Thrusters
        Vector3 thrustDir = transform.TransformVector(thrusterInput);
        rb.AddForce(thrustDir * thrustStrength, ForceMode.Acceleration);

        
        if (numCollisionTouches == 0)
        {
            rb.MoveRotation(smoothedRot);
        }
        */

        if (Input.GetKey(KeyCode.Space))
        {
            rb.AddForce(transform.up * thrustStrength, ForceMode.Acceleration);
        }

        HandleRotation();
    }

    private void Update()
    {
        HandleCamera();
    }

    private void HandleRotation()
    {
        // Store the mouse input as rotations
        Vector2 mouseInput = MouseInput();
        rotationX += mouseInput.x * turnSpeed;
        rotationY += mouseInput.y * turnSpeed;

        // Get the camera's forward and right directions
        Vector3 cameraForward = cam.transform.forward;
        Vector3 cameraRight = cam.transform.right;

        // Calculate the target rotation based on the camera's orientation
        Quaternion targetRotation = Quaternion.LookRotation(cameraForward, cam.transform.up) * Quaternion.Euler(-rotationY, rotationX, 0);
        rb.rotation = targetRotation;
    }

    private Quaternion GyroscopicAligner()
    {
        // Calculate the direction from the ship to the moon
        Vector3 directionToMoon = (moon.position - transform.position).normalized;

        // Calculate the target rotation that aligns the ship's down direction with the direction to the moon
        Quaternion alignDownToMoon = Quaternion.FromToRotation(-transform.up, directionToMoon);

        // Calculate the projected direction of the velocity onto the plane perpendicular to the direction to the moon
        Vector3 velocityProjected = Vector3.ProjectOnPlane(rb.velocity, directionToMoon);

        // Calculate the target rotation that aligns the ship's forward direction with the velocity projected onto the plane
        Quaternion alignForwardToCameraUp = Quaternion.FromToRotation(transform.forward, velocityProjected);

        // Combine the rotations
        Quaternion targetRotation = alignDownToMoon * alignForwardToCameraUp * transform.rotation;

        return targetRotation;
    }

    private void HandleCamera()
    {
        // Calculate the direction from the ship to the moon
        Vector3 directionToMoon = (moon.position - transform.position).normalized;

        // Calculate the projected direction of the velocity onto the plane perpendicular to the direction to the moon
        Vector3 velocityProjected = Vector3.ProjectOnPlane(rb.velocity, directionToMoon);

        // Calculate the target rotation that aligns the camera's up direction with the projected velocity
        Quaternion alignUpToVelocity = Quaternion.FromToRotation(cam.transform.up, velocityProjected);

        // Calculate the target rotation that aligns the camera's forward direction with the direction to the moon
        Quaternion alignForwardToMoon = Quaternion.LookRotation(directionToMoon, velocityProjected);

        // Combine the rotations
        Quaternion targetRotation = alignForwardToMoon * alignUpToVelocity;

        // Smoothly interpolate the camera's current rotation towards the target rotation
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, targetRotation, 5f * Time.deltaTime);

        // Set the camera's position to be behind the ship
        cam.transform.position = transform.position + (-directionToMoon * 10);
    }

    private Vector2 MouseInput()
    {
        return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
    }

    private void Start()
    {
        InitRigidbody();
        SetVelocity(Initial.Velocity());
        cam = Camera.main;

        // Lock the cursor to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
    }

    void InitRigidbody()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.centerOfMass = Vector3.zero;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    public void SetVelocity(Vector3 velocity)
    {
        rb.velocity = velocity;
    }
}
