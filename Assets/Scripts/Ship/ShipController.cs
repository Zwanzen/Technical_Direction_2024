using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Cinemachine;
using Unity.Mathematics;
using UnityEngine.UI;

public class ShipController : GravityObject
{
    public bool GyroscopicStabilization = true;

    private Camera cam;

    public Transform moon;

    public Slider thrustSlider;
    public ShipDirectionSetter Initial;
    public Rigidbody rb;

    public Vector3 cameraOffset;
    public float thrustStrength = 10f;
    public float turnSpeed = 1f;

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

        if (Input.GetMouseButton(0))
        {
            rb.AddForce(transform.up * thrustStrength * thrustSlider.value, ForceMode.Acceleration);
        }
        else
        {
            thrustSlider.value -= 1f * Time.fixedDeltaTime;
        }

        HandleRotation();

    }

    private void Update()
    {
        HandleCamera();
    }

    private void HandleRotation()
    {
        // Get input for yaw, pitch, and roll
        float yaw = -Input.GetAxisRaw("Horizontal"); // A/D or Left/Right arrow keys
        float pitch = Input.GetAxisRaw("Vertical"); // W/S or Up/Down arrow keys
        float roll = Input.GetAxisRaw("Roll"); // Q/E

        // Calculate the local torque to apply
        // Reordered to feel better, need to rename variables
        Vector3 localTorque = new Vector3(pitch, roll, yaw) * turnSpeed;

        // Transform the local torque to world space
        Vector3 worldTorque = transform.TransformDirection(localTorque);

        // Apply the torque to the rigidbody
        rb.AddTorque(worldTorque, ForceMode.Acceleration);
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
        cam.transform.position = transform.position + (-directionToMoon * cameraOffset.z) + (cam.transform.right.normalized * cameraOffset.x) + (cam.transform.up.normalized * cameraOffset.y);
    }

    private void Start()
    {
        InitRigidbody();
        SetVelocity(Initial.Velocity());
        cam = Camera.main;
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
