using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ShipController : GravityObject
{
    public Vector3 initialVelocity;

    public Rigidbody rb;

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

    }

    private void Start()
    {
        InitRigidbody();
        SetVelocity(initialVelocity);
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
