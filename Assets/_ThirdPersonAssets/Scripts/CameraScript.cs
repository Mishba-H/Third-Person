using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public static CameraScript instance;

    public event EventHandler OnCameraModeChanged;

    public enum CameraMode
    {
        Basic,
        Action,
        Aim,
        TopDown
    }

    public bool lockCameraRotation;
    public CameraMode currentMode;

    [SerializeField] internal Camera mainCamera;
    [SerializeField] private CameraModeSO[] cameraModeSOs;
    [SerializeField] private float modeChangeSpeed;
    [SerializeField] private Transform playerOrientation;
    [SerializeField] private LayerMask cameraCollisionLayer;
    [SerializeField] private float followSpeed;

    private float mouseSensitivity;
    private float gamepadSensitivity;
    private float touchscreenSensitivity;
    private Vector3 pivotOffset;
    private Vector3 offsetDir;
    private Vector3 cameraPositionOffset;
    private Vector3 cameraRotationOffset;
    private float topClampAngle;
    private float bottomClampAngle;

    private Vector2 lookDir;
    private float xRotation;
    private float yRotation;
    
    public Vector2 recoil;
    public float recoilTime;
    public float returnSpeed;
    private float remainingTime; 
    private Vector3 currentRotation;
    private Vector3 targetRotation;
    private Vector3 recoilOffset;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            Debug.LogError("Multiple instances of CameraScript found");
        }
        instance = this;
    }

    private void Start()
    {
        currentMode = CameraMode.Basic;
        InitializeCamera();

        Player.instance.rangedCombat.OnShoot += RangedCombat_OnShoot;

        UIManager.instance.OnWeaponWheelEnable += UiManager_OnWeaponWheelEnable;
        UIManager.instance.OnWeaponWheelDisable += UiManager_OnWeaponWheelDisable;
    }

    private void Update()
    {
        HandleCameraMode();
        CalculateCameraRotations();
        HandleCameraRecoil();
    }

    private void LateUpdate()
    {
        MoveCamera();
    }

    private void RangedCombat_OnShoot(object sender, EventArgs e)
    {
        GenerateCameraRecoil();
    }

    private void UiManager_OnWeaponWheelEnable(object sender, EventArgs e)
    {
        lockCameraRotation = true;
    }

    private void UiManager_OnWeaponWheelDisable(object sender, EventArgs e)
    {
        lockCameraRotation = false;
    }

    private void InitializeCamera()
    {
        foreach (CameraModeSO cameraModeSO in cameraModeSOs)
        {
            if (cameraModeSO.mode == currentMode)
            {
                mouseSensitivity = cameraModeSO.mouseSensitivity;
                gamepadSensitivity = cameraModeSO.gamepadSensitivity;
                touchscreenSensitivity = cameraModeSO.touchscreenSensitivity;
                pivotOffset = cameraModeSO.pivotOffset;
                cameraPositionOffset = cameraModeSO.cameraPositionOffset;
                cameraRotationOffset = cameraModeSO.cameraRotationOffset;
                topClampAngle = cameraModeSO.topClampAngle;
                bottomClampAngle = cameraModeSO.bottomClampAngle;
            }
        }
    }

    internal void SwitchCameraMode(CameraMode newMode)
    {
        if (newMode == currentMode) return;

        currentMode = newMode;

        foreach (CameraModeSO cameraModeSO in cameraModeSOs)
        {
            if (cameraModeSO.mode == currentMode)
            {
                mouseSensitivity = cameraModeSO.mouseSensitivity;
                gamepadSensitivity = cameraModeSO.gamepadSensitivity;
                touchscreenSensitivity = cameraModeSO.touchscreenSensitivity;
                pivotOffset = cameraModeSO.pivotOffset;
                cameraPositionOffset = cameraModeSO.cameraPositionOffset;
                cameraRotationOffset = cameraModeSO.cameraRotationOffset;
                topClampAngle = cameraModeSO.topClampAngle;
                bottomClampAngle = cameraModeSO.bottomClampAngle;
            }
        }

        OnCameraModeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void HandleCameraMode()
    {
        if (Player.instance.currentStance == Player.Stance.Pistol || Player.instance.currentStance == Player.Stance.Rifle)
        {
            if (Player.instance.rangedCombat.isAiming)
            {
                SwitchCameraMode(CameraMode.Aim);
            }
            else
            {
                SwitchCameraMode(CameraMode.Action);
            }
        }
        else
        {
            SwitchCameraMode(CameraMode.Basic);
        }
    }

    private void CalculateCameraRotations()
    {
        if (lockCameraRotation) return;

        lookDir = GameInput.instance.GetLookDirection();

        if (GameInput.instance.GetInputControlScheme() == "KBM")
        {
            xRotation -= lookDir.y * mouseSensitivity * 0.01f;
            yRotation += lookDir.x * mouseSensitivity * 0.01f;
        }
        else if (GameInput.instance.gamepadMode)
        {
            xRotation -= lookDir.y * Time.deltaTime * gamepadSensitivity * 10;
            yRotation += lookDir.x * Time.deltaTime * gamepadSensitivity * 10;
        }
        else if (GameInput.instance.GetInputControlScheme() == "Touchscreen")
        {
            xRotation -= lookDir.y * Time.deltaTime * touchscreenSensitivity * 0.5f;
            yRotation += lookDir.x * Time.deltaTime * touchscreenSensitivity * 0.5f;
        }

        xRotation = Mathf.Clamp(xRotation, -topClampAngle - cameraRotationOffset.x, bottomClampAngle - cameraRotationOffset.x);
    }

    private void MoveCamera()
    {
        //Update position and rotation of camera holder(pivot)
        offsetDir = pivotOffset.x * new Vector3(playerOrientation.right.x, 0f, playerOrientation.right.z) +
            pivotOffset.z * new Vector3(playerOrientation.forward.x, 0f, playerOrientation.forward.z) + pivotOffset.y * Vector3.up;
        transform.position = playerOrientation.position + offsetDir;
        transform.rotation = Quaternion.Euler(xRotation + recoilOffset.x, yRotation + recoilOffset.y, 0);

        //Update position and rotation of camera when CameraMode is changed
        Vector3 currentPos = mainCamera.transform.localPosition;
        Vector3 targetPos = cameraPositionOffset;
        mainCamera.transform.localPosition = Vector3.Lerp(currentPos, targetPos, modeChangeSpeed * Time.deltaTime);

        Quaternion currentRot = mainCamera.transform.localRotation;
        Quaternion targetRot = Quaternion.Euler(cameraRotationOffset.x, cameraRotationOffset.y, cameraRotationOffset.z);
        mainCamera.transform.localRotation = Quaternion.Lerp(currentRot, targetRot, modeChangeSpeed * Time.deltaTime);

        //Check for camera collision and move the camera
        Vector3 cameraCurrentPos = mainCamera.transform.position;
        Vector3 cameraTargetPos;
        var dist = Vector3.Distance(mainCamera.transform.position, transform.position);
        var dir = mainCamera.transform.position - transform.position;
        if (Physics.Raycast(transform.position, dir, out RaycastHit cameraHit,dist, cameraCollisionLayer))
        {
            cameraTargetPos = cameraHit.point;
            mainCamera.transform.position = Vector3.Lerp(cameraCurrentPos, cameraTargetPos, followSpeed * Time.deltaTime);
        }
    }

    private void GenerateCameraRecoil()
    {
        remainingTime = recoilTime;
        xRotation += recoilOffset.x;
        yRotation += recoilOffset.y;
        currentRotation = Vector3.zero;
        targetRotation = new Vector3(-recoil.y, UnityEngine.Random.Range(-recoil.x, recoil.x), 0f);
    }

    private void HandleCameraRecoil()
    {
        if (remainingTime > 0)
        {
            var recoilY = Mathf.LerpAngle(currentRotation.y, targetRotation.y, 1 - remainingTime / recoilTime);
            var recoilX = Mathf.LerpAngle(currentRotation.x, targetRotation.x, 1 - remainingTime / recoilTime);
            recoilOffset = new Vector3(recoilX, recoilY, 0f);
            remainingTime -= Time.deltaTime;
        }
        else
        {
            recoilOffset = Vector3.Slerp(recoilOffset, Vector3.zero, returnSpeed * Time.deltaTime);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(playerOrientation.position + offsetDir, 0.05f * Vector3.one);
    }
}
