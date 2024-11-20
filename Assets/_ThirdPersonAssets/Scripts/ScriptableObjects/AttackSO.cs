using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class AttackSO : ScriptableObject
{
    public enum AttackType
    {
        Light,
        Heavy,
        RunningLight,
        RunningHeavy,
        LightHold,
        HeavyHold,
        Air
    }

    public enum DamageType
    {
        Simple,
        Line,
        Area
    }

    public string attackName;
    public float clipLength;
    public AttackType attackType;
    public DamageType damageType;

    public float damageMultiplier;
    public float impulse;
    public float impulseTime;
    public float knockback;
}
