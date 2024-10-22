using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class CameraModeSO : ScriptableObject
{
    public CameraScript.CameraMode mode;
    public float mouseSensitivity;
    public float gamepadSensitivity;
    public float touchscreenSensitivity;
    public Vector3 pivotOffset;
    public Vector3 cameraPositionOffset;
    public Vector3 cameraRotationOffset;
    public float topClampAngle;
    public float bottomClampAngle;
}
