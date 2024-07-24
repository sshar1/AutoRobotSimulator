using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Drivetrain : MonoBehaviour {
    
    private const float MAX_LINEAR_VELOCITY = 8f;
    private const float MAX_ANGULAR_VELOCITY = 5f;
    private const float MAX_LINEAR_ACCEL = 12f;
    private const float BRAKE_LINEAR_ACCEL = 25f;
    private const float MAX_ANGULAR_ACCEL = 10f;
    private const float BRAKE_ANGULAR_ACCEL = 20f;

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
            rigidBody.angularVelocity = new Vector3(rigidBody.angularVelocity.x, requestedSpeeds.rotation, rigidBody.angularVelocity.z);
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
            rigidBody.velocity = new Vector3(requestedSpeeds.translation.x, rigidBody.velocity.y, rigidBody.velocity.z);
        } else {
            rigidBody.AddForce(new Vector3(Math.Abs(linearAcceleration.x) * xMultiplier, 0, 0), ForceMode.Acceleration);
        }

        // Z translation
        int zMultiplier = 1;
        if (requestedSpeeds.translation.y < currentSpeeds.translation.y) zMultiplier = -1;
        
        if ((currentSpeeds.translation.y + linearAcceleration.y * Time.fixedDeltaTime >= requestedSpeeds.translation.y && currentSpeeds.translation.y <= requestedSpeeds.translation.y) 
            || (currentSpeeds.translation.y - linearAcceleration.y * Time.fixedDeltaTime <= requestedSpeeds.translation.y && requestedSpeeds.translation.y <= currentSpeeds.translation.y)) {
            rigidBody.velocity = new Vector3(rigidBody.velocity.x, rigidBody.velocity.y, requestedSpeeds.translation.y);
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
