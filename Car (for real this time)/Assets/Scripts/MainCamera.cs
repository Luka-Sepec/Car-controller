using UnityEngine;
using UnityEngine.InputSystem;

public class MainCamera : MonoBehaviour
{
    public Transform target;
    public InputHandler input;

    public float distance = 6f;
    public float height = 2f;
    public float sensitivity = 0.1f;
    public float smoothTime = 0.1f;

    private float yaw;
    private float pitch;
    Vector3 velocity;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
    }
    private void LateUpdate()
    {
        Vector2 look = input.lookValue;
        yaw += look.x * sensitivity;
        pitch -= look.y * sensitivity;
        pitch = Mathf.Clamp(pitch, -5f, 45f);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPosition = target.position - rotation * Vector3.forward * distance + Vector3.up * height;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        transform.LookAt(target.position + Vector3.up * 1f);

        //if (input.lookValue.magnitude < 0.01f)
        //{
        //    float targetYaw = target.eulerAngles.y;
        //    yaw = Mathf.LerpAngle(yaw, targetYaw, Time.deltaTime * 2f);
        //}
    }

    //[SerializeField] CarMain Car;

    //private float previousCarYaw;
    //private float cameraYaw;

    //public float rotateStrength = 20f;
    //public float returnSpeed = 4f;

    //private void Update()
    //{
    //    float currentCarYaw = Car.transform.eulerAngles.y;
    //    float deltaYaw = Mathf.DeltaAngle(previousCarYaw, currentCarYaw);
    //    previousCarYaw = currentCarYaw; cameraYaw += deltaYaw * Time.deltaTime * rotateStrength;
    //    cameraYaw = Mathf.Lerp(cameraYaw, 0f, returnSpeed * Time.deltaTime);
    //    transform.localRotation = Quaternion.Euler(8f, -cameraYaw, 0f); ;
    //}

}
