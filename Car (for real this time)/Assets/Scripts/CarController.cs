using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[Serializable]
public class Engine
{
    public bool targetRPM = false;
    public float currentTarget = 0f;
    public bool forceAdjacentGears = false;
    public bool constantTorque = false;
    public float idleRPM = 800f;
    public float maxRPM = 8500f;
    public float peakPowerRPM = 7800f;
    public float[] gearRatios = { 3.31f, 2.27f, 1.69f, 1.32f, 1.02f, 0.82f, 0.67f, 0.56f, 0.46f };
    public float finalDriveRatio = 3.73f;
    private int currentGear = 0;
    private int previousGear = -1;
    private float lastGearChangeTime = 0f;
    private float gearCooldown = 1f;
    public bool automaticTransmission = true;
    private bool switchingGears = false;
    private float gearChangeTime = 0.09f;
    private float rpm = 0f;
    private bool engineInitialized = false;
    public void SetRPM(float averageWheelAngularVelocity)
    {
        if (!engineInitialized)
        {
            this.rpm = idleRPM;
            engineInitialized = true;
            return;
        }

        if (float.IsNaN(averageWheelAngularVelocity) || float.IsInfinity(averageWheelAngularVelocity)) averageWheelAngularVelocity = 0f;
        float averageWheelRPM = (averageWheelAngularVelocity * 60f) / (2f * Mathf.PI);
        float totalRatio = Mathf.Abs(gearRatios[currentGear] * finalDriveRatio);
        float transmissionRPM = averageWheelRPM * totalRatio;
        float targetRPM = Math.Max(idleRPM, transmissionRPM);
        this.rpm = Mathf.Clamp(targetRPM, idleRPM, maxRPM);
    }

    public float GetCurrentPower(MonoBehaviour context)
    {
        if (constantTorque) return 1;
        if (rpm >= maxRPM) return 0f;
        if (switchingGears) return 0.7f;

        float normalizedRPM = rpm / maxRPM;
        if (rpm < idleRPM) return 0f;
        if (rpm < peakPowerRPM)
        {
            float t = (rpm - idleRPM) / (peakPowerRPM - idleRPM);
            return Mathf.Lerp(0.3f, 1f, t * t);
        }
        else
        {
            float t = (rpm - peakPowerRPM) / (maxRPM - peakPowerRPM);
            return Mathf.Lerp(1f, 0.1f, t);
        }

    }

    public float AngularVelocityToRPM(float angularVelocity)
    {
        return angularVelocity * 60f / (2f * Mathf.PI);
    }

    public void UpGear(MonoBehaviour context)
    {
        if (currentGear < gearRatios.Length - 1 && !switchingGears)
        {
            previousGear = currentGear;
            currentGear++;
            lastGearChangeTime = Time.time;
            switchingGears = true;
            context.StartCoroutine(ResetSwitchingGearsCoroutine());
        }
    }

    public void DownGear(MonoBehaviour context)
    {
        if (currentGear > 0 && !switchingGears)
        {
            previousGear = currentGear;
            currentGear--;
            lastGearChangeTime = Time.time;
            switchingGears = true;
            context.StartCoroutine(ResetSwitchingGearsCoroutine());
        }
    }

    private System.Collections.IEnumerator ResetSwitchingGearsCoroutine()
    {
        yield return new WaitForSeconds(gearChangeTime);
        switchingGears = false;
    }

    public int getCurrentGear()
    {
        return currentGear + 1;
    }

