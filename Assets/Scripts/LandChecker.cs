using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandChecker : MonoBehaviour
{

    private GameHandler game;
    private ShipController ship;

    private float timer = 3f;
    private float t = 0;

    private void Awake()
    {
        game = FindObjectOfType<GameHandler>();
        ship = FindObjectOfType<ShipController>();
    }

    // if player stands still for 3 seconds, give points and generate new landing point
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (ship.rb.velocity.magnitude < 1 && ship.isGrounded)
            {
                t += Time.deltaTime;
                if(t >= timer)
                {
                    game.Landed();
                    t = 0;
                }
            }
            else
            {
                t=0;
            }
        }
    }
}
