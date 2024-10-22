using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class GlovesAndBootsSO : ScriptableObject
{
    public string setName;
    public GameObject gloveR;
    public GameObject gloveL;
    public GameObject bootR;
    public GameObject bootL;
    public int baseDamage;
    public float attackRange;
    public ComboSO[] comboSOs;
}
