using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    [SerializeField] PlayerInput Input;

    public float acceleration;
    public float steerValue;
    public bool braking;
    public bool reset;

    public Vector2 lookValue;

    private void OnEnable()
    {
        Input.actions["Accelerate"].performed += context => acceleration = context.ReadValue<float>();
        Input.actions["Accelerate"].canceled += context => acceleration = 0f;

        Input.actions["Steer"].performed += context => steerValue = context.ReadValue<float>();
        Input.actions["Steer"].canceled += context => steerValue = 0f;

        Input.actions["Brake"].performed += context => braking = true;
        Input.actions["Brake"].canceled += context => braking = false;

        Input.actions["Reset"].performed += context => reset = true;
        Input.actions["Reset"].canceled += context => reset = false;

        Input.actions["Look"].performed += context => lookValue = context.ReadValue<Vector2>();
        Input.actions["Look"].canceled += context => lookValue = Vector2.zero;
    }

    private void OnDisable()
    {
        Input.actions["Accelerate"].performed -= context => acceleration = context.ReadValue<float>();
        Input.actions["Accelerate"].canceled -= context => acceleration = 0f;

        Input.actions["Steer"].performed -= context => steerValue = context.ReadValue<float>();
        Input.actions["Steer"].canceled -= context => steerValue = 0f;

        Input.actions["Brake"].performed -= context => braking = true;
        Input.actions["Brake"].canceled -= context => braking = false;

        Input.actions["Reset"].performed -= context => reset = true;
        Input.actions["Reset"].canceled -= context => reset = false;

        Input.actions["Look"].performed -= context => lookValue = context.ReadValue<Vector2>();
        Input.actions["Look"].canceled -= context => lookValue = Vector2.zero;
    }
    
}
