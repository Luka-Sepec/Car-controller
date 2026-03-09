using UnityEngine;

public class Old
{

}
//using UnityEngine;
//using UnityEngine.Rendering;
//using UnityEngine.XR;

//public class CarMain : MonoBehaviour
//{
//    [SerializeField] private InputHandler inputHandler;
//    [SerializeField] private Wheel[] wheels;
//    [SerializeField] private Rigidbody rb;

//    public float engineForce = 8000f;
//    public float topSpeed = 360f; //kmh

//    [SerializeField] float sideGrip = 5000f;
//    [SerializeField] float rollingResistance = 150f;
//    [SerializeField] float antiRollStrength = 5000f;

//    public float downforceStrength = 50f;

//    public float currentSpeed = 0f;

//    private void Start()
//    {
//        //rb.centerOfMass = new Vector3(0f, -0.8f, 0f);
//    }
//    private void Update()
//    {
//        currentSpeed = rb.linearVelocity.magnitude * 3.6f;

//        for (int i = 0; i < 4; i++)
//        {
//            wheels[i].Rotate(currentSpeed, inputHandler.acceleration);
//            wheels[i].Steer(inputHandler.steerValue, currentSpeed);
//        }
//    }

//    private void FixedUpdate()
//    {
//        rb.AddForce(downforceStrength * rb.linearVelocity.magnitude * -transform.up);
//        engineForce = 8000f + Mathf.Floor(currentSpeed * 6f / topSpeed) * 1500f;
//        foreach (Wheel wheel in wheels)
//        {
//            wheel.UpdateSuspension(rb);
//        }

//        ApplyAntiRoll(wheels[0], wheels[1]);
//        ApplyAntiRoll(wheels[2], wheels[3]);

//        if (currentSpeed > topSpeed) return;

//        foreach (Wheel wheel in wheels)
//        {
//            bool grounded = Physics.Raycast(wheel.transform.position, -transform.up, 0.5f);
//            if (!grounded) continue;
//            Vector3 wheelVelocity = rb.GetPointVelocity(wheel.transform.position);
//            Vector3 forward = wheel.transform.forward;
//            Vector3 right = wheel.transform.right;

//            float forwardVelocity = Vector3.Dot(wheelVelocity, forward);
//            float sidewaysVelocity = Vector3.Dot(wheelVelocity, right);

//            float drivingForce = engineForce * inputHandler.acceleration;

//            float longitudinal = drivingForce - forwardVelocity * rollingResistance;
//            float lateral = (inputHandler.braking && !wheel.isFront) ?
//                            Mathf.Clamp(-sidewaysVelocity * sideGrip * (1 - currentSpeed / topSpeed), -8000f, 8000f) :
//                            Mathf.Clamp(-sidewaysVelocity * sideGrip, -8000f, 8000f);

//            if (inputHandler.braking)
//            {
//                float yawTorque = inputHandler.steerValue * 2500f;
//                rb.AddTorque(Vector3.up * yawTorque);
//                rb.AddForceAtPosition(
//                -transform.forward * 3000f,
//                rb.worldCenterOfMass
//                );
//            }

//            Vector3 force = forward * longitudinal + right * lateral;
//            rb.AddForceAtPosition(force, wheel.transform.position);

//            wheel.SetDebugForce(right * lateral);
//        }

//    }

//    public void ApplyAntiRoll(Wheel right, Wheel left)
//    {
//        if (!left.grounded && !right.grounded) return;

//        if (!left.grounded) rb.AddForceAtPosition(-left.transform.up * antiRollStrength, left.transform.position);
//        if (!right.grounded) rb.AddForceAtPosition(-right.transform.up * antiRollStrength, right.transform.position);

//        //float travelL = left.GetCompression01();
//        //float travelR = right.GetCompression01();

//        //float antiRollForce = (travelL - travelR) * antiRollStrength;

//        //if (left.grounded) rb.AddForceAtPosition(-left.transform.up * antiRollForce, left.contactPoint);
//        //if (right.grounded) rb.AddForceAtPosition(-right.transform.up * antiRollForce, right.contactPoint);
//    }

//}

//using UnityEngine;

//public class Wheel : MonoBehaviour
//{
//    [SerializeField] private GameObject WheelFrame;
//    [SerializeField] private GameObject Tire;
//    [SerializeField] private GameObject Caliper;
//    [SerializeField] private GameObject Rotor;
//    [SerializeField] public bool isFront;

//    private float steerAngle = 30f;
//    private float currentSteerAngle;
//    private float steerSpeed = 10f;
//    private float maxSteerSpeed = 300f;

//    //suspension
//    [SerializeField] float suspensionRestLength = 0.4f;
//    [SerializeField] float springStrength = 35000f;
//    [SerializeField] float damperStrength = 4500f;
//    [SerializeField] float wheelRadius = 0.35f;
//    public bool grounded;
//    public Vector3 contactPoint;
//    private float previousCompression;

//    private Vector3 debugForce;

//    public void Rotate(float speed, float direction)
//    {
//        WheelFrame.transform.Rotate(Vector3.right, speed * direction * 250f * Time.deltaTime, Space.Self);
//        Tire.transform.Rotate(Vector3.right, speed * direction * 250f * Time.deltaTime, Space.Self);
//        Rotor.transform.Rotate(Vector3.right, speed * direction * 250f * Time.deltaTime, Space.Self);
//    }

//    public void Steer(float steerInput, float speed)
//    {
//        if (!isFront) return;
//        float steerMultiplier = Mathf.Clamp(0.1f, Mathf.Pow(1 - speed / maxSteerSpeed, 2f), 1f);
//        float targetAngle = steerInput * steerAngle * steerMultiplier;
//        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetAngle, steerSpeed * Time.deltaTime);
//        transform.localRotation = Quaternion.Euler(0f, currentSteerAngle, 0f);
//    }

//    public void UpdateSuspension(Rigidbody rb)
//    {
//        Vector3 rayOrigin = transform.position;
//        float rayLength = suspensionRestLength + wheelRadius;

//        if (Physics.Raycast(rayOrigin, -transform.up, out RaycastHit hit, rayLength))
//        {
//            grounded = true;
//            contactPoint = hit.point;
//            float currentLength = hit.distance - wheelRadius;
//            float compression = suspensionRestLength - currentLength;

//            float compressionVelocity = (compression - previousCompression) / Time.fixedDeltaTime;
//            previousCompression = compression;

//            float springForce = compression * springStrength;
//            float damperForce = compressionVelocity * damperStrength;
//            float totalForce = springForce + damperForce;

//            rb.AddForceAtPosition(transform.up * totalForce, contactPoint, ForceMode.Force);
//        }
//        else
//        {
//            grounded = false;
//            previousCompression = 0f;
//        }
//    }

//    public float GetCompression01()
//    {
//        return Mathf.Clamp01(previousCompression / suspensionRestLength);
//    }
//    public void SetDebugForce(Vector3 force)
//    {
//        debugForce = force;
//    }

//    private void OnDrawGizmos()
//    {
//        Gizmos.color = Color.red;

//        Vector3 start = transform.position;
//        Vector3 end = start + debugForce * 0.0005f; // scale for visibility
//        Gizmos.DrawLine(start, end);
//        Gizmos.DrawSphere(end, 0.05f);

//        Gizmos.color = grounded ? Color.green : Color.gray;
//        Gizmos.DrawLine(
//            transform.position,
//            transform.position - transform.up * (suspensionRestLength + wheelRadius)
//        );
//    }
//}