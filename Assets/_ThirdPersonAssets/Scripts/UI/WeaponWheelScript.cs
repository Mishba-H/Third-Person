using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

public class WeaponWheelScript : MonoBehaviour
{
    [SerializeField] private Transform[] buttons;
    [SerializeField] private RectTransform weaponWheelButton;

    int buttonCount;
    float angle;
    private int lastButtonSelectedIndex;
    private Vector2 lookDir;

    UIManager uiManager;

    private void Awake()
    {
        uiManager = GetComponentInParent<UIManager>();
    }

    private void Start()
    {
        lookDir = Vector2.zero;
        buttonCount = buttons.Length;
        angle = 360 / buttonCount;
        lastButtonSelectedIndex = 0;
        SetButtonsInACircle();
    }

    private void Update()
    {
        SelectButton();
    }

    private void SetButtonsInACircle()
    {
        for (int i = 0; i < buttonCount; i++)
        {
            buttons[i].localPosition = Vector3.zero;
            buttons[i].localRotation = Quaternion.Euler(0f, 0f, -1 * i * angle);
            int childCount = buttons[i].childCount;
            for(int j = 0; j < childCount; j++) 
            {
                buttons[i].GetChild(j).rotation = Quaternion.identity;
            }
        }
    }

    private void SelectButton()
    {
        if (GameInput.instance.GetInputControlScheme() == "KBM")
        {
            var mousePos = GameInput.instance.GetPointerPositionFromCenter();
            lookDir = mousePos == Vector2.zero ? lookDir : mousePos;
        }
        else if (GameInput.instance.gamepadMode)
        {
            var lookInput = GameInput.instance.GetLookDirection().normalized;
            lookDir = lookInput == Vector2.zero ? lookDir : lookInput;
        }
        else if (GameInput.instance.GetInputControlScheme() == "Touchscreen")
        {
            foreach (TouchControl touch in Touchscreen.current.touches)
            {
                Vector2 startPos = touch.startPosition.ReadValue();
                if (RectTransformUtility.RectangleContainsScreenPoint(weaponWheelButton, startPos))
                {
                    lookDir = touch.position.ReadValue() - startPos;
                }
            }
        }

        float lookAngle = Mathf.Atan2(lookDir.x, lookDir.y) * Mathf.Rad2Deg;
        lookAngle = lookAngle < 0 ? 360 + lookAngle : lookAngle;
        for (int i = 0; i < buttonCount; i++)
        {
            float lowerAngle = (i - 0.5f) * angle;
            float upperAngle = (i + 0.5f) * angle;
            if (lookAngle > lowerAngle && lookAngle < upperAngle)
            {
                if (buttons[i].GetComponent<Button>().IsInteractable())
                {
                    buttons[i].GetComponent<Button>().Select();
                    lastButtonSelectedIndex = i;
                }
            }
        }
    }

    private void OnEnable()
    {
        buttons[lastButtonSelectedIndex].GetComponent<Button>().Select();
    }

    private void OnDisable()
    {
        buttons[lastButtonSelectedIndex].GetComponent<Button>().onClick.Invoke();
    }
}
