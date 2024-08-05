using System;
using System.Collections;
using System.Collections.Generic;
using Boxophobic.StyledGUI;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class TurretController : MonoBehaviour {

    [SerializeField] private Transform hood;
    [SerializeField] private Transform target;
    [SerializeField] private Drivetrain robot;
    [SerializeField] private Transform goalTurretAngle;

    private float currentAngle;
    private const float ANGLE_RADIUS = 270; // Can rotate -270 to 270

    private const float MAX_HOOD_ANGLE = 35f;

    private float previousAngle;
    private float goalAngle;

    private void FixedUpdate() {
        Vector3 dir = target.position - transform.position;
        Quaternion lookRot = Quaternion.LookRotation(dir);
        lookRot.x = 0; lookRot.z = 0;

        float previousGoal = goalTurretAngle.localEulerAngles.y;
        goalTurretAngle.rotation = lookRot;

        if (AreClockwise(previousGoal, goalTurretAngle.localEulerAngles.y)) {
            goalAngle += Mod(goalTurretAngle.localEulerAngles.y - previousGoal, 360);
        } else {
            goalAngle += (goalTurretAngle.localEulerAngles.y - previousGoal - 360) % 360;
        }

        if (goalAngle < -ANGLE_RADIUS) goalAngle += 360;
        else if (goalAngle > ANGLE_RADIUS) goalAngle -= 360;

        float rotationSpeed = 5f;
        float y = Mathf.Lerp(currentAngle, goalAngle, Mathf.Clamp01(rotationSpeed * Time.fixedDeltaTime));
        transform.localRotation = Quaternion.Euler(0, y, 0);

        if (AreClockwise(previousAngle, transform.localEulerAngles.y)) {
            currentAngle += Mod(transform.localEulerAngles.y - previousAngle, 360);
        } else {
            currentAngle += (transform.localEulerAngles.y - previousAngle - 360) % 360;
        }

        previousAngle = transform.localEulerAngles.y;

        Debug.DrawRay(goalTurretAngle.position, goalTurretAngle.forward * (goalTurretAngle.position - target.position).magnitude + new Vector3(0, 6, 0), Color.green);
        Debug.DrawRay(transform.position, transform.forward * (transform.position - target.position).magnitude + new Vector3(0, 6, 0), Color.red);

        float hoodRotationGoal = 0f;
        if (GameInput.Instance.ShootPressed()) {
            hoodRotationGoal = Math.Clamp(GetHorizontalDistanceToTarget(), 0, 35);
            if (Math.Abs(hood.localEulerAngles.x - hoodRotationGoal) < 0.1f) {
                CargoManager.Instance.ShootTopCargo(target.position + new Vector3(0, 1f, 0)); // 2f
            }
        }
        
        hood.localRotation = Quaternion.Euler(Mathf.Lerp(hood.localEulerAngles.x, hoodRotationGoal, Mathf.Clamp01(10f * Time.fixedDeltaTime)), 0, 0);
    }

    private float Mod(float x, float m) {
        return (x%m + m)%m;
    }

    // Returns true if the path from a to b is clockwise
    private bool AreClockwise(float a, float b) {
        float diff;
        if (b > a) {
            diff = b - a;
            return diff >= 0 && diff <= 180;
        }
        diff = a - b;
        return diff >= 180;
    }

    private float GetHorizontalDistanceToTarget() {
        Vector3 turretFlat = Vector3.Scale(transform.position, new Vector3(1, 0, 1));
        Vector3 targetFlat = Vector3.Scale(target.transform.position, new Vector3(1, 0, 1));

        return (targetFlat - turretFlat).magnitude;
    }
}
