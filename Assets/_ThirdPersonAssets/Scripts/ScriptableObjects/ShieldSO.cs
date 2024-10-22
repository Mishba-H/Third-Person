using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ShieldSO : ScriptableObject
{
    public string shieldName;
    public GameObject shieldPrefab;
    public float blockingPower;
}