    public void checkGearShifting (MonoBehaviour context, float throttle)
    {
        if (switchingGears) return;
        if (rpm > maxRPM * 0.91f && currentGear < gearRatios.Length -1)
        {
            UpGear(context);
            return;
        }
        if (!targetRPM)
        {
            currentTarget = 0.7f * maxRPM;
        }
        float tolerance = 0.2f * maxRPM;

        if (targetRPM)
        {
            float newTarget = Mathf.Clamp(maxRPM * throttle, idleRPM, maxRPM * 0.8f);
            if (newTarget >= currentTarget) currentTarget = newTarget;
            else currentTarget = Mathf.Lerp(currentTarget, newTarget, 0.002f);
        }

        if (forceAdjacentGears)
        {
            int optimalGear = FindOptimalGear(currentTarget);

            if (optimalGear > currentGear && currentGear < gearRatios.Length - 1)
            {
                int targetGear = currentGear + 1;
                if (targetGear != previousGear || Time.time - lastGearChangeTime > gearCooldown)
                {
                    UpGear(context);
                }
            }

            else if (optimalGear < currentGear && currentGear > 0)
            {
                int targetGear = currentGear - 1;
                if (targetGear != previousGear || Time.time - lastGearChangeTime > gearCooldown)
                {
                    DownGear(context);
                }
            }
        }
        else
        {
            int optimalGear = FindOptimalGear(currentTarget);
            if (optimalGear != previousGear || Time.time - lastGearChangeTime > gearCooldown)
            {
                previousGear = currentGear;
                currentGear = optimalGear;
                lastGearChangeTime = Time.time;
                switchingGears = true;
                context.StartCoroutine(ResetSwitchingGearsCoroutine());
            }
        }
    }

    private int FindOptimalGear(float targetRPM)
    {
        float currentWheelRPM = rpm / (Math.Abs(gearRatios[currentGear] * finalDriveRatio));
        int bestGear = currentGear;
        float bestDifference = float.MaxValue;

        for (int gear = 0; gear < gearRatios.Length; gear++)
        {
            float totalRatio = Math.Abs(gearRatios[gear] * finalDriveRatio);
            float projectedRPM = Mathf.Max(idleRPM, currentWheelRPM * totalRatio);

            projectedRPM = Mathf.Clamp(projectedRPM, idleRPM, maxRPM);

            float difference = Mathf.Abs(projectedRPM - targetRPM);

            if (difference < bestDifference)
            {
                bestDifference = difference;
                bestGear = gear;
            }

            else if (Mathf.Abs(difference - bestDifference) < 50f && gear > bestGear)
            {
                bestGear = gear;
            }
        }

        return bestGear;
    }

    public float getRPM()
    {
        return rpm;
    }

    public bool isSwitchingGears()
    {
        return switchingGears;
    }

    public float GetCurrentTotalGearRatio()
    {
        return gearRatios[currentGear] * finalDriveRatio;
    }
}

[Serializable]

public class WheelProperties
{
    //[HideInInspector] public TrailRenderer skidTrail;
    //[HideInInspector] public GameObject skidTrailGameObject;

    public Vector3 localPosition;
    public float turnAngle = 30f;
    public float suspensionLength = 0.5f;
    [HideInInspector] public float lastSuspensionLength = 0f;
    public float mass = 16f;
    public float size = 0.35f;
    public float engineTorque = 40f;
    public float brakeStrength = 0.5f;
    public bool sliding = false;
    [HideInInspector] public Vector3 worldSlipDirection;
    [HideInInspector] public Vector3 suspensionForceDirection;
    public Vector3 wheelWorldPosition;
    public float wheelCircumference;
    [HideInInspector] public float torque = 0.0f;
    public GameObject wheelObject;
    [HideInInspector] public Vector3 localVelocity;
    [HideInInspector] public float normalForce;
    [HideInInspector] public float angularVelocity;
    [HideInInspector] public float slip;
    [HideInInspector] public Vector2 input = Vector2.zero;
    [HideInInspector] public float braking = 0f;
    [HideInInspector] public float slipHistory = 0f;
    [HideInInspector] public float tcsReduction = 0f;
    [HideInInspector] public float steeringReduction = 0f;
    [HideInInspector] public float xSlipAngle = 0f;
}

public class CarController : MonoBehaviour
{
    public bool motorCycleControl = false;
    public float motorcycleTiltDamping = 2f;
    public float motorcycleYawDamping = 1f;
    public float restoreStrength = 1f; // Strength of restoring force when sliding
    public float restoreStrengthY = 1f; // Strength of restoring force when sliding
    public float steerAssistTarget = 0.75f; // Target slip ratio for steering assist
    public float coefFrictionMultiplier = 1.0f; // Multiplier for friction coefficient
    public Vector3 centerOfDownforce = new Vector3(0, 0, 0);

