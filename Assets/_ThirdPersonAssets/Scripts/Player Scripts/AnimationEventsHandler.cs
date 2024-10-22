using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventsHandler : MonoBehaviour
{
    Player player;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
    }

    public void ReadyToAttack()
    {
        if (player != null)
        {
            player.meleeCombat.canAttack = true;
        }
    }

    public void ApplyAttackImpulse()
    {
        StartCoroutine(player.meleeCombat.ApplyAttackImpulse());
    }
}
