using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MeleeCombat : MonoBehaviour
{
    public event EventHandler<OnPlayerAttackEventArgs> OnPlayerAttack;
    public class OnPlayerAttackEventArgs : EventArgs
    {
        public AttackSO attackSO;
    }

    internal bool isBlocking;

    [SerializeField] private float snapToTargetSpeed;
    [SerializeField] private float selectDistance;
    [SerializeField] private float selectHeight;
    [SerializeField][Range(20, 180)] private int selectAngle;
    private int noOfRays;
    private GameObject currentTarget;
    internal Vector3 attackDir;

    private GameObject[] currentGlovesAndBootsInstance;
    private GlovesAndBootsSO currentGlovesAndBootsSO;
    private GameObject currentWeaponInstance;
    private WeaponSO currentWeaponSO;
    private int baseDamage;
    private float attackRange;
    private ComboSO[] currentComboSOs;

    private GameObject currentShieldInstance;
    private ShieldSO currentShieldSO;
    private float blockingPower;

    [SerializeField] private AttackSO[] allAttackSOs;
    [SerializeField] private float comboInterval; //Interval between two combos
    [SerializeField] private float comboEndTime; //Time it takes for combo to be broken when not attacking

    public bool canAttack;
    private int comboIndex;
    private int attackIndex;
    private AttackSO[] currentCombo;
    private AttackSO currentAttackSO;
    private AttackSO[] attacksPerformed;
    private float lastComboBreakTime;
    private float lastAttackTime;

    Player player;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Start()
    {
        player.OnPlayerStanceChanged += Player_OnPlayerStanceChanged;
        player.playerRig.OnWeaponChange += PlayerRig_OnWeaponChange;

        canAttack = true;
        comboIndex = 0;
        attackIndex = 0;
        attacksPerformed = new AttackSO[allAttackSOs.Length];

        GetAttackClipLengths();

        selectAngle -= selectAngle % 5;
        noOfRays = selectAngle / 5 + 1;

        OnPlayerAttack += MeleeCombat_OnPlayerAttack;
    }

    private void Update()
    {
        SnapToTarget();

        HandleBlocking();
        HandlePlayerCanAttack();
        HandleMeleeCombat();
        EndCombo();
    }

    private void Player_OnPlayerStanceChanged(object sender, EventArgs e)
    {
    }

    private void PlayerRig_OnWeaponChange(object sender, PlayerRig.OnWeaponChangeEventArgs e)
    {
        if (e.currentGlovesAndBootsSO != null)
        {
            currentGlovesAndBootsSO = e.currentGlovesAndBootsSO;
            currentGlovesAndBootsInstance = e.currentGlovesAndBootsInstance;
        }
        else
        {
            currentGlovesAndBootsSO = null;
            currentGlovesAndBootsInstance = null;
        }
        if (e.weaponSO != null)
        {
            currentWeaponSO = e.weaponSO;
            currentWeaponInstance = e.currentWeaponInstance;
        }
        else
        {
            currentWeaponSO = null;
            currentWeaponInstance = null;
        }
        if (e.shieldSO != null)
        {
            currentShieldSO = e.shieldSO;
            currentShieldInstance = e.currentShieldInstance;
        }
        else
        {
            currentShieldSO = null;
            currentShieldInstance = null;
        }
        AssignWeaponProperties();
    }

    private void AssignWeaponProperties()
    {
        if (currentGlovesAndBootsSO != null)
        {
            baseDamage = currentGlovesAndBootsSO.baseDamage;
            attackRange = currentGlovesAndBootsSO.attackRange;
            currentComboSOs = currentGlovesAndBootsSO.comboSOs;
        }
        if (currentWeaponSO != null)
        {
            baseDamage = currentWeaponSO.baseDamage;
            attackRange = currentWeaponSO.attackRange;
            currentComboSOs = currentWeaponSO.comboSOs;
        }
        if (currentShieldSO != null)
        {
            blockingPower = currentShieldSO.blockingPower;
        }
    }

    private void MeleeCombat_OnPlayerAttack(object sender, OnPlayerAttackEventArgs e)
    {
        SelectTarget();
    }

    private void GetAttackClipLengths()
    {
        AnimationClip[] clips = player.anim.runtimeAnimatorController.animationClips;
        foreach (AttackSO attackSO in allAttackSOs)
        {
            foreach (AnimationClip clip in clips)
            {
                if (clip.name.Equals(attackSO.attackName))
                {
                    attackSO.clipLength = clip.length;
                    break;
                }
            }
        }
    }

    private void SelectTarget()
    {
        for (int i = 0; i < noOfRays; i++)
        {
            var index = i % 2 == 0 ? i / 2 : -(i / 2 + 1);
            var moveDir = player.playerMovement.moveDir == Vector3.zero ? transform.forward : player.playerMovement.moveDir;
            Vector3 dir = Quaternion.AngleAxis(index * 5, Vector3.up) * moveDir;
            Vector3 from = transform.position + Vector3.up * selectHeight;
            if (Physics.Raycast(from, dir, out RaycastHit targetHit, selectDistance, player.playerCollision.actionLayer))
            {
                currentTarget = targetHit.transform.gameObject;
                attackDir = currentTarget.transform.position - transform.position;
                attackDir = (new Vector3(attackDir.x, 0f, attackDir.z)).normalized;
                break;
            }
            else
            {
                currentTarget = null;
                attackDir = player.playerMovement.moveDir == Vector3.zero ? transform.forward : player.playerMovement.moveDir;
            }
        }
    }

    private void SnapToTarget()
    {
        if (player.currentAction == Player.Action.Attacking && currentTarget != null)
        {
            var targetPos = new Vector3(currentTarget.transform.position.x,
                transform.position.y, currentTarget.transform.position.z) - attackDir * attackRange;
            transform.position = Vector3.Lerp(transform.position, targetPos, snapToTargetSpeed * Time.deltaTime);
        }

        if (player.currentAction == Player.Action.Attacking)
        {
            transform.forward = Vector3.Slerp(transform.forward, attackDir, snapToTargetSpeed * Time.deltaTime);
        }
    }

    internal IEnumerator ApplyAttackImpulse()
    {
        if (currentTarget == null)
        {
            var impulse = currentAttackSO.impulse;
            var impulseTime = currentAttackSO.impulseTime;
            var remainingTime = impulseTime;
            Vector3 initialPos = transform.position;
            Vector3 targetPos = transform.position + attackDir * impulse;
            while (remainingTime > 0f)
            {
                transform.position = Vector3.Lerp(initialPos, targetPos, 1 - remainingTime / impulseTime);
                remainingTime -= Time.deltaTime;
                yield return null;
            }
        }
    }

    private void HandleBlocking()
    {
        if (!player.enableMeleeCombat) return;

        if (player.currentStance != Player.Stance.SwordAndShield && player.currentStance != Player.Stance.Axe) return;

        if (GameInput.instance.isBlockPressed)
        {
            isBlocking = true;
        }
        else
        {
            isBlocking = false;
        }
    }

    private void HandleMeleeCombat()
    {
        if (!player.enableMeleeCombat) return;
        if (!canAttack) return;

        if (player.currentStance != Player.Stance.Base && player.currentStance != Player.Stance.SwordAndShield &&
            player.currentStance != Player.Stance.Axe) return;

        if (GameInput.instance.isAttackTapped && canAttack)
        {
            //Light Attack was pressed
            SelectCombo();
            
        }
        else if (GameInput.instance.isAttackHeld)
        {
            //Light Attack was held
        }
        else if (GameInput.instance.isAttackAltTapped)
        {
            //Heavy Attack was pressed
            SelectCombo();
        }
        else if (GameInput.instance.isAttackAltHeld)
        {
            //Heavy Attack was held
        }
    }

    private void SelectCombo()
    {
        bool comboNotFound = false;
        for (int i = 0; i < currentComboSOs.Length; i++)
        {
            currentCombo = currentComboSOs[i].attackSOs;
            comboIndex = i;
            if (currentCombo.Length <= attackIndex)
            {
                comboNotFound = true;
                continue;
            }
            //Match previously performed attacks with current combo
            bool attacksDidNotMatch = false;
            for (int j = 0; j < attackIndex; j++)
            {
                if (currentCombo[j] != attacksPerformed[j])
                {
                    attacksDidNotMatch = true;
                    comboNotFound = true;
                    break;
                }
            }
            if (attacksDidNotMatch)
            {
                continue;
            }
            //If an attack was selected from the combo update attackIndex and return
            if (player.currentState == Player.State.Sprint)
            {
                if (SelectRunningAttack())
                {
                    lastAttackTime = Time.time;
                    canAttack = false;
                    attackIndex++;
                    return;
                }
            }
            else if (SelectAttack())
            {
                lastAttackTime = Time.time;
                canAttack = false ;
                attackIndex++;
                if (IsComboFinished())
                {
                    attackIndex = 0;
                }
                return;
            }
        }
        //If combo was not found, reset attackIndex and start a new combo
        if (comboNotFound)
        {
            attackIndex = 0;
            //Debug.Log("combo broken");
            SelectCombo();
        }
    }

    private bool IsComboFinished()
    {
        if (attackIndex == currentCombo.Length)
        {
            //Debug.Log("combo finished");
            lastComboBreakTime = Time.time;
            return true;
        }
        return false;
    }

    private bool SelectAttack()
    {
        currentAttackSO = currentCombo[attackIndex];

        //Select light attack
        if (GameInput.instance.isAttackTapped && currentAttackSO.attackType == AttackSO.AttackType.Light)
        {
            //Debug.Log(currentComboSOs[comboIndex].name + ", " + currentAttackSO.name + ", light, " + attackIndex);
            attacksPerformed[attackIndex] = currentAttackSO;
            OnPlayerAttack?.Invoke(this, new OnPlayerAttackEventArgs { attackSO = currentAttackSO});
            return true;
        }
        //Select heavy attack
        if (GameInput.instance.isAttackAltTapped && currentAttackSO.attackType == AttackSO.AttackType.Heavy)
        {
            //Debug.Log(currentComboSOs[comboIndex].name + ", " + currentAttackSO.name + ", heavy, " + attackIndex);
            attacksPerformed[attackIndex] = currentAttackSO;
            OnPlayerAttack?.Invoke(this, new OnPlayerAttackEventArgs { attackSO = currentAttackSO });
            return true;
        }
        //Current combo did not have the required attack type
        else
        {
            return false;
        }
    }
    private bool SelectRunningAttack()
    {
        currentAttackSO = currentCombo[attackIndex];

        //Select light attack
        if (GameInput.instance.isAttackTapped && currentAttackSO.attackType == AttackSO.AttackType.RunningLight)
        {
            //Debug.Log(currentComboSOs[comboIndex].name + ", " + currentAttackSO.name + ", light, " + attackIndex);
            attacksPerformed[attackIndex] = currentAttackSO;
            OnPlayerAttack?.Invoke(this, new OnPlayerAttackEventArgs { attackSO = currentAttackSO });
            return true;
        }
        //Select heavy attack
        if (GameInput.instance.isAttackAltTapped && currentAttackSO.attackType == AttackSO.AttackType.RunningHeavy)
        {
            //Debug.Log(currentComboSOs[comboIndex].name + ", " + currentAttackSO.name + ", heavy, " + attackIndex);
            attacksPerformed[attackIndex] = currentAttackSO;
            OnPlayerAttack?.Invoke(this, new OnPlayerAttackEventArgs { attackSO = currentAttackSO });
            return true;
        }
        //Current combo did not have the required attack type
        else
        {
            return false;
        }
    }

    private void HandlePlayerCanAttack()
    {
        if (currentAttackSO == null) return;

        if (Time.time - lastAttackTime > currentAttackSO.clipLength)
        {
            canAttack = true;
            if (currentWeaponInstance != null)
            {
                currentWeaponInstance.GetComponentInChildren<Collider>().enabled = false;
            }
            if (currentGlovesAndBootsInstance != null)
            {
                foreach(GameObject gameObject in currentGlovesAndBootsInstance)
                {
                    gameObject.GetComponentInChildren<Collider>().enabled = false;
                }
            }
        }
        else
        {
            if (currentWeaponInstance != null)
            {
                currentWeaponInstance.GetComponentInChildren<Collider>().enabled = true;
            }
            if (currentGlovesAndBootsInstance != null)
            {
                foreach (GameObject gameObject in currentGlovesAndBootsInstance)
                {
                    gameObject.GetComponentInChildren<Collider>().enabled = true;
                }
            }
        }
        if (Time.time - lastComboBreakTime < currentAttackSO.clipLength + comboInterval)
        {
            canAttack = false;
            player.enableMovement = false;
        }
        else
        {
            player.enableMovement = true;
        }
    }

    private void EndCombo()
    {
        if (currentAttackSO == null) return;
        //Combo broken
        if (Time.time - lastAttackTime > comboEndTime + currentAttackSO.clipLength)
        {
            attackIndex = 0;
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.TryGetComponent(out IHasHealth hasHealth))
        {
            int damage = (int)(baseDamage * currentAttackSO.damageMultiplier);
            hasHealth.TakeDamage(damage, gameObject);
        }
        if (collider.TryGetComponent(out IHasRigidbody hasRigidbody))
        {
            hasRigidbody.Push(attackDir, currentAttackSO.knockback);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        selectAngle -= selectAngle % 5;
        noOfRays = selectAngle / 5 + 1;
        /*if (Application.isPlaying)
        {
            for (int i = 0; i < noOfRays; i++)
            {
                var index = i % 2 == 0 ? i / 2 : -(i / 2 + 1); 
                var moveDir = player.playerMovement.moveDir == Vector3.zero ? transform.forward : player.playerMovement.moveDir;
                Vector3 dir = Quaternion.AngleAxis(index * 5, Vector3.up) * moveDir;
                Vector3 from = transform.position + Vector3.up * selectHeight;
                Vector3 to = transform.position + Vector3.up * selectHeight + dir.normalized * selectDistance;
                Gizmos.DrawLine(from, to);
            }
        }*/
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + attackDir * attackRange);
    }
}
