using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.UIElements;

public class Chassis : MonoBehaviour {

    private float totalTime;

    private const float TRACK_WIDTH = 1.86f;
    private const float WHEEL_BASE = 1.66f;

    public const float WHEEL_MAX_TRANSLATION_SPEED = 10f; // In units per second
    private const float WHEEL_MAX_ANGULAR_SPEED = 5f; // In units per second

    
    private const float MAX_LINEAR_VELOCITY = 8f;
    private const float MAX_ANGULAR_VELOCITY = 5f;
    private const float MAX_LINEAR_ACCEL = 12f;
    private const float BRAKE_LINEAR_ACCEL = 25f;
    private const float MAX_ANGULAR_ACCEL = 10f;
    private const float BRAKE_ANGULAR_ACCEL = 20f;

    [SerializeField] private SwerveModule frontLeftModule;
    [SerializeField] private SwerveModule frontRightModule;
    [SerializeField] private SwerveModule backLeftModule;
    [SerializeField] private SwerveModule backRightModule;

    private SwerveModule[] modules = new SwerveModule[4];

    private float[,] inverseKinematics;

    private Rigidbody rigidBody;

    private struct ChassisSpeeds {
        public Vector2 translation;
        public float rotation;

        public ChassisSpeeds(Vector2 translation, float rotation) {
            this.translation = translation;
            this.rotation = rotation;
        }

        public void ConvertToFieldRelative(float robotHeading) {
            robotHeading *= Mathf.Deg2Rad;
            translation = new Vector2(
                translation.x * (float)Math.Cos(robotHeading) - translation.y * (float)Math.Sin(robotHeading),
                translation.x * (float)Math.Sin(robotHeading) + translation.y * (float)Math.Cos(robotHeading)
            );
        }
    }

    private void Awake() {
        rigidBody = GetComponent<Rigidbody>();
    }

    private void Start() {
        modules[0] = frontLeftModule;
        modules[1] = frontRightModule;
        modules[2] = backLeftModule;
        modules[3] = backRightModule;

        // 8 x 3
        inverseKinematics = new float[8, 3]{
            {1,  0,  -TRACK_WIDTH / 2 },
            {0,  1,  -WHEEL_BASE   / 2 },
            {1,  0,  TRACK_WIDTH  / 2 },
            {0,  1,  -WHEEL_BASE   / 2 },
            {1,  0,  -TRACK_WIDTH / 2 },
            {0,  1,  WHEEL_BASE  / 2 },
            {1,  0,  TRACK_WIDTH  / 2 },
            {0,  1,  WHEEL_BASE  / 2 },
        };

        rigidBody.maxLinearVelocity = MAX_LINEAR_VELOCITY;
        rigidBody.maxAngularVelocity = MAX_ANGULAR_VELOCITY;
    }

    // Change max tranlation speed based on current angular velocity
    private float GetMaxTranslationSpeed() {
        return MAX_LINEAR_VELOCITY - (rigidBody.angularVelocity.y * 3 / MAX_ANGULAR_VELOCITY);
    }

