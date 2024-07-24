using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class SwerveModule : MonoBehaviour {

    [SerializeField] private Chassis chassis;

    public struct ModuleState {
        public float speed;
        public float rotation;

        public ModuleState(float speed, float rotation) {
            this.speed = speed;
            this.rotation = rotation;
        }
    }

    [SerializeField] private bool testing;

    const float PULLEY_SPEED_FACTOR = 100f;
    // const float VISUAL_WHEEL_SPEED_FACTOR = 3600f;
    const float WHEEL_YOFFSET = 53.676f;
    const float SPEED_VISUAL_FACTOR = 2f;

    private const float MOTOR_TORQUE = 5f;

    [SerializeField] private float wheelAngle; // FOR TESTING
    [SerializeField] private float wheelSpeed; // FOR TESTING

    [SerializeField] private Transform basePulley;
    [SerializeField] private Transform wheel;
    [SerializeField] private WheelCollider wheelCollider; 

    private ModuleState currentState;

    private void Start() {
        currentState = new ModuleState(0, 0);
        // Debug.Log(GetDistanceToCenter());
    }

    private void Update() {
        // if (testing) {
        //     float horizontal = Input.GetAxis("Horizontal");
        //     float vertical = Input.GetAxis("Vertical");

        //     basePulley.rotation = Quaternion.Euler(0, basePulley.eulerAngles.y + PULLEY_SPEED_FACTOR * Time.deltaTime * horizontal, 0);
        //     wheel.Rotate(WHEEL_SPEED_FACTOR * Time.deltaTime * vertical, 0, 0);
        // }

        // SetState(new ModuleState(wheelCollider.rotationSpeed * SPEED_VISUAL_FACTOR, wheelCollider.steerAngle));
        UpdateWheelVisual();
    }

    // Returns module's distance from the center
    public Vector2 GetDistanceToCenter() {
        return new Vector2(wheelCollider.transform.position.x, wheelCollider.transform.position.z);
    }

    public void SetState(ModuleState state) {
        float lerpedRotation;

        if (float.IsNaN(state.rotation)) {
            lerpedRotation = currentState.rotation;
        } else {
            lerpedRotation = Mathf.LerpAngle(currentState.rotation, state.rotation, Time.deltaTime * 15f);
        }

        float speedFactor = Mathf.InverseLerp(0, Chassis.WHEEL_MAX_TRANSLATION_SPEED, chassis.GetVelocity());
        float currentMotorTorque = Mathf.Lerp(MOTOR_TORQUE, 0, speedFactor);

        // wheelCollider.motorTorque = currentMotorTorque * state.speed;
        Vector3 accelDirection = wheelCollider.transform.forward;
        chassis.AddForceAtPosition(currentMotorTorque * accelDirection, wheelCollider.transform.position);

        // Debug.Log(wheelCollider.motorTorque);


        wheelCollider.steerAngle = lerpedRotation % 360;
        // wheelCollider.rotationSpeed = state.speed;
        // wheelCollider.rotationSpeed = state.speed * 999f;

        // wheelCollider.motorTorque = state.speed * 9999999f;

        currentState = new ModuleState(0, lerpedRotation);

        // TODO solution will be to add forces in the direction of the wheel; the forces will be proportional to the speed of the wheel collider
    }

    private void UpdateWheelVisual() {
        RotateVisualToAngle(GetWheelColliderWorldRotation());
        SetVisualSpeed(wheelCollider.rotationSpeed * SPEED_VISUAL_FACTOR);
    }

    private float GetWheelColliderWorldRotation() {
        Vector3 position;
        Quaternion rotation;

        wheelCollider.GetWorldPose(out position, out rotation);

        return rotation.eulerAngles.y - rotation.eulerAngles.z;
    }

    private void RotateVisualToAngle(float angle) {
        basePulley.rotation = Quaternion.Euler(0, angle + WHEEL_YOFFSET, 0);
    }

    private void SetVisualSpeed(float speed) {
        wheel.Rotate(Time.deltaTime * speed, 0, 0);
    }

    private float GetWheelColliderDiameter() {
        return wheelCollider.radius * 2f;
    }
}