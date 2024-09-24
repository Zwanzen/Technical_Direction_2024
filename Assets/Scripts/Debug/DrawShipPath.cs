using System.Reflection.Emit;
using UnityEngine;

[ExecuteInEditMode]
public class DrawShipPath : MonoBehaviour
{
    public ShipController ship;
    public CelestialBody OrbitingBody;
    public LayerMask planetMask;
    public int maxSteps = 1000;
    public float timeStep = 0.1f;

    public bool usePhysicsTimeStep;


    public float width = 100;
    public bool useThickLines;

    //temp
    private int timeToImpact;

    private void Awake()
    {
        ship = FindAnyObjectByType<ShipController>();
    }

    private void FixedUpdate()
    {
        if (Application.isPlaying)
        {
            DrawOrbit();
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            DrawOrbit();
        }

    }

    void DrawOrbit()
    {
        //initialize 
        var virtualBody = new VirtualBody(ship);
        var drawPoints = new Vector3[maxSteps];

        //simulate
        for (int step = 0; step < maxSteps; step++)
        {
            //update velocity
            virtualBody.velocity += CalculateAcceleration(virtualBody) * timeStep;

            //break if line collides with planet
            RaycastHit hit;
            if (Physics.Raycast(virtualBody.position, virtualBody.velocity.normalized, out hit, virtualBody.velocity.magnitude * timeStep, planetMask))
            {
                //impact point
                drawPoints[step] = hit.point;
                //reduce the array size to the step count
                System.Array.Resize(ref drawPoints, step + 1);

                //Temp
                float t = step * timeStep;
                if(timeToImpact != (int)t)
                {
                    timeToImpact = (int)t;
                    Debug.Log("Impact in: " + timeToImpact + " Seconds");
                }
                

                break;
            }

            //update position
            Vector3 newPos = virtualBody.position + virtualBody.velocity * timeStep;
            virtualBody.position = newPos;

            drawPoints[step] = newPos;


        }

        // draw paths
        var pathColour = Color.red; //

        if (useThickLines)
        {
            var lineRenderer = this.GetComponent<LineRenderer>();
            lineRenderer.enabled = true;
            lineRenderer.positionCount = drawPoints.Length;
            lineRenderer.SetPositions(drawPoints);
            lineRenderer.startColor = pathColour;
            lineRenderer.endColor = pathColour;
            lineRenderer.widthMultiplier = width;
        }
        else
        {
            for (int i = 0; i < drawPoints.Length - 1; i++)
            {
                Debug.DrawLine(drawPoints[i], drawPoints[i + 1], pathColour);
            }

            // Hide renderer
            var lineRenderer = this.GetComponent<LineRenderer>();
            if (lineRenderer)
            {
                lineRenderer.enabled = false;
            }

        }
    }

    private Vector3 CalculateAcceleration(VirtualBody body)
    {
        Vector3 acceleration = Vector3.zero;

        float sqrDst = (OrbitingBody.Position - body.position).sqrMagnitude;
        Vector3 forceDir = (OrbitingBody.Position - body.position).normalized;
        acceleration += forceDir * Universe.gravitationalConstant * OrbitingBody.mass / sqrDst;


        return acceleration;
    }

    class VirtualBody
    {
        public Vector3 position;
        public Vector3 velocity;
        public float mass;

        public VirtualBody(ShipController ship)
        {
            position = ship.transform.position;
            if (Application.isPlaying)
            {
                velocity = ship.rb.velocity;
            }
            else
            {
                velocity = ship.initialVelocity;
            }
            mass = ship.rb.mass;
        }
    }

    void OnValidate()
    {
        if (usePhysicsTimeStep)
        {
            timeStep = Universe.physicsTimeStep;
        }
    }
}
