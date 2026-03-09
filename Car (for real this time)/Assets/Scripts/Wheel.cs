using UnityEngine;

public class Wheel : MonoBehaviour
{
    [SerializeField] private GameObject WheelFrame;
    [SerializeField] private GameObject Tire;
    [SerializeField] private GameObject Caliper;
    [SerializeField] private GameObject Rotor;
    [SerializeField] private GameObject Visuals;
    [SerializeField] public bool isFront;
    [SerializeField] public bool isLeft;
    public CarMain car;

    private float currentSteerAngle;
    public Vector3 wheelVelocity;
    public Vector3 forwardVelocity;
    public Vector3 sidewaysVelocity;

    public Vector3 suspensionForce;
    public float springStrength = 40000f;
    public float dampingStrength = 5000f;
    public float defaultSpringLength = 0.45f;
    public float compression;

    public bool grounded;

    public void Rotate()
    {
        float forwardSpeed = Vector3.Dot(car.rb.linearVelocity, transform.forward);
        float rotation = (forwardSpeed / (2f * Mathf.PI * 0.45f)) * 360f * Time.deltaTime;
        Visuals.transform.Rotate(Vector3.right, rotation, Space.Self);
    }

    public void Steer(float speed, float steerInput)
    {
        if (!isFront) return;
        float speedFactor = Mathf.InverseLerp(0f, 200f, speed);
        float normalMax = Mathf.Lerp(30f, 4f, speedFactor);
        float driftMax = Mathf.Lerp(35f, 15f, speedFactor);

        float maxAngle = car.inputHandler.braking ? driftMax : normalMax;
        Debug.Log(maxAngle);
        float targetAngle = steerInput * maxAngle;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetAngle, 0.125f);
        transform.localRotation = Quaternion.Euler(0f, currentSteerAngle, 0f);
    }

    public void UpdateSuspension()
    {
        //suspension
        Vector3 springOrigin = transform.position + transform.up * 0.1f;
        if (Physics.Raycast(springOrigin, -transform.up, out RaycastHit hit, defaultSpringLength, LayerMask.NameToLayer("Ground")))
        {
            compression = defaultSpringLength - hit.distance;
            Vector3 pointVelocity = car.rb.GetPointVelocity(transform.position);
            float compressionVelocity = Vector3.Dot(transform.up, pointVelocity);
            float force = (compression * springStrength) - (compressionVelocity * dampingStrength);
            force = Mathf.Clamp(force, 0f, 30000f);
            suspensionForce = force * transform.up;
            grounded = true;
        }
        else
        {
            grounded = false;
            suspensionForce = Vector3.zero;
        }
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Vector3 start = transform.position;
        Vector3 end = start + wheelVelocity; //add other force here * 0.0005f; // scale for visibility
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.05f);

        Gizmos.color = Color.red;

        end = start + sidewaysVelocity; 
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.05f);

        Gizmos.color = Color.blue;

        end = start + forwardVelocity;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.05f);

        Gizmos.color = Color.hotPink;

        end = start + transform.up * 0.1f -transform.up * compression;
        Gizmos.DrawLine(start + transform.up * 0.1f, end);
        
    }
   
}