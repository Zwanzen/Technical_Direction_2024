using System.Reflection.Emit;
using UnityEngine;

[ExecuteInEditMode]
public class DrawShipPath : MonoBehaviour
{
    public ShipController ship;
    public CelestialBody OrbitingBody;
    public LayerMask rayMask;
    public int maxSteps = 1000;
    public float timeStep = 0.1f;

    public bool usePhysicsTimeStep;

    private LineRenderer line;
    public Color pathColour = Color.clear;
    public float width = 100;
    public bool useThickLines;

    public ParticleSystem ParticleTrail;
    public float particleEmissionRate = 1;
    public int particleStep = 10;
    private float particleTimer = 0;
    private int particleIndex = 0;

    // checks
    public int timeToImpact;
    public bool lostInSpace;
    public bool inOrbit;



    private void Awake()
    {
        ship = FindAnyObjectByType<ShipController>();
        ParticleTrail = GetComponentInChildren<ParticleSystem>();
        line = GetComponent<LineRenderer>();
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

        //DrawDots();

    }

    void DrawDots()
    {
        // spawn a particle on every index of line position at a speed
        particleTimer += Time.deltaTime;
        if (particleTimer >= particleEmissionRate)
        {
            // get the and check the current index of the line
            if (line.positionCount >= particleIndex + particleStep)
            {
                ParticleTrail.transform.position = line.GetPosition(particleIndex);
                ParticleTrail.Emit(1);
                particleIndex += particleStep;
            }
            else
            {

                ParticleTrail.transform.position = line.GetPosition(line.positionCount - 1);
                ParticleTrail.Emit(1);
                particleIndex = 0;
            }


            particleTimer = 0;
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
            if (Physics.Raycast(virtualBody.position, virtualBody.velocity.normalized, out hit, virtualBody.velocity.magnitude * timeStep, rayMask))
            {
                if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Ship"))
                {
                    //impact point
                    drawPoints[step] = ship.transform.position;
                    //reduce the array size to the step count
                    System.Array.Resize(ref drawPoints, step + 1);

                    inOrbit = true;
                    timeToImpact = 0;
                    break;
                }
                else if(hit.transform.gameObject.layer == LayerMask.NameToLayer("Planet"))
                {
                    //impact point
                    drawPoints[step] = hit.point;
                    //reduce the array size to the step count
                    System.Array.Resize(ref drawPoints, step + 1);

                    
                    
                    float t = step * timeStep;
                    if (timeToImpact != (int)t)
                    {
                        timeToImpact = (int)t;
                    }

                    inOrbit = false;
                    break;
                }
                lostInSpace = false;
            }
            else
            {
                timeToImpact = 0;
                lostInSpace = true;
                inOrbit = false;
            }

            //update position
            Vector3 newPos = virtualBody.position + virtualBody.velocity * timeStep;
            virtualBody.position = newPos;

            drawPoints[step] = newPos;


        }

        // draw paths
        if (useThickLines)
        {
            line = this.GetComponent<LineRenderer>();
            line.enabled = true;
            line.positionCount = drawPoints.Length;
            line.SetPositions(drawPoints);
            line.startColor = pathColour;
            line.endColor = pathColour;
            line.widthMultiplier = width;

        }
        else
        {
            for (int i = 0; i < drawPoints.Length - 1; i++)
            {
                Debug.DrawLine(drawPoints[i], drawPoints[i + 1], pathColour);
            }

            // Hide renderer
            line = this.GetComponent<LineRenderer>();
            if (line)
            {
                line.enabled = false;
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
                velocity = ship.Initial.Velocity();
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
