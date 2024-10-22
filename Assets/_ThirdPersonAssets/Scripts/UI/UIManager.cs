using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    public static UIManager instance {  get; private set; }

    public event EventHandler OnWeaponWheelEnable;

    public event EventHandler OnWeaponWheelDisable;

    [SerializeField] private GameObject touchscreenUI;
    [SerializeField] private GameObject weaponWheel;
    [SerializeField] private GameObject crosshairUI;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        instance = this;
    }

    private void Update()
    {
        HandleTouchscreenUI();
        HandleWeaponWheelUI();
        HandleCrosshairUI();
    }

    private void HandleTouchscreenUI()
    {
        if (GameInput.instance.GetInputControlScheme().Equals("Touchscreen") && !GameInput.instance.gamepadMode)
        {
            touchscreenUI.SetActive(true);
        }
        else
        {
            touchscreenUI.SetActive(false);
        }
    }

    private void HandleWeaponWheelUI()
    {
        if (GameInput.instance.isWeaponWheelPressed)
        {
            if (GameInput.instance.GetInputControlScheme().Equals("KBM"))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            weaponWheel.SetActive(true);
            OnWeaponWheelEnable?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            weaponWheel.SetActive(false);
            OnWeaponWheelDisable?.Invoke(this, EventArgs.Empty);
        }
    }

    private void HandleCrosshairUI()
    {
        if (Player.instance.currentStance == Player.Stance.Rifle || Player.instance.currentStance == Player.Stance.Pistol)
        {
            crosshairUI.SetActive(true);
        }
        else
        {
            crosshairUI.SetActive(false);
        }
    }
}
