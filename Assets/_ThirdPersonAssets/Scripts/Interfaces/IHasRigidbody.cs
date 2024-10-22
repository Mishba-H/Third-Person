using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHasRigidbody
{
    void Push(Vector3 puchDir, float force);
}
