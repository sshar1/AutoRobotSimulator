using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

public class GameInput : MonoBehaviour {

    public static GameInput Instance { get; private set; }

    private PlayerInputActions playerInputActions;

    private void Awake() {
        Instance = this;

        playerInputActions = new PlayerInputActions();
        playerInputActions.Enable();
    }

    public Vector2 GetTranslationVectorNormalized() {
        return GetTranslationVector().normalized;
    }

    public Vector2 GetTranslationVector() {
        return playerInputActions.Player.Translate.ReadValue<Vector2>();
    }

    public Vector2 GetTranslationVectorSnapping() {
        float rotation = GetInputRotation();
        float deadband = 10; // In degrees

        // If closer to 0 or 180 or 360, ignore x; otherwise, ignore y
        if (rotation % 90 <= deadband || rotation % 90 >= 90 - deadband) {
            if (Mathf.Round(rotation / 90 ) == 0f || Mathf.Round(rotation / 90 ) == 2f || Mathf.Round(rotation / 90 ) == 4f) {
                return new Vector2(0, GetTranslationVector().y);
            }
            return new Vector2(GetTranslationVector().x, 0);
        }
        return GetTranslationVector();
    }

    public float GetRotationInput() {
        return playerInputActions.Player.Rotate.ReadValue<float>();
    }

    // 0 is top, 90 is left, etc going CCW+
    public float GetInputRotation() {
        Vector2 v = GetTranslationVector();
        if (Math.Abs(v[0]) < 1e-6) return 90;
        float raw = (float)Math.Atan2(v[1], v[0]) * Mathf.Rad2Deg - 90;
        return raw < 0 ? raw + 360 : raw;
    }

    public bool IntakePressed() {
        return playerInputActions.Player.Intake.IsPressed();
    }

    public bool ShootPressed() {
        return playerInputActions.Player.Shoot.IsPressed();
    }

    public bool ClimberPressedThisFrame() {
        return playerInputActions.Player.ClimbExtend.triggered && playerInputActions.Player.ClimbExtend.ReadValue<float>() > 0f;
    }

    public bool EnabledAutonPressedThisFrame() {
        return playerInputActions.Player.EnableAuton.triggered && playerInputActions.Player.EnableAuton.ReadValue<float>() > 0f;
    }

    public bool TestButtonPressedThisFrame() {
        return playerInputActions.Player.TestingButton.triggered && playerInputActions.Player.TestingButton.ReadValue<float>() > 0f;
    }
}
