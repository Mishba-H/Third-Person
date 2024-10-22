using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedCombat : MonoBehaviour
{
    public event EventHandler OnShoot;

    internal bool isAiming;
    internal bool isFiring;
    internal bool isReloading;
    internal float lastFireTime;
    private int bulletsInMag;
    private Vector3 fireDir;

    internal GunSO currentGunSO;
    private GameObject currentGunInstance;
    private int damage;
    private float knockback;

    private float fireInterval;
    private int magSize;
    private float reloadTime;
    private Transform firePoint;
    private float bulletSpeed;
    private float missDistance;
    private TrailRenderer trailRenderer;

    private Vector3 targetPosition;
    private Vector3 targetRotation;
    private Vector3 currentPosition;
    private Vector3 currentRotation;
    private float snappiness;
    private float returnSpeed;
    private float recoilX;
    private float recoilY;
    private float recoilZ;
    private float kickbackX;
    private float kickbackY;
    private float kickbackZ;

    Player player;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Start()
    {
        lastFireTime = 0;

        player.playerRig.OnWeaponChange += PlayerRig_OnWeaponChange;
        OnShoot += RangedCombat_OnShoot;
    }

    private void LateUpdate()
    {
        if (player.currentStance != Player.Stance.Pistol && player.currentStance != Player.Stance.Rifle) return;

        HandleAiming();
        HandleReloading();
        HandleFiring();
        HandleWeaponRecoil();
    }

    private void PlayerRig_OnWeaponChange(object sender, PlayerRig.OnWeaponChangeEventArgs e)
    {
        if (e.gunSO != null)
        {
            currentGunSO = e.gunSO;
            currentGunInstance = e.currentGunInstance;
            AssignGunProperties();
        }
        else
        {
            currentGunSO = null;
            currentGunInstance = null;
        }
    }

    private void AssignGunProperties()
    {
        if (player.currentStance != Player.Stance.Pistol && player.currentStance != Player.Stance.Rifle) return;

        damage = currentGunSO.gunDamage;
        knockback = currentGunSO.knockback;
        fireInterval = 1 / currentGunSO.fireRate;
        magSize = currentGunSO.magSize;
        reloadTime = currentGunSO.reloadTime;
        firePoint = currentGunInstance.transform.Find("FirePoint");
        bulletSpeed = currentGunSO.bulletSpeed;
        missDistance = currentGunSO.maxShootDistance;
        trailRenderer = currentGunSO.bulletTrail;

        snappiness = currentGunSO.snappiness;
        returnSpeed = currentGunSO.returnSpeed;
        recoilX = currentGunSO.recoilX;
        recoilY = currentGunSO.recoilY;
        recoilZ = currentGunSO.recoilZ;
        kickbackX = currentGunSO.kickbackX;
        kickbackY = currentGunSO.kickbackY;
        kickbackZ = currentGunSO.kickbackZ;
    }

    private void HandleAiming()
    {
        if (GameInput.instance.isAimPressed && !isReloading)
        {
            isAiming = true;
        }
        else
        {
            isAiming = false;
        }
    }

    private void HandleReloading()
    {
        if (isReloading) return;

        if (bulletsInMag == 0)
        {
            StartCoroutine(Reload());
        }
        if (GameInput.instance.isReloadPressed && bulletsInMag != magSize)
        {
            StartCoroutine(Reload());
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        bulletsInMag = magSize;
        isReloading = false;
    }

    private void HandleFiring()
    {
        if (GameInput.instance.isFirePressed && !isReloading)
        {
            isFiring = true;
            if (Time.time - lastFireTime > fireInterval && bulletsInMag > 0)
            {
                lastFireTime = Time.time;
                bulletsInMag--;
                Vector3 screenCenterPoint = GameInput.instance.GetScreenCenterWorldPoint();
                Vector3 aimPoint = player.playerCollision.GetAimPoint();
                fireDir = (aimPoint - screenCenterPoint).normalized;
                TrailRenderer newTrail = Instantiate(trailRenderer);
                if (Physics.Raycast(screenCenterPoint, fireDir, out RaycastHit hit, float.MaxValue,
                    player.playerCollision.aimLayer))
                {
                    StartCoroutine(PlayBulletTrail(newTrail, firePoint.position, hit.point, hit));
                }
                else
                {
                    StartCoroutine(PlayBulletTrail(newTrail, firePoint.position, firePoint.position + fireDir * missDistance, hit));
                }

                OnShoot?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            isFiring = false;
        }
    }

    private IEnumerator PlayBulletTrail(TrailRenderer bulletTrail, Vector3 startPoint, Vector3 endPoint, RaycastHit hit)
    {
        bulletTrail.transform.position = startPoint;
        yield return null;
        bulletTrail.emitting = true;
        float distance = Vector3.Distance(startPoint, endPoint);
        float remainingDistance = distance;
        while (remainingDistance > 0)
        {
            bulletTrail.transform.position = Vector3.Lerp(startPoint, endPoint, 1 - remainingDistance / distance);
            remainingDistance -= bulletSpeed * Time.deltaTime;
            yield return null;
        }
        bulletTrail.transform.position = endPoint;
        Destroy(bulletTrail.gameObject);
        if (hit.collider != null)
        {
            OnBulletHit(hit);
        }
    }

    private void HandleWeaponRecoil()
    {
        targetPosition = Vector3.Lerp(targetPosition, Vector3.zero, returnSpeed * Time.deltaTime);
        currentPosition = Vector3.Lerp(currentPosition, targetPosition, snappiness * Time.deltaTime);
        currentGunInstance.transform.localPosition = currentPosition;

        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Lerp(currentRotation, targetRotation, snappiness * Time.deltaTime);
        currentGunInstance.transform.localRotation = Quaternion.Euler(currentRotation);
    }

    private void RangedCombat_OnShoot(object sender, EventArgs e)
    {
        targetPosition += new Vector3(UnityEngine.Random.Range(-kickbackX, kickbackX),
            UnityEngine.Random.Range(0, kickbackY),
            -kickbackZ);
        targetRotation += new Vector3(-recoilX, 
            UnityEngine.Random.Range(-recoilY, recoilY),
            UnityEngine.Random.Range(-recoilZ, recoilZ));
    }

    private void OnBulletHit(RaycastHit hitObject)
    {
        if (hitObject.transform.TryGetComponent(out IHasHealth hasHealth))
        {
            hasHealth.TakeDamage(damage, gameObject);
        }
        if (hitObject.transform.TryGetComponent(out IHasRigidbody hasRigidbody))
        {
            hasRigidbody.Push(fireDir, knockback);
        }
    }
}
