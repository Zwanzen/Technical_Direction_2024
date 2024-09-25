using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ShipController : GravityObject
{
    public ShipDirectionSetter Initial;
    public Rigidbody rb;

    public float thrustStrength = 10f;
    public float turnSpeed = 1f;

    private Vector2 mouseDelta;

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

        RotateRigidbody();

    }

    private void Update()
    {
        // Calculate mouse delta
        mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        // Reset mouse delta for next frame
        mouseDelta *= turnSpeed;
    }

    private Vector2 MouseInput()
    {
        Vector2 mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        return mouseInput;
    }

    void RotateRigidbody()
    {
        // Get camera forward vector
        Camera cam = Camera.main;
        Vector3 cameraForward = cam.transform.forward;

        // Calculate desired rotation quaternion
        Quaternion targetRotation = Quaternion.LookRotation(cameraForward);

        // Apply rotation to Rigidbody
        rb.MoveRotation(targetRotation * Quaternion.Euler(-mouseDelta.y, mouseDelta.x, 0));

        // Reset mouse delta for next frame
        mouseDelta = Vector2.zero;
    }

    private void Start()
    {
        InitRigidbody();
        SetVelocity(Initial.Velocity());
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