    private void FixedUpdate() {

        ChassisSpeeds requestedSpeeds = GetFieldRelativeChassiSpeeds();
        ChassisSpeeds currentSpeeds = new ChassisSpeeds(new Vector2(rigidBody.velocity.x, rigidBody.velocity.z), rigidBody.angularVelocity.y);

        int rotationMultiplier = 1;
        if (requestedSpeeds.rotation < currentSpeeds.rotation) rotationMultiplier = -1;

        bool angularBraking = requestedSpeeds.rotation == 0f;
        float angularAcceleration = angularBraking ? BRAKE_ANGULAR_ACCEL : MAX_ANGULAR_ACCEL;

        if ((currentSpeeds.rotation + angularAcceleration * Time.fixedDeltaTime >= requestedSpeeds.rotation && currentSpeeds.rotation <= requestedSpeeds.rotation) 
            || (currentSpeeds.rotation - angularAcceleration * Time.fixedDeltaTime <= requestedSpeeds.rotation && requestedSpeeds.rotation <= currentSpeeds.rotation)) {
            rigidBody.angularVelocity = new Vector3(0, requestedSpeeds.rotation, 0);
        } else {
            rigidBody.AddTorque(new Vector3(0, rotationMultiplier * angularAcceleration, 0), ForceMode.Acceleration);
        }


        bool linearBraking = requestedSpeeds.translation.magnitude == 0f;
        Vector2 linearAcceleration;
        if (linearBraking) {
            linearAcceleration = new Vector2(BRAKE_LINEAR_ACCEL, BRAKE_LINEAR_ACCEL);
        } else {
            linearAcceleration = new Vector2(requestedSpeeds.translation.x - currentSpeeds.translation.x, requestedSpeeds.translation.y - currentSpeeds.translation.y);
            linearAcceleration = linearAcceleration.normalized * MAX_LINEAR_ACCEL;
        }

        // X translation
        int xMultiplier = 1;
        if (requestedSpeeds.translation.x < currentSpeeds.translation.x) xMultiplier = -1;

        if ((currentSpeeds.translation.x + linearAcceleration.x * Time.fixedDeltaTime >= requestedSpeeds.translation.x && currentSpeeds.translation.x <= requestedSpeeds.translation.x) 
            || (currentSpeeds.translation.x - linearAcceleration.x * Time.fixedDeltaTime <= requestedSpeeds.translation.x && requestedSpeeds.translation.x <= currentSpeeds.translation.x)) {
            rigidBody.velocity = new Vector3(requestedSpeeds.translation.x, 0, rigidBody.velocity.z);
        } else {
            rigidBody.AddForce(new Vector3(Math.Abs(linearAcceleration.x) * xMultiplier, 0, 0), ForceMode.Acceleration);
        }

        // Z translation
        int zMultiplier = 1;
        if (requestedSpeeds.translation.y < currentSpeeds.translation.y) zMultiplier = -1;
        
        if ((currentSpeeds.translation.y + linearAcceleration.y * Time.fixedDeltaTime >= requestedSpeeds.translation.y && currentSpeeds.translation.y <= requestedSpeeds.translation.y) 
            || (currentSpeeds.translation.y - linearAcceleration.y * Time.fixedDeltaTime <= requestedSpeeds.translation.y && requestedSpeeds.translation.y <= currentSpeeds.translation.y)) {
            rigidBody.velocity = new Vector3(rigidBody.velocity.x, 0, requestedSpeeds.translation.y);
        } else {
            rigidBody.AddForce(new Vector3(0, 0, Math.Abs(linearAcceleration.y) * zMultiplier), ForceMode.Acceleration);
        }
    }

    private ChassisSpeeds GetFieldRelativeChassiSpeeds() {
        Vector2 translation = GameInput.Instance.GetTranslationVectorSnapping();
        float rotation = GameInput.Instance.GetRotationInput();

        Vector2 translationSpeed = translation * GetMaxTranslationSpeed();
        if (translationSpeed.magnitude > GetMaxTranslationSpeed()) {
            translation = translation.normalized;
        }

        return new ChassisSpeeds(
            translation     *       GetMaxTranslationSpeed(),
            rotation        *       MAX_ANGULAR_VELOCITY
        );
    }

    private void Update() {
        // totalTime += Time.deltaTime;
        Vector2 input = GameInput.Instance.GetTranslationVector();

        // float maxWheelSpeed = 10000f; // degrees per second

        // float speed = (float)Math.Sqrt(Math.Pow(input[0], 2) + Math.Pow(input[1], 2)) * maxWheelSpeed;
        // float angle = GameInput.Instance.GetInputRotation();

        ChassisSpeeds chassisSpeeds = GetInputValues();
        chassisSpeeds.ConvertToFieldRelative(GetHeading());        

        SwerveModule.ModuleState[] moduleStates = GetModuleStatesFromChassisSpeeds(chassisSpeeds);

        for (int i = 0; i < modules.Length; i++) {
            // modules[i].SetState(moduleStates[i]);

            // modules[i].SetState(new SwerveModule.ModuleState(MAX_TRANSLATION_SPEED, 0));
        }

        // rigidBody.velocity = new Vector3(0, 0, 10);

    }

