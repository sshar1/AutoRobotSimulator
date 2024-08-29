using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cargo : MonoBehaviour {

    const float HEIGHT_THRESHOLD = 1f;

    [SerializeField] private Color color;

    public enum Color {
        RED,
        BLUE
    }

    private bool isInRobot = false; // If the robot owns the piece
    private bool inRobotPosition = false; // If the piece is in its spot (top or bottom)
    private bool isSwitching = false;

    public void SetParentToRobot(Transform parent, bool switching) {
        transform.parent = parent.transform;
        GetComponent<SphereCollider>().enabled = false;
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        GetComponent<Rigidbody>().useGravity = false;

        isInRobot = true;
        isSwitching = switching;
    }

    public void LerpToLocalPosition(float dt, Vector3 deltaParentPosition) {
        transform.position += deltaParentPosition;
        transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero, Mathf.Clamp01(dt * 5));// + deltaParentPosition;

        if (transform.localPosition.magnitude < 0.05f) {
            transform.localPosition = Vector3.zero;
            inRobotPosition = true;
            isSwitching = false;
        }
    }

    public void SetParentToWorldScene() {
        transform.parent = null;

        isInRobot = false;
        inRobotPosition = false;
        isSwitching = false;
    }

    public CargoNeighbor GetClosestNeighbor(List<Cargo> cargos) {
        float minDistance = Mathf.Infinity;
        Cargo closestCargo = null;

        foreach (Cargo cargo in cargos) {
            if (ReferenceEquals(cargo, this)) continue;

            float distance = (this.transform.position - cargo.transform.position).sqrMagnitude;
            if (distance < minDistance) {
                closestCargo = cargo;
                minDistance = distance;
            }
        }

        return new CargoNeighbor(closestCargo, Mathf.Sqrt(minDistance));
    }

    public bool BelowHeightThreshold() {
        return transform.position.y < HEIGHT_THRESHOLD;
    }

    public Transform GetParent() {
        return transform.parent;
    }

    public bool IsInRobot() {
        return isInRobot;
    }

    public bool IsInPosition() {
        return inRobotPosition;
        // return isInRobot && (transform.localPosition - Vector3.zero).magnitude < 0.01f;
    }

    public bool IsSwitching() {
        return isSwitching;
    }

    public Color GetColor() {
        return color;
    }

    public struct CargoNeighbor {
        public Cargo cargo;
        public float distance;

        public CargoNeighbor(Cargo cargo, float distance) {
            this.cargo = cargo;
            this.distance = distance;
        }
    }
}
