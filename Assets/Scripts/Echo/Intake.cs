using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Intake : MonoBehaviour {

    [SerializeField] private Transform intake;

    private const string INTAKE_DOWN = "IntakeDown";

    private Animator animator;

    private void Awake() {
        animator = intake.GetComponent<Animator>();
    }

    private void Update() {
        if (GameInput.Instance.IntakePressed()) {
            animator.SetBool(INTAKE_DOWN, true);
        } else {
            animator.SetBool(INTAKE_DOWN, false);
        }
        
    }
}
