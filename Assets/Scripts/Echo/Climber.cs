using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Climber : MonoBehaviour {

    [SerializeField] private Transform climber;

    private const string CLIMBER_UP = "ClimberUp";

    private Animator animator;

    private void Awake() {
        animator = climber.GetComponent<Animator>();
    }

    private void Update() {
        if (GameInput.Instance.ClimberPressedThisFrame()) {
            animator.SetBool(CLIMBER_UP, !animator.GetBool(CLIMBER_UP));
        }
    }
}
