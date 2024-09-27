using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FastIKFabric : MonoBehaviour
{
    public Transform target; // The target the IK chain will try to reach
    public Transform pole; // Optional pole to control the bending direction
    public int chainLength = 2; // Number of bones in the IK chain

    private Transform[] bones;
    private Vector3[] positions;
    private float[] boneLengths;
    private float completeLength;

    void Awake()
    {
        Init();
    }

    void Init()
    {
        bones = new Transform[chainLength + 1];
        positions = new Vector3[chainLength + 1];
        boneLengths = new float[chainLength];

        Transform current = transform;
        completeLength = 0;

        for (int i = chainLength; i >= 0; i--)
        {
            bones[i] = current;
            if (i < chainLength)
            {
                boneLengths[i] = (bones[i + 1].position - current.position).magnitude;
                completeLength += boneLengths[i];
            }
            current = current.parent;
        }
    }

    void LateUpdate()
    {
        ResolveIK();
    }

    void ResolveIK()
    {
        if (target == null) return;

        for (int i = 0; i < bones.Length; i++)
        {
            positions[i] = bones[i].position;
        }

        if ((target.position - bones[0].position).sqrMagnitude >= completeLength * completeLength)
        {
            Vector3 direction = (target.position - positions[0]).normalized;
            for (int i = 1; i < positions.Length; i++)
            {
                positions[i] = positions[i - 1] + direction * boneLengths[i - 1];
            }
        }
        else
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = positions.Length - 1; j > 0; j--)
                {
                    if (j == positions.Length - 1)
                    {
                        positions[j] = target.position;
                    }
                    else
                    {
                        positions[j] = positions[j + 1] + (positions[j] - positions[j + 1]).normalized * boneLengths[j];
                    }
                }

                for (int j = 1; j < positions.Length; j++)
                {
                    positions[j] = positions[j - 1] + (positions[j] - positions[j - 1]).normalized * boneLengths[j - 1];
                }

                if (pole != null)
                {
                    for (int j = 1; j < positions.Length - 1; j++)
                    {
                        Plane plane = new Plane(positions[j + 1] - positions[j - 1], positions[j - 1]);
                        Vector3 projectedPole = plane.ClosestPointOnPlane(pole.position);
                        Vector3 projectedBone = plane.ClosestPointOnPlane(positions[j]);
                        float angle = Vector3.SignedAngle(projectedBone - positions[j - 1], projectedPole - positions[j - 1], plane.normal);
                        positions[j] = Quaternion.AngleAxis(angle, plane.normal) * (positions[j] - positions[j - 1]) + positions[j - 1];
                    }
                }
            }
        }

        for (int i = 0; i < bones.Length - 1; i++)
        {
            bones[i].position = positions[i];
            bones[i].rotation = Quaternion.LookRotation(positions[i + 1] - positions[i]);
        }
        bones[bones.Length - 1].position = positions[bones.Length - 1];
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (target != null)
        {
            Gizmos.DrawSphere(target.position, 0.1f);
        }

        Gizmos.color = Color.yellow;
        if (pole != null)
        {
            Gizmos.DrawSphere(pole.position, 0.1f);
        }

        if (bones != null && bones.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < bones.Length - 1; i++)
            {
                if (bones[i] != null && bones[i + 1] != null)
                {
                    Gizmos.DrawLine(bones[i].position, bones[i + 1].position);
                    Gizmos.DrawSphere(bones[i].position, 0.05f);
                }
            }
        }
    }
}
