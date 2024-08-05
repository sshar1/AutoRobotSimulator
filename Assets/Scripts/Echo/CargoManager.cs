using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class CargoManager : MonoBehaviour {

    public static CargoManager Instance { get; private set; }

    [SerializeField] private Transform topCargoSpot;
    [SerializeField] private Transform bottomCargoSpot;
    [SerializeField] private Transform shootSpot;
    [SerializeField] private Transform intakeCover;

    private Vector3 gravity = Physics.gravity;

    private Cargo topCargo = null;
    private Cargo bottomCargo = null;

    private bool raycastBrokenLastFrame = false;

    private List<Cargo> cargos;

    private long lastShotTimestamp;

    private const float SHOT_COOLDOWN = 0.5f;

    // This should be higher for short distances and higher for longer distances
    private const float yNegativeDerivative = 9f; // Increase for a higher shot

    private Vector3 previousCargoSpotPosition;

    private void Awake() {
        Instance = this;

        cargos = new List<Cargo>();
    }

    private void Update() {
        Debug.DrawRay(transform.position, Vector3.up * 0.5f, Color.cyan);

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.up * 0.5f, out hit, 0.5f)) {
            if (!raycastBrokenLastFrame) {
                if (hit.collider.gameObject.GetComponent<Cargo>() && !hit.collider.gameObject.GetComponent<Cargo>().IsInRobot()) {//!ReferenceEquals(hit.collider.gameObject, bottomCargo.gameObject)) {
                    AddCargo(hit.collider.GetComponent<Cargo>());
                }
            }
            raycastBrokenLastFrame = true;
        } else {
            raycastBrokenLastFrame = false;
        }

        if (!TopOccupied() && topCargo == null && BottomOccupied() && bottomCargo != null) {
            MoveCargoBottomToTop();
        }
    }

    private void FixedUpdate() {
        for (int i = cargos.Count - 1; i >= 0; i--) {
            if (cargos[i].IsInRobot()) {
                if (cargos[i].IsInPosition() && !cargos[i].IsSwitching()) {
                    cargos[i].transform.position = cargos[i].transform.parent.position;
                } else {
                    cargos[i].LerpToLocalPosition(Time.fixedDeltaTime, topCargoSpot.position - previousCargoSpotPosition);
                }
                cargos[i].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            } else {
                cargos.RemoveAt(i);
            }
        }
        previousCargoSpotPosition = topCargoSpot.position;

        if (cargos.Count >= 2) {
            intakeCover.gameObject.SetActive(true);
        } else {
            intakeCover.gameObject.SetActive(false);
        }

        // foreach (Cargo cargo in cargos) {      
        //     if (cargo.IsInRobot()) {
        //         cargo.transform.position = cargo.transform.parent.position;
        //         cargo.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        //     } else {
        //         cargosToRemove.Add(cargo);
        //     }
        // }

        // foreach (Cargo cargo in cargos) {      
        //     if (cargo.IsInRobot()) {
        //         cargo.transform.position = cargo.transform.parent.position;
        //         cargo.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        //     } else {
        //         cargosToRemove.Add(cargo);
        //     }
        // }
    }

    public void ShootTopCargo(Vector3 targetPosition) {
        Debug.DrawLine(topCargoSpot.position, targetPosition, Color.white);
        if (topCargo == null || !TopOccupied() || (DateTime.Now.Ticks - lastShotTimestamp) / 10000000f < SHOT_COOLDOWN) return;

        // cargos.Remove(topCargo);
        topCargo.GetComponent<Rigidbody>().useGravity = true;
        // topCargo.transform.parent = null;
        topCargo.SetParentToWorldScene();
        topCargo.transform.position = shootSpot.transform.position;

        // Vertical is just y
        float verticalDistance = targetPosition.y - topCargo.transform.position.y;
        float verticalVelocity = GetVerticalVelocity(verticalDistance);
        float projectileTime = GetProjectileTime(verticalVelocity);

        // Horizontal is a combination of x and z
        Vector3 targetHorizontal = Vector3.Scale(targetPosition, new Vector3(1, 0, 1));
        Vector3 originHorizontal = Vector3.Scale(topCargo.transform.position, new Vector3(1, 0, 1));
        Vector3 horizontalDir = (targetHorizontal - originHorizontal).normalized;
        float distance = (targetHorizontal - originHorizontal).magnitude;
        Vector3 horizontalVelocity = distance * horizontalDir / projectileTime;

        Vector3 velocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);

        StartCoroutine(DelayCargoCollidable(topCargo, 0.2f));

        topCargo.GetComponent<Rigidbody>().velocity = velocity;
        lastShotTimestamp = DateTime.Now.Ticks;
        topCargo = null;
    }

    // Derived with calc + kinematics; check readme
    private float GetVerticalVelocity(float verticalDisplacement) {
        return (float)Math.Sqrt(2 * -gravity.y * verticalDisplacement + Math.Pow(yNegativeDerivative, 2));
    }

    // Derived with calc + kinematics; check readme
    private float GetProjectileTime(float verticalVelocity) {
        return (yNegativeDerivative + verticalVelocity) / -gravity.y;
    }

    private void AddCargo(Cargo cargo) {
        // if (!cargo.CompareTag("Cargo")) Debug.Log("not cargo!");

        if (!TopOccupied() && topCargo == null) {
            cargo.SetParentToRobot(topCargoSpot, false);

            topCargo = cargo;
            cargos.Add(cargo);
        } else if (!BottomOccupied() && bottomCargo == null) {
            cargo.SetParentToRobot(bottomCargoSpot, false);

            bottomCargo = cargo;
            cargos.Add(cargo);
        } else {
            Debug.Log("tried to intake third ball!");
        }

        // if (TopOccupied() || topCargo != null) {
        //     // cargo.transform.parent = bottomCargoSpot.transform;
        //     // cargo.transform.position = bottomCargoSpot.position;
        //     // cargo.GetComponent<Rigidbody>().velocity = Vector3.zero;
        //     // cargo.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        //     // cargo.GetComponent<Rigidbody>().useGravity = false;
        //     cargo.SetParentToRobot(bottomCargoSpot);

        //     bottomCargo = cargo;
        //     cargos.Add(cargo);
        // } else if (!BottomOccupied() && bottomCargo == null) {
        //     // cargo.transform.parent = topCargoSpot.transform;
        //     // cargo.transform.position = topCargoSpot.position;
        //     // cargo.GetComponent<Rigidbody>().velocity = Vector3.zero;
        //     // cargo.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        //     // cargo.GetComponent<Rigidbody>().useGravity = false;
        //     cargo.SetParentToRobot(topCargoSpot);

        //     topCargo = cargo;
        //     cargos.Add(cargo);
        // } else {
        //     Debug.Log("trying to intake third ball!");
        // }
    }

    private void MoveCargoBottomToTop() {
        // If nothing is in the bottom or something is in top
        if (bottomCargo == null || !BottomOccupied() || TopOccupied() || topCargo != null) return;

        // bottomCargo.transform.parent = topCargoSpot;
        // bottomCargo.transform.position = topCargoSpot.position;
        bottomCargo.SetParentToRobot(topCargoSpot, true);
        topCargo = bottomCargo;
        bottomCargo = null;
    }

    private bool TopOccupied() {
        foreach (Transform child in topCargoSpot.transform) {
            if (child.GetComponent<Cargo>() && child.GetComponent<Cargo>().IsInPosition()) {
                return true;
            }
        }
        return false;
    }

    private bool BottomOccupied() {
        foreach (Transform child in bottomCargoSpot.transform) {
            if (child.GetComponent<Cargo>() && child.GetComponent<Cargo>().IsInPosition()) {
                return true;
            }
        }
        return false;
    }

    IEnumerator DelayCargoCollidable(Cargo cargo, float delayTime) {
        cargo.GetComponent<SphereCollider>().enabled = false;
        yield return new WaitForSeconds(delayTime);
        cargo.GetComponent<SphereCollider>().enabled = true;
        // cargo.GetComponent<SphereCollider>().includeLayers = LayerMask.GetMask("Nothing");
        // cargo.GetComponent<SphereCollider>().excludeLayers = LayerMask.GetMask("Nothing");
    }

}
