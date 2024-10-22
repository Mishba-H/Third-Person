using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class TagSO : ScriptableObject
{
    public enum TagType
    {
        GameManager,
        Player,
        Enemy
    }

    public TagType tag;
}
