using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class WeaponSO : ScriptableObject
{
    public enum WeaponType
    {
        Sword,
        LongSword,
        GreatSword,
        DualBlades
    }

    public string weaponName;
    public GameObject weaponPrefab;
    public WeaponType weaponType;
    public int baseDamage;
    public float attackRange;
    public ComboSO[] comboSOs;
}
