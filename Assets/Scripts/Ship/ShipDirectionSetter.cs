using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ShipDirectionSetter : MonoBehaviour
{

    public Transform ship;

    public float initialForce;

    public Vector3 Velocity()
    {
        var dir = transform.up.normalized * initialForce;
        return dir;
    }

    private void Update()
    {
        if(Application.isPlaying)
            return;

        transform.position = ship.position;
        ship.rotation = transform.rotation;
    }
}
