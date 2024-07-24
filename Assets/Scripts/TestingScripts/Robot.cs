using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class Robot : MonoBehaviour {

    private const float MAX_TRANSLATION_SPEED = 5f;
    private const float MAX_ROTATION_SPEED = 8f;
    private const float MAX_TRANSLATION_ACCELERATION = 9999999f; // 3000
    private const float MAX_ROTATION_ACCELERATION = 9999f; // 1000

    private struct ChassisSpeeds {
        private Vector2 translation;
        private float rotation;

        public ChassisSpeeds(Vector2 translation, float rotation) {
            this.translation = translation;
            this.rotation = rotation;
        }
    }

    private float xSpeed;
    private float ySpeed;
    private float rotationSpeed;

    private new Rigidbody rigidbody;

    private void Awake() {
        rigidbody = GetComponent<Rigidbody>();
    }

    // Use forces instead: find force needed to go from current speed to desired speed and simply apply that
    private void FixedUpdate() {
        // Vector2 inputTranslationVectorNormalized = GameInput.Instance.GetTranslationVectorNormalized();
        // TODO feels weird translating when changing directions (-x to +x or -y to +y)
        Vector2 inputTranslationVector = GameInput.Instance.GetTranslationVector();
        float rotationValue = GameInput.Instance.GetRotationInput();

        ChassisSpeeds chassisSpeeds = GetFieldRelativeChassisSpeeds(
            inputTranslationVector                  *       MAX_TRANSLATION_SPEED, 
            rotationValue                           *       MAX_ROTATION_SPEED, 
            gameObject.transform.rotation.y         *       Mathf.Deg2Rad
        );
        
        float frameMaxAccelMagnitude = MAX_TRANSLATION_ACCELERATION * Time.fixedDeltaTime;

        Vector2 desiredSpeed = new Vector2(
            MAX_TRANSLATION_SPEED * inputTranslationVector.x, 
            MAX_TRANSLATION_SPEED * inputTranslationVector.y
        );
        desiredSpeed = desiredSpeed.magnitude > MAX_TRANSLATION_SPEED ? ScaleVectorToMagnitude(desiredSpeed, MAX_TRANSLATION_SPEED) : desiredSpeed;

        // Debug.Log(desiredSpeed);

        Vector2 desiredAccel = new Vector2(
            Math.Abs(desiredSpeed.x - rigidbody.velocity.x) / Time.fixedDeltaTime,
            Math.Abs(desiredSpeed.y - rigidbody.velocity.z) / Time.fixedDeltaTime
        );
        float desiredAccelMagnitude = desiredAccel.magnitude;
        Vector2 resultAccel = (desiredAccelMagnitude > frameMaxAccelMagnitude) ? ScaleVectorToMagnitude(desiredAccel, frameMaxAccelMagnitude) : desiredAccel;

        // float maxXDeltaSpeed = resultAccel.x * Time.deltaTime;
        // float maxYDeltaSpeed = resultAccel.y * Time.deltaTime;
        xSpeed = rigidbody.velocity.x + (resultAccel.x * GetSpeedSign(inputTranslationVector.x, rigidbody.velocity.x) * Time.fixedDeltaTime);
        ySpeed = rigidbody.velocity.z + (resultAccel.y * GetSpeedSign(inputTranslationVector.y, rigidbody.velocity.z) * Time.fixedDeltaTime);
        xSpeed = Math.Clamp(xSpeed, -MAX_TRANSLATION_SPEED, MAX_TRANSLATION_SPEED);
        ySpeed = Math.Clamp(ySpeed, -MAX_TRANSLATION_SPEED, MAX_TRANSLATION_SPEED);

        // if (maxXDeltaSpeed > Math.Abs(rigidbody.velocity.x - desiredSpeed.x)) {
        //     xSpeed = rigidbody.velocity.x + (resultAccel.x * GetSpeedSign(inputTranslationVector.x, rigidbody.velocity.x) * Time.deltaTime); //desiredSpeed.x;
        // } else {
        //     xSpeed = rigidbody.velocity.x + (resultAccel.x * GetSpeedSign(inputTranslationVector.x, rigidbody.velocity.x) * Time.deltaTime);
        // }

        // if (maxYDeltaSpeed > Math.Abs(rigidbody.velocity.z - desiredSpeed.y)) {
        //     ySpeed = rigidbody.velocity.x + (resultAccel.x * GetSpeedSign(inputTranslationVector.x, rigidbody.velocity.x) * Time.deltaTime);// desiredSpeed.y;
        // } else {
        //     ySpeed = rigidbody.velocity.z + (resultAccel.y * GetSpeedSign(inputTranslationVector.y, rigidbody.velocity.z) * Time.deltaTime);
        // }

        Vector2 speedVector = new Vector2(xSpeed, ySpeed);
        // if (speedVector.magnitude > MAX_TRANSLATION_SPEED) {
        //     Debug.Log(speedVector.magnitude);
        // }
        if (speedVector.magnitude > MAX_TRANSLATION_SPEED) {
            speedVector = GetClampedSpeedVector(speedVector, MAX_TRANSLATION_SPEED);
        }

        // Debug.Log((rotationSpeed == rigidbody.angularVelocity.y) + ",     " + rotationSpeed + ",    " + rigidbody.angularVelocity.y);


        float desiredRotationSpeed = rotationValue * MAX_ROTATION_SPEED;
        float frameMaxRotationAccel = MAX_ROTATION_ACCELERATION * Time.fixedDeltaTime;
        float desiredRotationAccel = Math.Abs(desiredRotationSpeed - rigidbody.angularVelocity.y) / Time.fixedDeltaTime;
        // Debug.Log(desiredRotationAccel + ",     " + frameMaxRotationAccel);
        float rotationAccel = Math.Min(desiredRotationAccel, frameMaxRotationAccel);
        rotationSpeed = rigidbody.angularVelocity.y + (rotationAccel * GetSpeedSign(desiredRotationSpeed, rigidbody.angularVelocity.y) * Time.fixedDeltaTime);
        // Debug.Log(rotationAccel * GetSpeedSign(desiredRotationSpeed, rigidbody.angularVelocity.y) * Time.deltaTime);
        // Debug.Log(rotationSpeed);
        // Debug.Log(rotationSpeed + ",    " + rigidbody.angularVelocity.y);

        // Debug.Log(rotationSpeed + ",    " + desiredRotationSpeed);
        // float rotationAcceleration = Math.Abs(desiredRotationSpeed - lastDesiredRotationSpeed) * Time.deltaTime * ACCELERATION_CONSTANT;

        // Debug.Log(speedVector.x + ",   " + desiredXSpeed);

        rigidbody.velocity = new Vector3(speedVector.x, 0, speedVector.y);
        rigidbody.angularVelocity = new Vector3(0, rotationSpeed, 0);
    }

    private Vector2 ScaleVectorToMagnitude(Vector2 oldVector, float newMagnitude) {
        return oldVector.normalized * newMagnitude;
    }

    private Vector2 GetClampedSpeedVector(Vector2 speedVector, float maxSpeed) {
        return speedVector.normalized * maxSpeed;
    }

    private int GetSpeedSign(float inputTranslation, float currentSpeed) {
        int sign = Math.Sign(inputTranslation);

        return sign == 0 ? -Math.Sign(currentSpeed) : sign;
    }

    private ChassisSpeeds GetFieldRelativeChassisSpeeds(Vector2 inputTranslation, float inputRotation, float robotHeading) {
        Vector2 rotated = RotateBy(inputTranslation, robotHeading);
        return new ChassisSpeeds(rotated, inputRotation);
    }

    // Angle must be in radians!!
    private Vector2 RotateBy(Vector2 vector, float angle) {
        return new Vector2(
            (float)(vector.x * Math.Cos(angle) - vector.y * Math.Sin(angle)),
            (float)(vector.x * Math.Sin(angle) + vector.y * Math.Cos(angle))
        );
    }
}