    [Header("Audio")]
    //public CarAudioController audioController;
    [Header("Aerodynamics")]
    public float dragCoefficient = 0.278f;
    public float frontalArea = 1.88f;
    public float airDensity = 1.225f;
    public float lowSpeedDragCoefficient = 0.37f;
    public float rollingResistanceCoeff = 0.015f;
    public GameObject adaptiveBrakingWing;
    public float brakingWingAngle = 60f;
    public float brakingWingSpeed = 8f;
    [HideInInspector] public float currentWingAngle = 0f;
    public InputHandler input;
    public Engine engine;
    //public GameObject skidMarkPrefab;
    public float smoothTurn = 0.03f;
    float coefStaticFriction = 0.95f;
    float coefKineticFriction = 0.35f;
    public GameObject wheelPrefab;
    public GameObject wheelPrefabMirrored;
    public WheelProperties[] wheels;
    public float wheelGripX = 8f;
    public float wheelGripZ = 42f;
    public float suspensionForce = 90f;
    public float dampAmount = 2.5f;
    public float suspensionForceClamp = 200f;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public bool forwards = true;

    //Assists
    public bool steeringAssist = true;
    public bool throttleAssist = true;
    public bool brakeAssist = true;
    [HideInInspector] public Vector2 userInput = Vector2.zero;
    public float downForce = 0.16f;
    [HideInInspector] public float isBraking = 0f;

    public Vector3 COMOffset = new Vector3(0f, -0.2f, 0f);
    public float Inertia = 1.2f;
    public Vector2 RawInput = Vector2.zero;

    public float carSpeedFactor = 0.03f;
    float handBrakeInput = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();

        foreach (var wheel in wheels)
        {
            if (wheel == wheels[2] || wheel == wheels[3])
            {
                wheel.wheelObject = Instantiate(wheelPrefabMirrored, transform);
            }
            else
            {
                wheel.wheelObject = Instantiate(wheelPrefab, transform);
            }
            wheel.wheelObject.transform.localPosition = wheel.localPosition;
            wheel.wheelObject.transform.eulerAngles = transform.eulerAngles;
            //if (wheel.localPosition.x == -0.98f) wheel.wheelObject.transform.GetChild(0).eulerAngles = new Vector3(0f, 180f, 0f);
            wheel.wheelObject.transform.localScale = new Vector3(100f, 100f, 100f);//2f * new Vector3(wheel.size, wheel.size, wheel.size);
            wheel.wheelCircumference = 2f * Mathf.PI * wheel.size;
        }

        foreach (var wheel in wheels)
        {
            wheel.tcsReduction = 0f;
            wheel.slipHistory = 0f;
            wheel.steeringReduction = 0f;
        }

