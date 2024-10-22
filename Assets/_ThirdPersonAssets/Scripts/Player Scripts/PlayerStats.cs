using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IHasHealth
{
    public event EventHandler OnTakeDamage;

    public event EventHandler OnZeroHealth;

    [SerializeField] private int maxHealth;

    private bool isInvincible;
    internal int currentHealth;

    Player player;

    private void Awake()
    {
        player = GetComponent<Player>();
    }
    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void Heal(int healAmount)
    {
        currentHealth += healAmount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }

    public void TakeDamage(int damageAmount, GameObject attacker)
    {
        if (currentHealth == 0) return;

        if (!isInvincible)
        {
            currentHealth -= damageAmount;
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                OnZeroHealth?.Invoke(this,EventArgs.Empty);
                return;
            }
            OnTakeDamage?.Invoke(this,EventArgs.Empty);
        }
    }
}
