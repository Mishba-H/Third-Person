using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerRig : MonoBehaviour
{
    public event EventHandler<OnWeaponChangeEventArgs> OnWeaponChange;
    public class OnWeaponChangeEventArgs : EventArgs
    {
        public GameObject[] currentGlovesAndBootsInstance = null;
        public GameObject currentWeaponInstance = null;
        public GameObject currentShieldInstance = null;
        public GameObject currentGunInstance = null;
        public GlovesAndBootsSO currentGlovesAndBootsSO = null;
        public WeaponSO weaponSO = null;
        public ShieldSO shieldSO = null;
        public GunSO gunSO = null;
    }

    #region BONE_POSITIONS
    [SerializeField] private Transform rightHandTransform;
    [SerializeField] private Transform rightForearmTransform;
    [SerializeField] private Transform leftHandTransform;
    [SerializeField] private Transform leftForearmTransform;
    [SerializeField] private Transform rightLegTransform;
    [SerializeField] private Transform leftLegTransform;
    #endregion

    #region WEAPON_REFERENCES
    [SerializeField] private GlovesAndBootsSO[] glovesAndBootsSOs;
    [SerializeField] private WeaponSO[] weaponSOs;
    [SerializeField] private ShieldSO[] shieldSOs;
    [SerializeField] private GunSO[] pistolSOs;
    [SerializeField] private GunSO[] rifleSOs;
    #endregion

    #region CONSTRAINTS_&_TARGETS
    [SerializeField] private MultiAimConstraint spineConstraint;
    [SerializeField] private MultiAimConstraint chestConstraint;
    [SerializeField] private MultiAimConstraint headConstraint;
    [SerializeField] private TwoBoneIKConstraint rightArmConstraint;
    [SerializeField] private TwoBoneIKConstraint leftArmConstraint;
    [SerializeField] private Rig weaponRig;
    [SerializeField] private Transform lookTarget;
    [SerializeField] private Transform rightHandTarget;
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform pistolAimTransform;
    [SerializeField] private Transform pistolIdleTransform;
    [SerializeField] private Transform rifleAimTransform;
    [SerializeField] private Transform rifleHipFireTransform;
    [SerializeField] private Transform rifleIdleTransform;
    #endregion

    private GameObject[] lastInstantiated;
    private Transform rightHandPos;
    private Transform leftHandPos;

    Player player;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Start()
    {
        player.OnPlayerStanceChanged += Player_OnPlayerStanceChanged;

        lastInstantiated = new GameObject[4];
    }

    private void LateUpdate()
    {
        MoveLookTarget();
        HandleWeaponPosition();
        HandleRig();
        AttachHandsToWeapon();
    }

    private void Player_OnPlayerStanceChanged(object sender, EventArgs e)
    {
        foreach (GameObject gameObject in lastInstantiated)
        {
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }

        if (player.currentStance == Player.Stance.Base)
        {
            lastInstantiated[0] = Instantiate(glovesAndBootsSOs[0].gloveR, rightHandTransform);
            lastInstantiated[1] = Instantiate(glovesAndBootsSOs[0].gloveL, leftHandTransform);
            lastInstantiated[2] = Instantiate(glovesAndBootsSOs[0].bootR, rightLegTransform);
            lastInstantiated[3] = Instantiate(glovesAndBootsSOs[0].bootL, leftLegTransform);
            OnWeaponChange?.Invoke(this, new OnWeaponChangeEventArgs {
                currentGlovesAndBootsInstance = lastInstantiated,
                currentGlovesAndBootsSO = glovesAndBootsSOs[0]
            });
        }
        else if (player.currentStance == Player.Stance.SwordAndShield)
        {
            lastInstantiated[0] = Instantiate(weaponSOs[0].weaponPrefab, rightHandTransform);
            lastInstantiated[1] = Instantiate(shieldSOs[0].shieldPrefab, leftHandTransform);
            lastInstantiated[0].transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            lastInstantiated[1].transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            OnWeaponChange?.Invoke(this, new OnWeaponChangeEventArgs
            {
                currentWeaponInstance = lastInstantiated[0],
                currentShieldInstance = lastInstantiated[1],
                weaponSO = weaponSOs[0],
                shieldSO = shieldSOs[0]
            });
        }
        else if (player.currentStance == Player.Stance.Axe)
        {
            lastInstantiated[0] = Instantiate(weaponSOs[0].weaponPrefab, rightHandTransform);
            lastInstantiated[0].transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            OnWeaponChange?.Invoke(this, new OnWeaponChangeEventArgs
            {
                currentWeaponInstance = lastInstantiated[0],
                weaponSO = weaponSOs[0],
            });
        }
        else if (player.currentStance == Player.Stance.Pistol)
        {
            lastInstantiated[0] = Instantiate(pistolSOs[0].gunPrefab);
            OnWeaponChange?.Invoke(this, new OnWeaponChangeEventArgs
            {
                currentGunInstance = lastInstantiated[0],
                gunSO = pistolSOs[0]
            });
        }
        else if (player.currentStance == Player.Stance.Rifle)
        {
            lastInstantiated[0] = Instantiate(rifleSOs[0].gunPrefab);
            OnWeaponChange?.Invoke(this, new OnWeaponChangeEventArgs
            {
                currentGunInstance = lastInstantiated[0],
                gunSO = rifleSOs[0]
            });
        }
    }

    private void MoveLookTarget()
    {
        lookTarget.position = player.playerCollision.GetAimPoint();
    }

    private void HandleWeaponPosition()
    {
        if (player.currentStance == Player.Stance.Pistol)
        {
            if (player.rangedCombat.isAiming || player.rangedCombat.isFiring)
            {
                lastInstantiated[0].transform.SetParent(pistolAimTransform);
            }
            else
            {
                lastInstantiated[0].transform.SetParent(pistolIdleTransform);
                lastInstantiated[0].transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }

        if (player.currentStance == Player.Stance.Rifle)
        {
            if (player.rangedCombat.isAiming)
            {
                lastInstantiated[0].transform.SetParent(rifleAimTransform);
            }
            else if (player.rangedCombat.isFiring)
            {
                lastInstantiated[0].transform.SetParent(rifleHipFireTransform);
            }
            else
            {
                lastInstantiated[0].transform.SetParent(rifleIdleTransform);
                lastInstantiated[0].transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }
    }

    private void AttachHandsToWeapon()
    {
        if (player.currentStance == Player.Stance.Rifle || player.currentStance == Player.Stance.Pistol)
        {
            rightHandPos = lastInstantiated[0].transform.Find("RightHandPos").transform;
            leftHandPos = lastInstantiated[0].transform.Find("LeftHandPos").transform;
            rightHandTarget.SetPositionAndRotation(rightHandPos.position, rightHandPos.rotation);
            leftHandTarget.SetPositionAndRotation(leftHandPos.position, leftHandPos.rotation);
        }
    }

    private void HandleRig()
    {
        if (player.rangedCombat.isAiming || player.rangedCombat.isFiring)
        {
            spineConstraint.weight = 1;
            chestConstraint.weight = 1;
            headConstraint.weight = 1;
            weaponRig.weight = 1;
            rightArmConstraint.weight = 1;
            leftArmConstraint.weight = 1;
        }
        else if (player.currentStance == Player.Stance.Rifle || player.currentStance == Player.Stance.Pistol)
        {
            spineConstraint.weight = 0;
            chestConstraint.weight = 0;
            headConstraint.weight = 0;
            weaponRig.weight = 0;
            rightArmConstraint.weight = 1;
            leftArmConstraint.weight = 1;
        }
        else
        {
            spineConstraint.weight = 0;
            chestConstraint.weight = 0;
            headConstraint.weight = 0;
            weaponRig.weight = 0;
            rightArmConstraint.weight = 0;
            leftArmConstraint.weight = 0;
        }
    }
}