        rb.centerOfMass += COMOffset;
        rb.inertiaTensor *= Inertia;
        engine.SetRPM(0f);
    }

    private void Awake()
    {
        input = GetComponent<InputHandler>();
    }

    private void Update()
    {
        if (input.reset)
        {
            float yrotation = transform.rotation.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0, yrotation, 0);
            transform.position += Vector3.up * 2f;
            rb.angularVelocity = transform.forward * 5f;
            rb.angularVelocity = Vector3.zero;
        }

        float steerInput = input.steerValue;
        float throttleInput = input.acceleration;
        float handbrakeInput = input.braking? 0f : 1f;

        userInput.x = Mathf.Lerp(userInput.x, steerInput / (1 + rb.linearVelocity.magnitude * carSpeedFactor), 50f * Time.deltaTime);
        userInput.y = Mathf.Lerp(userInput.y, throttleInput, 50f * Time.deltaTime);
        isBraking = userInput.y < 0f && forwards ? Mathf.Abs(userInput.y) : 0f;

        if (adaptiveBrakingWing != null)
        {
            float targetAngle = isBraking > 0.15f ? brakingWingAngle : 0f;
            currentWingAngle = Mathf.Lerp(currentWingAngle, targetAngle, brakingWingSpeed * Time.deltaTime);

            adaptiveBrakingWing.transform.localRotation = Quaternion.Euler(currentWingAngle, 0, 0);
        }

        foreach (var wheel in wheels)
        {
            if (float.IsNaN(wheel.slip) || float.IsInfinity(wheel.slip)) wheel.slip = 0f;

            if (throttleAssist)
            {
                float targetSlip = 0.91f;
                float slipTolerance = 0.02f;
                if (wheel.slip > targetSlip + slipTolerance)
                {
                    float overShoot = wheel.slip - targetSlip;
                    float reduction = Mathf.Clamp01(overShoot * 2.0f);
                    wheel.tcsReduction = Mathf.Lerp(wheel.tcsReduction, 1, reduction / 5f);
                }
                else if (wheel.slip < targetSlip - slipTolerance) 
                {
                    wheel.tcsReduction = Mathf.Lerp(wheel.tcsReduction, 0f, 0.6f * Time.deltaTime);
                }
                wheel.tcsReduction = Mathf.Clamp01(wheel.tcsReduction);
            }
            
            if (steeringAssist)
            {
                float targetSlip = steerAssistTarget;
                float slipTolerance = 0.02f;
                if (wheel.slip > targetSlip + slipTolerance)
                {
                    float overshoot = wheel.slip - targetSlip;
                    float reduction = Mathf.Clamp01(overshoot * 2f);
                    wheel.steeringReduction = Mathf.Lerp(wheel.steeringReduction, 1, reduction / 5f);
                }

                else if (wheel.slip < targetSlip - slipTolerance)
                {
                    wheel.steeringReduction = Mathf.Lerp(wheel.steeringReduction, 0f, 6f * Time.deltaTime);
                }
                wheel.steeringReduction = Mathf.Clamp01(wheel.steeringReduction);
            }

            wheel.braking = isBraking * (1 - wheel.tcsReduction);
            float maxTilt = 8f + rb.linearVelocity.magnitude * 2f;
            maxTilt = Mathf.Min(maxTilt, 65f);

            if (!motorCycleControl) wheel.input.x = Mathf.Lerp(wheel.input.x, userInput.x * (1f - wheel.steeringReduction), Time.deltaTime * 60f);
            else
            {
                float currentLeanAngle = transform.localRotation.eulerAngles.z;
                if (currentLeanAngle > 180f) currentLeanAngle -= 360f;

                Vector3 localAngularVelocity = transform.InverseTransformDirection(rb.angularVelocity);
                float tiltAngularVelocity = localAngularVelocity.z;
                float yawAngularVeloctiy = localAngularVelocity.z;

                float tiltDampingComponent = tiltAngularVelocity * motorcycleTiltDamping;
                tiltDampingComponent = Mathf.Clamp(tiltDampingComponent, -40f, 40f);

                if ((currentLeanAngle > 0f && tiltDampingComponent < 0f) || (currentLeanAngle < 0f && tiltDampingComponent > 0f))
                {
                    tiltDampingComponent *= 0.7f;
                }

                float blend = Mathf.Clamp01(Mathf.Abs(currentLeanAngle) / maxTilt);

                float yawDampingComponent = yawAngularVeloctiy * motorcycleYawDamping;
                yawDampingComponent = Mathf.Clamp(yawDampingComponent, -30f, 30f);

                float leanSteering = (currentLeanAngle * (1 + Mathf.Max(blend - (Mathf.Min(rb.linearVelocity.magnitude / 20f, 1)), 0) * 3f) +
                                      userInput.x * (1 - blend) * 30f * (1 + rb.linearVelocity.magnitude / 60f) *
                                      (1 + Mathf.Max(0, rb.linearVelocity.magnitude - 2f) / 3f) + tiltDampingComponent * (1.8f - blend * 0.5f) +
                                      yawDampingComponent) / 45f;
                leanSteering = Mathf.Clamp(leanSteering, -1f, 1f);

                wheel.input.x = Mathf.Lerp(wheel.input.x, -leanSteering / (1 + Mathf.Max(0, rb.linearVelocity.magnitude - 2f) / 3f), Time.deltaTime * 60f);

            }

            if (wheel.slip > 1.0f && steeringAssist) wheel.input.x = Mathf.Lerp(wheel.input.x, wheel.xSlipAngle / wheel.turnAngle, Time.deltaTime);

            float finalThrottle = userInput.y * (1f - wheel.tcsReduction);
            if (float.IsNaN(finalThrottle) || float.IsInfinity(finalThrottle)) finalThrottle = 0f;
            if (float.IsNaN(wheel.steeringReduction) || float.IsInfinity(wheel.steeringReduction)) wheel.steeringReduction = 0f;

            if (throttleAssist)
            {
                wheel.input.y = Mathf.Lerp(wheel.input.y, finalThrottle, 0.95f * Time.deltaTime * 60f);
            }
            else wheel.input.y = userInput.y;

            wheel.input.x = Mathf.Clamp(wheel.input.x, -1f, 1f);
            wheel.input.y = Mathf.Clamp(wheel.input.y, -1f, 1f);

            if (Time.time % 1f < 0.1f) // Log once per second for first wheel only
            {
                Debug.Log($"Input Debug - userInput: {userInput}, wheel input: {wheel.input}, TCS: {wheel.tcsReduction}, steerReduction: {wheel.steeringReduction}");
            }

            //if (Input.GetKeyDown(KeyCode.E)) engine.UpGear(this);
            //else if (Input.GetKeyDown(KeyCode.D)) engine.DownGear(this);
            //if (audioController != null)
            //{
            //    // Calculate average slip from all wheels
            //    float averageSlip = 0f;
            //    for (int i = 0; i < wheels.Length; i++)
            //    {
            //        averageSlip += wheels[i].slip;
            //    }
            //    averageSlip /= wheels.Length;

            //    // Update audio controller with current car state
            //    audioController.UpdateAudioValues(
            //        e.getRPM(),                          // rpm
            //        Mathf.Max(0f, userInput.y),          // throttle (only positive values)
            //        rb.velocity.magnitude,               // speed
            //        averageSlip,                         // slip
            //        e.isSwitchingGears()                 // shifting
            //    );
            //}


        }
    }

    private void ApplyAerodynamicDrag()
    {
        Vector3 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;
        float speedKmh = speed * 3.6f;

        float currentDragCoeff = dragCoefficient * (isBraking > 0.3f ? (adaptiveBrakingWing != null ? 2f : 1f) : 1f);
        float dragMagnitude = 0.5f * airDensity * speed * speed * currentDragCoeff * frontalArea;

        Vector3 dragForce = -velocity.normalized * dragMagnitude;

        rb.AddForce(dragForce / 200f, ForceMode.Force);
    }

    private void FixedUpdate()
    {
        
        ApplyAerodynamicDrag();

        rb.AddForceAtPosition(rb.linearVelocity.magnitude * downForce / 28f * -transform.up, transform.position + transform.TransformDirection(centerOfDownforce), ForceMode.Acceleration);
        rb.AddForceAtPosition(0.9f * transform.InverseTransformDirection(rb.linearVelocity).y * -transform.up, transform.position + transform.TransformDirection(new Vector3(0, 0, -1.5f * restoreStrengthY)), ForceMode.Acceleration);

        float averageWheelAngularVelocity = 0f;
        foreach (var wheel in wheels)
        {
            wheel.wheelWorldPosition = transform.TransformPoint(wheel.localPosition);
            RaycastHit hit;
            float RayLen = wheel.size * 2f + wheel.suspensionLength;
            Transform wheelObj = wheel.wheelObject.transform;
            Transform wheelVisual = wheelObj.GetChild(0);
            Transform wheelCaliper = wheelObj.GetChild(1);

            float steerAngle = wheel.turnAngle * wheel.input.x;
            if (float.IsNaN(steerAngle) || float.IsInfinity(steerAngle))
            {
                steerAngle = 0f;
            }

            wheelObj.localRotation = Quaternion.Slerp(wheelObj.localRotation, Quaternion.Euler(0, steerAngle, 0), 0.125f);
            Vector3 velocityAtWheel = rb.GetPointVelocity(wheel.wheelWorldPosition);
            wheel.localVelocity = wheelObj.InverseTransformDirection(velocityAtWheel);
            forwards = wheel.localVelocity.z > 0.1f;

            float enginePower = engine.GetCurrentPower(this);
            float gearRatio = engine.GetCurrentTotalGearRatio();
            wheel.torque = wheel.engineTorque * wheel.input.y * enginePower * gearRatio;

            float inertia = wheel.mass * wheel.size * wheel.size / 2f;
            float lateralVelocity = wheel.localVelocity.x;

            bool grounded = Physics.Raycast(wheel.wheelWorldPosition, -transform.up, out hit, RayLen);
            Vector3 worldVelocityAtHit = rb.GetPointVelocity(hit.point);
            float lateralHitVelocity = wheelObj.InverseTransformDirection(worldVelocityAtHit).x;

            float lateralFriction = -wheelGripX * lateralVelocity - 2f * lateralHitVelocity;
            float longitudinalFriction = -wheelGripZ * (wheel.localVelocity.z - wheel.angularVelocity * wheel.size);
            //Debug.Log($"WheelGripZ: {wheelGripZ}");
            //Debug.Log($"Wheel local velocity z: {wheel.localVelocity.z}");
            //Debug.Log($"Wheel angular velocity: {wheel.angularVelocity}");

            float rollingResistanceTorque = 0f;
            if (motorCycleControl && wheel.normalForce < (9.81f * wheel.mass * 0.3f)) userInput.y = 0f;
            if (grounded)
            {
                float rollingResistanceForce = this.rollingResistanceCoeff * wheel.normalForce;
                rollingResistanceForce = rollingResistanceForce * wheel.size;

                rollingResistanceTorque *= -Mathf.Sign(wheel.angularVelocity);
            }

            wheel.angularVelocity += (wheel.torque - longitudinalFriction * wheel.size - rollingResistanceTorque) / inertia * Time.fixedDeltaTime;
            wheel.angularVelocity *= 1 - wheel.braking * wheel.brakeStrength * Time.fixedDeltaTime;
            if (handBrakeInput > 0.5f)
            {
                wheel.angularVelocity = 0f;
            }

            Vector3 totalLocalForce = new Vector3(lateralFriction, 0f, longitudinalFriction)
                * wheel.normalForce * coefStaticFriction * coefFrictionMultiplier * Time.fixedDeltaTime;
            float currentMaxFrictionForce = wheel.normalForce * coefStaticFriction * coefFrictionMultiplier;
            wheel.sliding = totalLocalForce.magnitude > currentMaxFrictionForce;
            wheel.slip = totalLocalForce.magnitude / currentMaxFrictionForce;
            totalLocalForce = Vector3.ClampMagnitude(totalLocalForce, currentMaxFrictionForce);
            totalLocalForce *= wheel.sliding ? (coefKineticFriction / coefStaticFriction) : 1;

            Vector3 totalWorldForce = wheelObj.TransformDirection(totalLocalForce);
            wheel.worldSlipDirection = totalWorldForce;

            if (wheel.localVelocity.magnitude > 0.5f)
            {
                float velocityAngle = Mathf.Atan2(wheel.localVelocity.x, wheel.localVelocity.z) * Mathf.Rad2Deg;
                float currentWheelAngle = wheel.turnAngle * wheel.input.x;

                float rawSlipAngle = velocityAngle - currentWheelAngle;

                while (rawSlipAngle > 180f) rawSlipAngle -= 360f;
                while (rawSlipAngle < -180f) rawSlipAngle += 360f;

                wheel.xSlipAngle = Mathf.Lerp(wheel.xSlipAngle, rawSlipAngle, Time.fixedDeltaTime * 10f);
            }
            else
            {
                wheel.xSlipAngle = Mathf.Lerp(wheel.xSlipAngle, 0f, Time.fixedDeltaTime * 5f);
            }

            if (grounded)
            {
                float compression = RayLen - hit.distance;
                float damping = (wheel.lastSuspensionLength - hit.distance) * dampAmount;

                wheel.normalForce = (compression + damping) * suspensionForce;
                wheel.normalForce = Mathf.Clamp(wheel.normalForce, 0f, suspensionForceClamp);

                Vector3 springDir = hit.normal * wheel.normalForce;
                wheel.suspensionForceDirection = springDir;              
                rb.AddForceAtPosition(springDir + totalWorldForce, hit.point);
                wheel.lastSuspensionLength = hit.distance;

                wheelObj.position = hit.point + transform.up * wheel.size;

                averageWheelAngularVelocity += wheel.angularVelocity;

                wheelVisual.Rotate(Vector3.right, wheel.angularVelocity * Mathf.Rad2Deg * Time.fixedDeltaTime, Space.Self);
                averageWheelAngularVelocity /= wheels.Length;
                engine.SetRPM(averageWheelAngularVelocity);

            }

        }

    }

}

     
