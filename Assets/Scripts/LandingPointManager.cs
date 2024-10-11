using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandingPointManager : MonoBehaviour
{
    [Header("Configurations")]
    [SerializeField] private float outerRadius = 10f;
    [SerializeField] private LayerMask moonLayer;
    [SerializeField] private Transform moon;
    [SerializeField] private GameObject landingPointPrefab;
    [SerializeField] bool debug = false;

    private GameObject landingPoint;

    private void Start()
    {
        GenerateLandingPoint();
    }

    public void GenerateLandingPoint()
    {
        // send a raycast from a random point around the moon's outer radius towards the moon
        Vector3 randomPoint = Random.onUnitSphere * outerRadius;
        RaycastHit hit;
        if (Physics.Raycast(moon.position + randomPoint, -randomPoint, out hit, outerRadius, moonLayer))
        {
            if(landingPoint == null)
            {
                // if landing point doesn't exist, create one
                landingPoint = Instantiate(landingPointPrefab, hit.point, Quaternion.identity);
            }

            // align the landing point's up direction with the direction from the moon to the hit point
            landingPoint.transform.up = hit.point - moon.position;
            landingPoint.transform.position = hit.point;
        }
    }

    private void OnDrawGizmos()
    {
        if(debug)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(moon.position, outerRadius);
        }
    }
}
