using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class AutonController : MonoBehaviour {

    [SerializeField] private Unit unit;

    [SerializeField] private bool autonomousEnabled = false;
    
    [SerializeField] Cargo[] allCargos; // All red cargos
    [SerializeField] List<Cargo> availableCargos; // Red cargos that are within a certain y-threshold

    //private bool fullPathCreated = false;
    private bool followingPath = false;
    private Queue<Cargo> optimalPath = new Queue<Cargo>();

    private void Update() {

        if (GameInput.Instance.EnabledAutonPressedThisFrame()) {
            autonomousEnabled = !autonomousEnabled;
            if (!autonomousEnabled) {
                unit.TogglePathFollow(false);
                optimalPath.Clear();
                followingPath = false;
                // fullPathCreated = false;
            }
        }

        if (!autonomousEnabled) return;

        if (/*fullPathCreated && */ optimalPath.Count >= 1) {
            UnityEngine.Debug.DrawLine(transform.position, optimalPath.Peek().transform.position, Color.yellow);

            if (!followingPath) {
                unit.SetTarget(optimalPath.Dequeue().transform);
                unit.TogglePathFollow(true);
                followingPath = true;
            // } else if (!unit.GetTarget().GetComponent<Cargo>().BelowHeightThreshold()) {
            //     followingPath = false;
            //     if (optimalPath.Count == 0) {
            //         unit.TogglePathFollow(false);
            //         unit.SetTarget(null);
            //     }
            }

            if (GameInput.Instance.TestButtonPressedThisFrame()) {
                // optimalPath.RemoveAt(0);
                followingPath = false;
                print("button pressed");
                if (optimalPath.Count == 0) {
                    unit.TogglePathFollow(false);
                    // unit.SetTarget(null);
                    //fullPathCreated = false;
                }
                // unit.SetTarget(optimalPath[0].transform);
            }

            // print("running");

            return;
        };

        Stopwatch sw = new Stopwatch();
        sw.Start();

        RemoveInvalidCargos();

        float smallestCost = Mathf.Infinity;

        // TODO look for shortest path of 4 cargos
        foreach (Cargo cargo in availableCargos) {
            Cargo.CargoNeighbor neighbor = cargo.GetClosestNeighbor(availableCargos);

            float totalCost = GetDistanceFromRobot(cargo.transform.position) + neighbor.distance;
            if (totalCost < smallestCost) {
                smallestCost = totalCost;
                optimalPath.Clear();
                optimalPath.Enqueue(cargo);
                optimalPath.Enqueue(neighbor.cargo);
            }

            UnityEngine.Debug.DrawLine(cargo.transform.position, neighbor.cargo.transform.position);
        }
        sw.Stop();
        print("took " + sw.ElapsedMilliseconds + "ms");

        if (optimalPath.Count >= 2) {
            UnityEngine.Debug.DrawLine(transform.position, optimalPath.Peek().transform.position, Color.yellow);
            // UnityEngine.Debug.DrawLine(optimalPath[0].transform.position, optimalPath[1].transform.position, Color.blue);
            // fullPathCreated = true;
        } else if (optimalPath.Count == 1) {
            UnityEngine.Debug.DrawLine(transform.position, optimalPath.Peek().transform.position, Color.yellow);
            // fullPathCreated = true;
        }

        availableCargos = allCargos.ToList();
    }

    public bool autonEnabled() {
        return autonomousEnabled;
    }

    private void RemoveInvalidCargos() {
        foreach (Cargo cargo in allCargos) {
            if (!cargo.BelowHeightThreshold()) {
                availableCargos.Remove(cargo);
            }
        }
    }

    private float GetDistanceFromRobot(Vector3 otherPosition) {
        return Mathf.Sqrt((transform.position - otherPosition).magnitude);
    }
}
