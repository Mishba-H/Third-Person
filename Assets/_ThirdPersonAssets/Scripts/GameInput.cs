using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Interactions;

public class GameInput : MonoBehaviour
{
    public static GameInput instance;

    public bool gamepadMode;

    [SerializeField] private PlayerInput playerInput;
    private Camera mainCam;

    public PlayerInputActions inputActions;

    #region EVENTS
    #endregion

    #region IS_ACTION_PRESSED
    public bool isWalkPressed;
    public bool isSprintPressed;
    public bool isJumpPressed;
    public bool isRollPressed;
    public bool isAttackTapped;
    public bool isAttackHeld;
    public bool isAttackAltTapped;
    public bool isAttackAltHeld;
    public bool isBlockPressed;
    public bool isFirePressed;
    public bool isAimPressed;
    public bool isReloadPressed;
    public bool isWeaponWheelPressed;
    #endregion

    private float attackPerformedTime;
    [SerializeField] private float attackInputStoreTime;
    [SerializeField] private RectTransform moveArea;
    [SerializeField] private RectTransform lookArea;

    private void Awake()
    {
        if (instance != null && instance !=  this)
        {
            Destroy(gameObject);
            Debug.LogError("Multiple instances of GameInput found");
        }
        instance = this;

        inputActions = new PlayerInputActions();
        mainCam = CameraScript.instance.mainCamera;
    }

    private void Start()
    {
        inputActions.Player.Walk.performed += Walk_Performed;
        inputActions.Player.Walk.canceled += Walk_Canceled;
        inputActions.Player.Jump.performed += Jump_Performed;
        inputActions.Player.Sprint.performed += Sprint_Performed;
        inputActions.Player.Roll.performed += Roll_Performed;
        inputActions.Player.Attack.performed += Attack_Performed;
        inputActions.Player.AttackAlt.performed += AttackAlt_Performed;
        inputActions.Player.Block.performed += Block_Performed;
        inputActions.Player.Block.canceled += Block_Canceled;
        inputActions.Player.Fire.performed += Fire_Performed;
        inputActions.Player.Fire.canceled += Fire_Canceled;
        inputActions.Player.Aim.performed += Aim_Performed;
        inputActions.Player.Aim.canceled += Aim_Canceled;
        inputActions.Player.Reload.performed += Reload_Performed;
        inputActions.Player.WeaponWheel.performed += WeaponWheel_Performed;
        inputActions.Player.WeaponWheel.canceled += WeaponWheel_Canceled;

        Player.instance.meleeCombat.OnPlayerAttack += MeleeCombat_OnPlayerAttack;
    }

    private void Update()
    {
        HandleAttackInputs();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        EnhancedTouch.EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
        EnhancedTouch.EnhancedTouchSupport.Disable();
    }

    public string GetCurrentDevice()
    {
        if (playerInput.GetDevice<Gamepad>() != null)
        {
            return "Gamepad";
        }
        if (playerInput.GetDevice<Touchscreen>() != null)
        {
            return "Touchscreen";
        }
        return null;
    }

    public string GetInputControlScheme()
    {
        return playerInput.currentControlScheme;
    }

    public Vector2 GetMoveDirection()
    {
        return inputActions.Player.Move.ReadValue<Vector2>();
    }

    public Vector2 GetLookDirection()
    {
        if (GetInputControlScheme() == "KBM")
        {
            return inputActions.Player.Look.ReadValue<Vector2>();
        }
        else if (gamepadMode)
        {
            return inputActions.Player.Look.ReadValue<Vector2>();
        }
        else if (GetInputControlScheme() == "Touchscreen")
        {
            foreach (TouchControl touch in Touchscreen.current.touches)
            {
                Vector2 touchPos = touch.startPosition.ReadValue();
                bool touchStartedInLookArea = RectTransformUtility.RectangleContainsScreenPoint(lookArea, touchPos);
                if (touchStartedInLookArea)
                {
                    return touch.delta.ReadValue();
                }
            }
        }
        return Vector2.zero;
    }

    public Vector2 GetPointerPositionFromCenter()
    {
        var pos = inputActions.Player.Pointer.ReadValue<Vector2>();
        pos = new Vector2(pos.x - Screen.width * 0.5f, pos.y - Screen.height * 0.5f);
        return pos;
    }