    private ChassisSpeeds GetInputValues() {
        Vector2 translation = GameInput.Instance.GetTranslationVector();
        float rotation = GameInput.Instance.GetRotationInput();

        return new ChassisSpeeds(
            translation     *       WHEEL_MAX_TRANSLATION_SPEED,
            rotation        *       WHEEL_MAX_ANGULAR_SPEED
        );
    } 

    private SwerveModule.ModuleState[] GetModuleStatesFromChassisSpeeds(ChassisSpeeds chassisSpeeds) {
        // ChassisSpeeds inputValues = GetInputValues();
        float[,] chassisSpeedMatrix = {{chassisSpeeds.translation[1]}, {chassisSpeeds.translation[0]}, {chassisSpeeds.rotation}};

        float[] moduleStatesMatrix = MultiplyMatrices(inverseKinematics, chassisSpeedMatrix);

        bool wheelAboveMaxSpeed = false;

        SwerveModule.ModuleState[] moduleStates = new SwerveModule.ModuleState[4];
        for (int i = 0; i < moduleStates.Length; i++) {
            float speed = CalculateHypotenuse(moduleStatesMatrix[i*2], moduleStatesMatrix[i*2 + 1]);
            
            float rawAngle = (float)Math.Atan2(moduleStatesMatrix[i*2 + 1] / speed, moduleStatesMatrix[i*2] / speed) * Mathf.Rad2Deg;
            float angle = rawAngle < 0 ? rawAngle + 360 : rawAngle;

            if (speed > WHEEL_MAX_TRANSLATION_SPEED) wheelAboveMaxSpeed = true;

            moduleStates[i] = new SwerveModule.ModuleState(speed, angle);
        }

        if (wheelAboveMaxSpeed) {
            DesaturateWheelSpeeds(moduleStates, WHEEL_MAX_TRANSLATION_SPEED);
        }

        return moduleStates;
    }  

    // ONLY CALL THIS IF ONE OR MORE MODULE SPEEDS ARE ABOVE MAX SPEED
    private void DesaturateWheelSpeeds(SwerveModule.ModuleState[] moduleStates, float maxAttainableSpeed) {
        float[] speeds = new float[4];
        float maxAssignedSpeed = 0f;
        for (int i = 0; i < speeds.Length; i++) {
            speeds[i] = moduleStates[i].speed;
            maxAssignedSpeed = Math.Max(Math.Abs(speeds[i]), maxAssignedSpeed);
        }

        if (maxAssignedSpeed < maxAttainableSpeed) {
            Debug.LogWarning("Tried to desaturate wheel speeds that do not need to be desaturated!");
            return;
        }

        for (int i = 0; i < speeds.Length; i++) {
            moduleStates[i].speed = speeds[i] / maxAssignedSpeed * maxAttainableSpeed;
        }
    }

    private float[] MultiplyMatrices(float[,] first, float[,] second) {
        float[] result = new float[first.GetLength(0)];
        for (int i = 0; i < result.Length; i++) {
            for (int j = 0; j < second.GetLength(0); j++) {
                result[i] += second[j, 0] * first[i, j];
            }
        }
        return result;
    }

    public void AddForceAtPosition(Vector3 force, Vector3 position) {
        rigidBody.AddForceAtPosition(force, position);
    }

    private float CalculateHypotenuse(float x, float y) {
        return (float)Math.Sqrt(x*x + y*y);
    }

    public float GetHeading() {
        return transform.eulerAngles.y;
    }

    public float GetVelocity() {
        Vector3 velocity = rigidBody.velocity;
        return CalculateHypotenuse(velocity.x, velocity.z);
    }
}
