using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class GunSO : ScriptableObject
{
    public enum GunType
    {
        Pistol,
        Rifle,
        MarksmanRifle,
        SniperRifle,
        RocketLauncher
    }

    public enum FireType
    {
        Single,
        SemiAuto,
        FullAuto
    }

    public string gunName;
    public GameObject gunPrefab;
    public GunType gunType;
    public int gunDamage;
    public float knockback;

    public float fireRate;
    public int magSize;
    public float reloadTime;
    public float bulletSpeed;
    public float maxShootDistance;

    public TrailRenderer bulletTrail;

    public float snappiness;
    public float returnSpeed;
    public float recoilX;
    public float recoilY;
    public float recoilZ;
    public float kickbackX;
    public float kickbackY;
    public float kickbackZ;
}
