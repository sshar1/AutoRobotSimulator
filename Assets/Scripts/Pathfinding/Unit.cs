using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Unit : MonoBehaviour {

    const float minPathUpdateTime = .1f;
    const float pathUpdateMoveThreshold = .3f;

    [SerializeField] private Transform target;
    public float speed = 10f;
    public float turnSpeed = 3;
    public float turnDst = 5;
    public float stoppingDst = 10;
    public float rotationGoalDst = 2;

    Path path;

    // void Start() {
    //     StartCoroutine("UpdatePath");
    // }

    public void TogglePathFollow(bool autonEnabled) {
        if (autonEnabled) {
            StartCoroutine("UpdatePath");
            // StopCoroutine("FollowPath");
            // StartCoroutine("FollowPath");
        } else {
            StopCoroutine("UpdatePath");
            StopCoroutine("FollowPath");
        }
    }

    public void OnPathFound(Vector3[] waypoints, bool pathSuccessful) {
        if (pathSuccessful) {
            path = new Path(waypoints, transform.position, turnDst, stoppingDst, rotationGoalDst);
            StopCoroutine("FollowPath");
            StartCoroutine("FollowPath");
        }
    }

    IEnumerator UpdatePath() {

        if (Time.timeSinceLevelLoad < .3f) {
            yield return new WaitForSeconds(.3f);
        }

        PathRequestManager.RequestPath(new PathRequest(transform.position, target.position, OnPathFound));

        float sqrMovethreshold = pathUpdateMoveThreshold * pathUpdateMoveThreshold;
        Vector3 targetPosOld = target.position;

        while (true) {
            yield return new WaitForSeconds(minPathUpdateTime);

            if ((target.position - targetPosOld).sqrMagnitude > sqrMovethreshold) {
                PathRequestManager.RequestPath(new PathRequest(transform.position, target.position, OnPathFound));
                targetPosOld = target.position;
            }
        }
    }

    IEnumerator FollowPath() {
        bool followingPath = true;
        int pathIndex = 0;
        // transform.LookAt(path.lookPoints[0]);

        float speedPercent = 1;
        float rotationPercent = 0;

        Vector2 startingPosition = new Vector2(transform.position.x, transform.position.z);

        while (followingPath) {
            Vector2 pos2D = new Vector2(transform.position.x, transform.position.z);
            while (path.turnBoundaries[pathIndex].HasCrossedLine(pos2D)) {
                if (pathIndex == path.finishLineIndex) {
                    followingPath = false;
                    break;
                } else {
                    pathIndex++;
                }
            }

            if (followingPath) {

                if (pathIndex >= path.slowDownIndex && stoppingDst > 0) {
                    speedPercent = Mathf.Clamp01(path.turnBoundaries[path.finishLineIndex].DistanceFromPoint(pos2D) / stoppingDst);
                    if (speedPercent < 0.01f) {
                        followingPath = false;
                    }
                }

                if (pathIndex <= path.rotationGoalIndex && rotationGoalDst > 0) {
                    // Debug.Log(rotationPercent);
                    rotationPercent = Mathf.Clamp01((transform.position - path.lookPoints[0]).magnitude / (path.lookPoints[path.rotationGoalIndex] - path.lookPoints[0]).magnitude);
                }

                // Quaternion targetRotation = Quaternion.LookRotation(path.lookPoints[pathIndex] - transform.position);
                Quaternion targetRotation;
                if (path.lookPoints.Length >= 2) {
                    targetRotation = Quaternion.LookRotation(path.lookPoints[path.finishLineIndex] - path.lookPoints[path.finishLineIndex-1]);
                } else {
                    targetRotation = Quaternion.LookRotation(path.lookPoints[path.finishLineIndex] - transform.position);
                }
                
                Vector3 targetSpeed = (path.lookPoints[pathIndex] - transform.position).normalized * speed;
                Vector3 newSpeed = Vector3.Lerp(GetComponent<Rigidbody>().velocity, targetSpeed, Time.deltaTime * turnSpeed);
                GetComponent<Rigidbody>().velocity = newSpeed * speedPercent;
                transform.rotation = targetRotation; //Quaternion.Lerp(transform.rotation, targetRotation, rotationPercent);
                // transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
                // transform.Translate(Vector3.forward * Time.deltaTime * speed * speedPercent, Space.Self);
            }

            yield return null;
        }
    }

    public void SetTarget(Transform target) {
        this.target = target;
    }

    public Transform GetTarget() {
        return target;
    }

    public void OnDrawGizmos() {
        if (path != null) {
            path.DrawWithGizmos();
        }
    }
}
