using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnController : MonoBehaviour
{
    public GameObject occupiedBy;
    public bool spawnOccupied;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<BarrelController>() != null)
        { spawnOccupied = true;
          occupiedBy = collision.gameObject;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject == occupiedBy) { spawnOccupied = false; occupiedBy = null; }
    }
}
