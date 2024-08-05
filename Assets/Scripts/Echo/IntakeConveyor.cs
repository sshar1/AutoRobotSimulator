using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntakeConveyor : MonoBehaviour {

    [SerializeField] private Transform endPosition;

    private bool intakeEnabled = false;

    [SerializeField] private float speed = 1f;

    List<Cargo> cargos;

    private void Awake() {
        cargos = new List<Cargo>();
    }

    private void FixedUpdate() {
        if (!intakeEnabled) return;

        // foreach (Cargo cargo in cargos) {
        //     Vector3 dir = (endPosition.position - cargo.transform.position).normalized;
        //     cargo.GetComponent<Rigidbody>().AddForce(dir * speed);
        //     Debug.DrawRay(cargo.transform.position, dir);
        // }

        for (int i = cargos.Count - 1; i >= 0; i--) {
            if (!cargos[i].IsInRobot()) {
                Vector3 dir = (endPosition.position - cargos[i].transform.position).normalized;
                cargos[i].GetComponent<Rigidbody>().AddForce(dir * speed);
                Debug.DrawRay(cargos[i].transform.position, dir);
            } else {
                cargos.RemoveAt(i);
            }
        }
    }

    private void OnCollisionEnter(Collision collision) {
        if (collision.collider.GetComponent<Cargo>() && !cargos.Contains(collision.collider.GetComponent<Cargo>())) {
            cargos.Add(collision.collider.GetComponent<Cargo>());
        }
    }

    private void OnCollisionExit(Collision collision) {
        if (collision.collider.GetComponent<Cargo>()) {
            cargos.Remove(collision.collider.GetComponent<Cargo>());
        }
    }

    public void Enable() {
        intakeEnabled = true;
        gameObject.SetActive(true);
    }

    public void Disable() {
        intakeEnabled = false;
        gameObject.SetActive(false);
    }
}
