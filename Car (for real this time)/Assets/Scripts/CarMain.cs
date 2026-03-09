using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

public class CarMain : MonoBehaviour
{
    [SerializeField] public InputHandler inputHandler;
    [SerializeField] private Wheel[] wheels;
    [SerializeField] public Rigidbody rb;

    public float engineForce = 8000f;
    public float topSpeed = 360f; //kmh
    public float currentSpeed = 0f;
    public float sideGrip;
    public float driftFactor = 0.2f;
    public float antiFlipStrength;
    public float springStrength = 200000f;
    //spring strength has to be this high because the car's mass is 1000
    public float dampingStrength = 5000f;

    //made for reset but it doesnt work
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    [Header("Debug")]
    public float drivingForceMag;
    public float sideForceMag;
    public float frictionForceMag;
    private void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        rb.centerOfMass += new Vector3(0f, -0.5f, 0f);
    }
    private void Update()
    {
        currentSpeed = rb.linearVelocity.magnitude * 3.6f;
        foreach(Wheel wheel in wheels)
        {
            wheel.Rotate();
            wheel.Steer(currentSpeed, inputHandler.steerValue);
            wheel.springStrength = springStrength;
            wheel.dampingStrength = dampingStrength;
        }

        if (inputHandler.reset)
        {
            Reset();
            inputHandler.reset = false;
        }
    }

    private void FixedUpdate()
    {
        if (currentSpeed > topSpeed)
        {
            currentSpeed = topSpeed;
            return;
        }

        foreach (Wheel wheel in wheels)
        {
            wheel.UpdateSuspension();
            if (!wheel.grounded) continue;
            //Velocities
            Vector3 wheelVelocity = rb.GetPointVelocity(wheel.transform.position);
            Vector3 forwardVelocity = Vector3.Dot(wheelVelocity, wheel.transform.forward) * wheel.transform.forward;
            Vector3 sidewaysVelocity = Vector3.Dot(wheelVelocity, wheel.transform.right) * wheel.transform.right;

            //Debug for wheels
            wheel.wheelVelocity = wheelVelocity;
            wheel.forwardVelocity = forwardVelocity;
            wheel.sidewaysVelocity = sidewaysVelocity;

            Vector3 driveForce = inputHandler.acceleration * engineForce * wheel.transform.forward;
            Vector3 frictionForce = rb.mass * -forwardVelocity * 0.07f;

            float slip = sidewaysVelocity.magnitude;
            float driftGrip = Mathf.Lerp(1f, driftFactor, slip / 10f);
            float grip = inputHandler.braking && !wheel.isFront ? driftGrip : 1f;
            Vector3 sideGripForce = Vector3.ClampMagnitude(sideGrip * grip * -sidewaysVelocity, 8000f);

            drivingForceMag = driveForce.magnitude;
            sideForceMag = sideGripForce.magnitude;
            frictionForceMag = frictionForce.magnitude;

            Vector3 totalForce = driveForce + frictionForce + sideGripForce;

            rb.AddForceAtPosition(totalForce, wheel.transform.position);
            rb.AddForceAtPosition(wheel.suspensionForce, wheel.transform.position);

        }
        Wheel frontRight = wheels[0];
        Wheel frontLeft = wheels[1];
        Wheel backRight = wheels[2];
        Wheel backLeft = wheels[3];

        float rollForce = (frontLeft.compression - frontRight.compression) * antiFlipStrength;

        rb.AddForceAtPosition(rollForce * -frontLeft.transform.up, frontLeft.transform.position);
        rb.AddForceAtPosition(rollForce * frontRight.transform.up, frontRight.transform.position);

        rollForce = (backLeft.compression - backRight.compression) * antiFlipStrength;

        rb.AddForceAtPosition(rollForce * -backLeft.transform.up, backLeft.transform.position);
        rb.AddForceAtPosition(rollForce * backRight.transform.up, backRight.transform.position);

        if (inputHandler.braking)
        {
            float yawAssist = inputHandler.steerValue * 1500f;
            rb.AddTorque(transform.up * yawAssist);
        }
        if (!inputHandler.braking)
        {
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 2f);
        }

    }

    public void Reset()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = originalPosition;
        rb.rotation = originalRotation;
        rb.Sleep();
    }

}