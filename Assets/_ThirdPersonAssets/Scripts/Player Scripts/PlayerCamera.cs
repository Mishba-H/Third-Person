using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] Player player;

    public CameraMode currentMode;

    public GameObject basicCinemachine;
    public GameObject combatCinemachine;
    public GameObject topDownCinemachine;

    private CameraMode lastMode;

    public enum CameraMode
    {
        Basic,
        Combat,
        TopDown
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SwitchCameraMode(currentMode);
        lastMode = currentMode;
    }

    private void Update()
    {
        if (lastMode != currentMode)
        {
            SwitchCameraMode(currentMode);
            lastMode = currentMode;
        }
    }

    internal void SwitchCameraMode(CameraMode newMode)
    {
        currentMode = newMode;

        basicCinemachine.SetActive(false);
        combatCinemachine.SetActive(false);
        topDownCinemachine.SetActive(false);

        if (newMode == CameraMode.Basic) { basicCinemachine.SetActive(true); }
        if (newMode == CameraMode.Combat) { combatCinemachine.SetActive(true); }
        if (newMode == CameraMode.TopDown) {  topDownCinemachine.SetActive(true);}
    }
}
