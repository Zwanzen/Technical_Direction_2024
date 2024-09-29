using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Cinemachine;
using Unity.Mathematics;
using UnityEngine.UI;
using System;

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
    public float shipTurnSpeed = 1f;
    public float cameraTurnSpeed = 5f;

    // LANDING LEGS CONTROLLER
    [Space(20)]
    [Header("Landing Legs")]
    [SerializeField] private Transform[] IKTargets = new Transform[3];
    [SerializeField] private Transform[] ArmatureOriginPoints = new Transform[3]; // used to calculate the down direction from the legs
    [Space(5)]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float legcastDistance = 3f;
    [SerializeField] private float legcastHeight = 1f;
    [Space(5)]
    [SerializeField] private float landingCastDistance = 1f;
    [SerializeField] private float landingSpringForce = 10f;

    private Transform[] IKStartTransformCopies = new Transform[3];



    private void Start()
    {
        InitializeShip();
    }

    private void FixedUpdate()
    {
        // Gravity
        Vector3 gravity = NBodySimulation.CalculateAcceleration(rb.position);
        rb.AddForce(gravity, ForceMode.Acceleration);


        // Inputs should not be handled here, Need to change this
        if (Input.GetMouseButton(0))
        {
            rb.AddForce(transform.up * thrustStrength * thrustSlider.value, ForceMode.Acceleration);
        }
        else
        {
            thrustSlider.value -= 1f * Time.fixedDeltaTime;
        }

        HandleRotation();

        HandleLanding();
    }

    private void Update()
    {
        HandleCamera();
        HandleIKLegs();
    }

    // All input should be handled in the update function
    // This is to get the most responsive input you can from your system
    // It will activate every frame, while FixedUpdate will only activate every physics update
    private void InputHandler()
    {

    }

    // Calculate the direction to the moon
    // Comment only to make it more visually pleasing to look at the code :)
    private Vector3 DirectionToMoon()
    {
        return (moon.position - transform.position).normalized;
    }

    // Take the velocity of the ship and project it onto the plane defined by the direction to the moon
    // This gives me the velocity of the ship relative to the moon
    // This is both used in runtime and in the editor, If statement makes sure it uses the correct projection
    private Vector3 VelocityProjected()
    {
        if (Application.isPlaying)
        {
            return Vector3.ProjectOnPlane(rb.velocity, DirectionToMoon());
        }

        return Vector3.ProjectOnPlane(Initial.Velocity(), DirectionToMoon());
    }

    // Calculate the target rotation that aligns the up direction to velocity projected on the direction to the moon
    // And the forward direction to the direction to the moon
    // Need to add some sort of controlable/dynamic offset to the camera
    // And rename the method when I do
    private Quaternion TargetCameraRotation()
    {
        return Quaternion.LookRotation(DirectionToMoon(), VelocityProjected());
    }
    private void HandleRotation()
    {
        // Get input for yaw, pitch, and roll
        float yaw = -Input.GetAxisRaw("Horizontal"); // A/D or Left/Right arrow keys
        float pitch = Input.GetAxisRaw("Vertical"); // W/S or Up/Down arrow keys
        float roll = Input.GetAxisRaw("Roll"); // Q/E

        // Calculate the local torque to apply
        // Reordered to feel better, need to rename variables
        Vector3 localTorque = new Vector3(pitch, roll, yaw) * shipTurnSpeed;

        // Transform the local torque to world space
        Vector3 worldTorque = transform.TransformDirection(localTorque);

        // Apply the torque to the rigidbody
        rb.AddTorque(worldTorque, ForceMode.Acceleration);
    }

    private Quaternion GyroscopicAligner()
    {
        // Calculate the target rotation that aligns the ship's down direction with the direction to the moon
        Quaternion alignDownToMoon = Quaternion.FromToRotation(-transform.up, DirectionToMoon());

        // Calculate the target rotation that aligns the ship's forward direction with the velocity projected onto the plane
        Quaternion alignForwardToCameraUp = Quaternion.FromToRotation(transform.forward, VelocityProjected());

        // Combine the rotations
        Quaternion targetRotation = alignDownToMoon * alignForwardToCameraUp * transform.rotation;

        return targetRotation;
    }

    private void HandleCamera()
    {
        // Set the camera's position, I was really tired when I wrote this
        cam.transform.position = transform.position + (-DirectionToMoon() * cameraOffset.z) + (cam.transform.right.normalized * cameraOffset.x) + (cam.transform.up.normalized * cameraOffset.y);

        // Smoothly interpolate the camera's current rotation towards the target rotation
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, TargetCameraRotation(), cameraTurnSpeed * Time.deltaTime);
    }

    private void HandleIKLegs()
    {
        // first cast a ray from each leg downwards
        for (int i = 0; i < 3; i++)
        {
            RaycastHit hit;
            Transform currentCopy = IKStartTransformCopies[i];

            // If the ray hits the ground, set the IK target to the hit point, else set it to the start position
            if (Physics.Raycast(currentCopy.position + (currentCopy.up * legcastHeight), -transform.up, out hit, legcastDistance + legcastHeight, groundLayer))
            {
                IKTargets[i].position = hit.point;
            }
            else
            {
                IKTargets[i].position = currentCopy.position;
            }
        }
    }

    // controls the damping of the ship when landing
    private void HandleLanding()
    {
        // cast a ray to see if the ship is close enough to the ground to land
        RaycastHit hit;
        Vector3 rayStartOffset = -transform.up * 2f; // sphere has a radius of 2, start the ray at that distance
        if (Physics.Raycast(rayStartOffset + transform.position, -transform.up, out hit, landingCastDistance, groundLayer))
        {
            // if the ship is close enough, apply force depending on the distance to the ground
            float distance = Vector3.Distance(transform.position, hit.point);
            rb.AddForce(transform.up * distance * landingSpringForce, ForceMode.Acceleration);

            // also add forces to stop the ship from rotating
            rb.AddTorque(-rb.angularVelocity * landingSpringForce, ForceMode.Acceleration);

        }

        Debug.DrawLine(rayStartOffset + transform.position, -transform.up, color:Color.magenta);
    }

    private void InitializeCamera()
    {
        cam = Camera.main;

        // Set the camera's position, I was really tired when I wrote this
        cam.transform.position = transform.position + (-DirectionToMoon() * cameraOffset.z) + (cam.transform.right.normalized * cameraOffset.x) + (cam.transform.up.normalized * cameraOffset.y);

        // Instantly set the camera's rotation to the target rotation
        cam.transform.rotation = TargetCameraRotation();
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

    private void InitializeLegs()
    {
        for (int i = 0; i < 3; i++)
        {
            IKStartTransformCopies[i] = new GameObject().transform;
            IKStartTransformCopies[i].parent = transform;
            IKStartTransformCopies[i].position = IKTargets[i].position;
            IKStartTransformCopies[i].rotation = transform.rotation;
        }
    }

    private void InitializeShip()
    {
        // Get and Set the initial velocity of the ship
        InitRigidbody();
        SetVelocity(Initial.Velocity());

        // Position the camera relative to the ships rigidbody
        // Has to be below InitRigidbody()
        InitializeCamera();
        InitializeLegs();

    }
}
