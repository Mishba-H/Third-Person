using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHasHealth
{
    void Heal(int healAmaount);

    void TakeDamage(int damageAmount, GameObject attacker);
}
