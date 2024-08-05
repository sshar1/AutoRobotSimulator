using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cargo : MonoBehaviour {

    [SerializeField] private Color color;

    public enum Color {
        RED,
        BLUE
    }

    private bool isInRobot = false; // If the robot owns the piece
    private bool inRobotPosition = false; // If the piece is in its spot (top or bottom)
    private bool isSwitching = false;

    // private Vector3 previousParentPosition;

    // private Vector3 previousLocalPosition; // For lerping to local position
    // private bool hadParentPreviousFrame; // for lerping to local position

    public void SetParentToRobot(Transform parent, bool switching) {
        transform.parent = parent.transform;
        // GetComponent<SphereCollider>().excludeLayers = LayerMask.NameToLayer("Everything");
        // GetComponent<SphereCollider>().includeLayers = LayerMask.NameToLayer("Cargo");
        // transform.position = parent.position;
        GetComponent<SphereCollider>().enabled = false;
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        GetComponent<Rigidbody>().useGravity = false;

        isInRobot = true;
        isSwitching = switching;
    }

    public void LerpToLocalPosition(float dt, Vector3 deltaParentPosition) {
        // Vector3 deltaLocalPosition = Vector3.zero;
        // if (previousLocalPosition != Vector3.zero) {
        //     // transform.localPosition = previousLocalPosition;
        //     deltaLocalPosition = transform.localPosition - previousLocalPosition;
        // }

        // previousLocalPosition = transform.localPosition;

        // Vector3 deltaParentPosition = Vector3.zero;
        // if (GetParent() == null) {
        //     deltaParentPosition = Vector3.zero;
        //     previousParentPosition = Vector3.zero;
        //     hadParentPreviousFrame = false;
        // } else {
        //     if (hadParentPreviousFrame && !isSwitching) {
        //         deltaParentPosition = GetParent().position - previousParentPosition;
        //     }
        //     // if (previousParentPosition == Vector3.zero) {
        //     //     deltaParentPosition = Vector3.zero;
        //     // } else {
        //     //     deltaParentPosition = GetParent().position - previousParentPosition;
        //     // }
        //     previousParentPosition = GetParent().position;
        //     hadParentPreviousFrame = true;
        // }

        transform.position += deltaParentPosition;
        transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero, Mathf.Clamp01(dt * 5));// + deltaParentPosition;

        if (transform.localPosition.magnitude < 0.05f) {
            transform.localPosition = Vector3.zero;
            // previousLocalPosition = Vector3.zero;
            inRobotPosition = true;
            isSwitching = false;
        }
    }

    public void SetParentToWorldScene() {
        transform.parent = null;
        // hadParentPreviousFrame = false;

        isInRobot = false;
        inRobotPosition = false;
        isSwitching = false;
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
}
