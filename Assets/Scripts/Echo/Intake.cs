using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Intake : MonoBehaviour {

    [SerializeField] private List<IntakeConveyor> conveyors;

    private const string INTAKE_DOWN = "IntakeDown";

    private Animator animator;

    private void Awake() {
        animator = GetComponent<Animator>();
    }

    private void Update() {
        if (GameInput.Instance.IntakePressed()) {
            animator.SetBool(INTAKE_DOWN, true);
            EnableConveyors();
        } else {
            animator.SetBool(INTAKE_DOWN, false);
            DisableConveyors();
        }
    }

    private void EnableConveyors() {
        foreach (IntakeConveyor conveyor in conveyors) {
            conveyor.Enable();
        }
    }

    private void DisableConveyors() {
        foreach (IntakeConveyor conveyor in conveyors) {
            conveyor.Disable();
        }
    }
}
