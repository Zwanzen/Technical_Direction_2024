using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ShipController : MonoBehaviour
{
    public DrawShipPath pathDrawer;

    public bool inMenue = false;
    private bool dead = false;

    public bool GyroscopicStabilization = true;

    private GameHandler game;
    private Camera cam;

    public Transform moon;

    public Slider thrustSlider;
    public ShipDirectionSetter Initial;
    public Rigidbody rb;

    public Vector3 cameraOffset;
    public float thrustStrength = 10f;
    public float shipTurnSpeed = 1f;
    public float cameraTurnSpeed = 5f;

    [Space(20)]
    [Header("Fuel")]
    [SerializeField] private Image fuelSlider;
    [SerializeField] private float fuel = 1000f;
    private float maxFuel;

    // LANDING LEGS CONTROLLER
    [Space(20)]
    [Header("Landing Legs")]
    [SerializeField] private Transform[] IKTargets = new Transform[3];
    [SerializeField] private FastIKFabric[] IK = new FastIKFabric[3];
    [Space(5)]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float legCastDistance = 3f;
    [SerializeField] private float legCastHeight = 1f;
    [SerializeField] private float hoverDistance = 1.2f;
    [Space(5)]
    [SerializeField] private float landingCheckDistance = 1f;
    [SerializeField] private float landingSpringForce = 5f;
    [SerializeField] private float springDampener = 3f;
    [Space(5)]
    [SerializeField] private float stepDist = 0.25f;
    [SerializeField] private float legMoveSpeed = 0.5f;

    private Transform[] IKTargetCopies = new Transform[3];
    private Transform[] IKStartPositions = new Transform[3];
    private Vector3[] IKOldPos = new Vector3[3];
    private float[] legTargetTimer1 = new float[3];
    private float[] legTargetTimer2 = new float[3];
    private bool skipStep = true;

    [HideInInspector]
    public bool isGrounded = false;

    [Space(20)]
    [Header("Particles")]
    [SerializeField] private ParticleSystem thruster;
    [SerializeField] private ParticleSystem explosion;


    // INPUT
    private bool isThrusting = false;
    private bool isTurning = false;



    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "GameScene")
        {
            inMenue = false;
        }
        else
        {
            inMenue = true;
        }

        InitializeShip();

        game = FindObjectOfType<GameHandler>();
    }

    private void FixedUpdate()
    {
        // Gravity
        Vector3 gravity = NBodySimulation.CalculateAcceleration(rb.position);
        rb.AddForce(gravity, ForceMode.Acceleration);

        if (!inMenue && !dead)
        {
            HandleRotation();
            HandleLanding();
            HandleThrust();
        }
    }

    private void Update()
    {

        if (!inMenue)
        {
            HandleCamera();
            InputHandler();
        }

        HandleIKLegs();

        // does not change based on fps
        fuelSlider.fillAmount = fuel / maxFuel;

        if(fuel <= 0)
        {
            var emission = thruster.emission;
            emission.rateOverTime = 0;
        }
        else
        {
            var emission = thruster.emission;
            emission.rateOverTime = Mathf.Lerp(0, 100, thrustSlider.value);
        }

        if(fuel <= 0 && !dead)
        {
            // if in orbit: die
            if(pathDrawer.inOrbit)
            {
                DIE();
            }

            // if lost in space: die
            if (Vector3.Distance(transform.position, moon.position) > 200f)
            {
                DIE();
            }

            // if no movement, and time to impact is less than 10 seconds: die 
            if (pathDrawer.timeToImpact < 10 && rb.velocity.magnitude < 3f)
            {
                DIE();
            }

        }

    }

    // All input should be handled in the update function
    // This is to get the most responsive input you can from your system
    // It will activate every frame, while FixedUpdate will only activate every physics update
    private void InputHandler()
    {
        if (Input.GetMouseButton(0) && thrustSlider.value > 0f)
        {
            isThrusting = true;
        }
        else
        {
            isThrusting = false;
        }

        if(Input.GetMouseButton(1))
        {
            isTurning = true;
        }
        else
        {
            isTurning = false;
        }
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

    private void HandleThrust()
    {
        if (isThrusting && fuel > 0)
        {
            // the fuel is equal to the force applied
            // so if the force is greater than the fuel, set the force to the fuel
            var force = thrustStrength * thrustSlider.value;
            if (force > fuel)
            {
                force = fuel;
            }

            // add the force, then subtract from the fuel for next tick
            rb.AddForce(transform.up * force, ForceMode.Acceleration);
            fuel -= force;
        }
        else
        {
            thrustSlider.value -= 1f * Time.fixedDeltaTime;
        }
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

        // slowly align the ships up rotation to the direction to the moon
        if (isGrounded)
        {
            // get the surface normal of the ground
            RaycastHit hit;
            Ray ray = new Ray(transform.position, -transform.up);
            if (Physics.Raycast(ray, out hit, 10f, groundLayer))
            {
                // Calculate the target rotation that alignes the ship's up direction with the normal of the ground
                Quaternion alignToMoonSurface = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;

                // rotate the ship towards the target rotation
                transform.rotation = Quaternion.RotateTowards(transform.rotation, alignToMoonSurface, Time.fixedDeltaTime * 40f);
            }
        }
    }

    /* may not be needed
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
    */

    // what even is this
    // could add forward offset based on speed to be more behind or above when needed
    private Vector3 CalculateCameraPos()
    {
        return transform.position + (-DirectionToMoon().normalized * cameraOffset.z) + (Vector3.Cross(DirectionToMoon().normalized, VelocityProjected().normalized) * cameraOffset.x) + (VelocityProjected().normalized * cameraOffset.y);
    }

    private void HandleCamera()
    {
        // Set the camera's position, I was really tired when I wrote this
        cam.transform.position = CalculateCameraPos();

        if (isTurning)
        {
            // rotate the camera based on mouse movement, the camera z direction should be locked towards velocity projected
            cam.transform.RotateAround(cam.transform.position, -(moon.position - cam.transform.position).normalized, Input.GetAxis("Mouse X") * cameraTurnSpeed);
            cam.transform.RotateAround(cam.transform.position, cam.transform.right, -Input.GetAxis("Mouse Y") * cameraTurnSpeed);


            return;
        }

        // up dir should be the velocity projected onto the moon's surface
        // also the forward direction should be the direction to the moon
        // but be adjusted based on the current velocity projected to dynamically adjust the camera to look where ur going
        Vector3 forward = Vector3.Lerp(DirectionToMoon().normalized, (VelocityProjected().normalized * 2) + DirectionToMoon().normalized, VelocityProjected().magnitude/30);
        var targetCameraRotation = Quaternion.LookRotation(forward, VelocityProjected());


        // Smoothly interpolate the camera's current rotation towards the target rotation
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, targetCameraRotation, cameraTurnSpeed * Time.deltaTime);
    }

    private void HandleIKLegs()
    {
        // first cast a ray from each leg downwards
        for (int i = 0; i < 3; i++)
        {
            RaycastHit hit;
            Ray ray = new Ray(IKStartPositions[i].position + (IKStartPositions[i].up * legCastHeight), -IKStartPositions[i].up);

            Debug.DrawRay(ray.origin, ray.direction * (legCastDistance + legCastHeight), Color.magenta);

            // If the ray hits the ground, set the IK target to the hit point, else set it to the start position
            if (Physics.Raycast(ray, out hit, legCastDistance + legCastHeight, groundLayer) && isGrounded)
            {
                // reset other timer
                for (int j = 0; j < 3; j++)
                {
                    legTargetTimer2[j] = 0;
                }

                // if landing gear needs new footing based on distance, set a new one
                if (Vector3.Distance(IKTargetCopies[i].position, hit.point) > stepDist)
                {
                    Vector3 dir = (hit.point - IKTargetCopies[i].position).normalized;

                    IKTargetCopies[i].position = hit.point + dir * 0.5f;
                   
                    legTargetTimer1[i] = 0;
                    IKOldPos[i] = IKTargets[i].position;
                }

                if (legTargetTimer1[i] < 1 && !skipStep)
                {
                    // Slowly move the copies of the IK targets to the hit point
                    Vector3 targetPos = Vector3.Lerp(IKOldPos[i], IKTargetCopies[i].position, legTargetTimer1[i]);
                    targetPos += (transform.up * 1f) * Mathf.Sin(legTargetTimer1[i] * Mathf.PI);
                    IKTargets[i].position = targetPos;
                    legTargetTimer1[i] += legMoveSpeed * Time.deltaTime;
                }
                else if(legTargetTimer1[i] < 1)
                {
                    IKTargets[i].position = Vector3.Lerp(IKOldPos[i], IKTargetCopies[i].position, legTargetTimer1[i]);
                    legTargetTimer1[i] += legMoveSpeed * Time.deltaTime;
                }
                else
                {
                    skipStep = false;
                }
            }
            else
            {
                // reset other timer
                for (int j = 0; j < 3; j++)
                {
                    legTargetTimer1[j] = 0;
                }

                skipStep = true;

                if(legTargetTimer2[i] < 1)
                {
                    // Slowly move the IK target back to the start position
                    IKTargets[i].position = Vector3.Lerp(IKTargets[i].position, IKStartPositions[i].position, legTargetTimer2[i]);
                    legTargetTimer2[i] += legMoveSpeed * Time.deltaTime;
                }
                else
                {
                    IKTargets[i].position = IKStartPositions[i].position;
                }


                // just make sure the legs always get new footing
                IKTargetCopies[i].position = Vector3.zero;
            }
        }
    }

    // controls the damping of the ship when landing
    private void HandleLanding()
    {
        // cast a ray to see if the ship is close enough to the ground to land
        RaycastHit hit;
        Vector3 rayStartOffset = -transform.up; // sphere has a radius of 2, start the ray at that distance
        Ray ray = new Ray(rayStartOffset + transform.position, -transform.up);

        if (Physics.Raycast(ray, out hit, landingCheckDistance, groundLayer) && thrustSlider.value < 0.2f)
        {
            isGrounded = true;

            // from last ga3 course
            Vector3 vel = rb.velocity;

            Vector3 otherVel = Vector3.zero;
            Rigidbody hitBody = hit.rigidbody;
            if (hitBody != null)
            {
                otherVel = hitBody.velocity;
            }

            float relDirVel = Vector3.Dot(DirectionToMoon(), vel);
            float otherDirVel = Vector3.Dot(DirectionToMoon(), otherVel);

            float relVel = relDirVel - otherDirVel;

            float x = hit.distance - hoverDistance;

            float springForce = (x * landingSpringForce) - (relVel * springDampener);
            rb.AddForce(DirectionToMoon() * springForce);
            // ----

            // add a force to stop the ship from sliding
            rb.AddForce(-VelocityProjected(), ForceMode.Acceleration);

            // debug
            // draw the hit normal
            Debug.DrawRay(hit.point, hit.normal, Color.green);
        }
        else
        {
            isGrounded = false;
        }

        Debug.DrawRay(ray.origin, ray.direction * landingCheckDistance, Color.magenta);
    }

    private void InitializeCamera()
    {
        cam = Camera.main;

        // Set the camera's position, I was really tired when I wrote this
        cam.transform.position = CalculateCameraPos();

        // Instantly set the camera's rotation
        cam.transform.rotation = Quaternion.LookRotation(DirectionToMoon(), VelocityProjected()); ;
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
            IKTargets[i].position = IK[i].transform.position;

            IKTargetCopies[i] = new GameObject().transform;
            IKTargetCopies[i].position = IKTargets[i].position;
            IKTargetCopies[i].rotation = transform.rotation;

            IKStartPositions[i] = new GameObject().transform;
            IKStartPositions[i].parent = transform;
            IKStartPositions[i].position = IK[i].transform.position;
            IKStartPositions[i].rotation = transform.rotation;
        }
    }

    private void InitializeShip()
    {
        InitRigidbody();

        if (!inMenue)
        {
            RandomSpawn();
            InitializeCamera();
        }

        // Get and Set the initial velocity of the ship
        SetVelocity(Initial.Velocity());

        // Has to be below InitRigidbody()
        InitializeLegs();


        maxFuel = fuel;

    }

    public float GetFuel()
    {
        return fuel;
    }

    private void RandomSpawn()
    {
        // randomly place the ship above the surface of the moon
        Vector3 randomPos = Random.onUnitSphere * Random.Range(166f, 200f);

        transform.position = randomPos;
        rb.position = randomPos;
        Initial.transform.position = randomPos;

        // rotate initial to face the moon
        Initial.transform.rotation = Quaternion.LookRotation(-randomPos.normalized, Vector3.up);

        // rotate initial randomly along the forward axis to give the ship a random initial direction
        Initial.transform.Rotate(Initial.transform.forward, Random.Range(0, 360), Space.World);

        // rotate the ship to match the initial
        transform.rotation = Initial.transform.rotation;
        rb.rotation = Initial.transform.rotation;
    }

    private void DIE()
    {
        // U dies
        game.GameOver();

        dead = true;

        // spawn explosion
        ParticleSystem p = Instantiate(explosion, transform.position, Quaternion.identity);
        p.Play();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // if the ship collides with the ground, and the impact is hard enough, spawn an explosion
        if (collision.relativeVelocity.magnitude > 5f)
        {
            DIE();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(CalculateCameraPos(), 0.5f);
    }
}