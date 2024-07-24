using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntakeAnimationTest : MonoBehaviour {

    private const string INTAKE_DOWN = "IntakeDown";

    private Animator animator;

    private void Awake() {
        animator = GetComponent<Animator>();
    }

    private void Update() {
        // if (Input.GetKey(KeyCode.I)) {
        //     animator.SetBool(INTAKE_DOWN, true);
        // } else {
        //     animator.SetBool(INTAKE_DOWN, false);
        // }
    }
}
