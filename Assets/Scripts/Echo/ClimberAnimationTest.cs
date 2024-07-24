using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClimberAnimationTest : MonoBehaviour {

    private const string CLIMBER_UP = "ClimberUp";

    private Animator animator;

    private void Awake() {
        animator = GetComponent<Animator>();
    }

    private void Update() {
        // if (Input.GetKeyUp(KeyCode.C)) {
        //     animator.SetBool(CLIMBER_UP, !animator.GetBool(CLIMBER_UP));
        // }
    }
}