    public Vector3 GetMouseWorldPoint()
    {
        var mousePos = inputActions.Player.Pointer.ReadValue<Vector2>();
        return mainCam.ScreenToWorldPoint(mousePos);
    }

    public Vector3 GetScreenCenterWorldPoint()
    {
        var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        return mainCam.ScreenToWorldPoint(screenCenter);
    }

    private void Walk_Performed(InputAction.CallbackContext context)
    {
        isWalkPressed = true;
        isSprintPressed = false;
    }

    private void Walk_Canceled(InputAction.CallbackContext context)
    {
        isWalkPressed = false;
    }

    private void Sprint_Performed(InputAction.CallbackContext context)
    {
        isSprintPressed = true;
        StartCoroutine(ReleaseSprintButton());
    }

    private IEnumerator ReleaseSprintButton()
    {
        yield return null;
        isSprintPressed = false;
    }

    private void Jump_Performed(InputAction.CallbackContext context)
    {
        isJumpPressed = true;
        StartCoroutine(ReleaseJumpButton());
    }

    private IEnumerator ReleaseJumpButton()
    {
        yield return null;
        isJumpPressed = false;
    }

    private void Roll_Performed(InputAction.CallbackContext context)
    {
        isRollPressed = true;
        StartCoroutine(ReleaseRollButton());
    }

    private IEnumerator ReleaseRollButton()
    {
        yield return null;
        isRollPressed = false;
    }

    private void Attack_Performed(InputAction.CallbackContext context)
    {
        if (context.interaction is TapInteraction)
        {
            isAttackTapped = true;
            isAttackHeld = false;
        }
        else if (context.interaction is HoldInteraction)
        {
            isAttackHeld = true;
            isAttackTapped = false;
        }
        attackPerformedTime = Time.time;

        isAttackAltTapped = false;
        isAttackAltHeld = false;
    }

    private void AttackAlt_Performed(InputAction.CallbackContext context)
    {
        if (context.interaction is TapInteraction)
        {
            isAttackAltTapped = true;
            isAttackAltHeld = false;
        }
        else if (context.interaction is HoldInteraction)
        {
            isAttackAltHeld = true;
            isAttackAltTapped = false;
        }
        attackPerformedTime = Time.time;

        isAttackTapped = false;
        isAttackHeld = false;
    }

    private void Block_Performed(InputAction.CallbackContext obj)
    {
        isBlockPressed = true;
    }

    private void Block_Canceled(InputAction.CallbackContext obj)
    {
        isBlockPressed = false;
    }

    private void MeleeCombat_OnPlayerAttack(object sender, MeleeCombat.OnPlayerAttackEventArgs e)
    {
        isAttackTapped = false;
        isAttackHeld = false;
        isAttackAltTapped = false;
        isAttackAltHeld = false;
    }

    private void HandleAttackInputs()
    {
        if (Time.time - attackPerformedTime > attackInputStoreTime)
        {
            isAttackTapped = false;
            isAttackHeld = false;
            isAttackAltTapped = false;
            isAttackAltHeld = false;
        }
    }

    private void Fire_Performed(InputAction.CallbackContext context)
    {
        isFirePressed = true;
    }

    private void Fire_Canceled(InputAction.CallbackContext context)
    {   
        isFirePressed = false;
    }

    private void Aim_Performed(InputAction.CallbackContext context)
    {
        isAimPressed = true;
    }

    private void Aim_Canceled(InputAction.CallbackContext context)
    {
        isAimPressed = false;
    }

    private void Reload_Performed(InputAction.CallbackContext obj)
    {
        isReloadPressed = true;
        StartCoroutine(ReleaseReloadButton());
    }

    private IEnumerator ReleaseReloadButton()
    {
        yield return null;
        isReloadPressed = false;
    }

    private void WeaponWheel_Performed(InputAction.CallbackContext context)
    {
        isWeaponWheelPressed = true;
    }

    private void WeaponWheel_Canceled(InputAction.CallbackContext context)
    {
        isWeaponWheelPressed = false;
    }
}
