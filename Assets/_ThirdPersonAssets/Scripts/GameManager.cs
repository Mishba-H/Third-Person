using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField][Range(-1, 60)] private int targetFrameRate;
    [SerializeField][Range(0, 3)] private float weaponWheelTimeScale;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            Debug.LogError("Multiple instances of GameInput found");
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        UIManager.instance.OnWeaponWheelEnable += UIManager_OnWeaponWheelEnable;
        UIManager.instance.OnWeaponWheelDisable += UIMananger_OnWeaponWheelDisable;
    }

    private void Update()
    {
        Application.targetFrameRate = targetFrameRate;
    }

    private void UIManager_OnWeaponWheelEnable(object sender, System.EventArgs e)
    {
        Time.timeScale = weaponWheelTimeScale;
    }

    private void UIMananger_OnWeaponWheelDisable(object sender, System.EventArgs e)
    {
        Time.timeScale = 1;
    }
}
