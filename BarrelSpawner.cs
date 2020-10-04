using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrelSpawner : MonoBehaviour
{
    public GameObject[] weaponBarrels; //Low-teir =    [0 - 2]
                                       //Mid-teir =    [3 - 4]
                                       //High-teir =   [5 - 7]
                                       //--------------------
                                       //Random low =  [-]
                                       //Random mid =  [-]
                                       //Random high = [-]
                                       //True Random = [7]

    [Header("SpawnPoints:")]
    public GameObject upperLeftSpawn;
    public GameObject upperRightSpawn;
    public GameObject middleLeftSpawn;
    public GameObject middleRightSpawn;
    public GameObject throneroomLeftSpawn;
    public GameObject throneroomRightSpawn;

    [Header("Input Factors:")]
    public GameObject sceneCamera;
        private CameraRail cameraRail;
    public GameObject player1;
        private Damager p1Damager;
    public GameObject player2;
        private Damager p2Damager;

    [Header("Control Variables:")]
    public GameObject ulImmediateSpawn; //Starting barrel in ul position
    public GameObject urImmediateSpawn; //Starting barrel in ur position
    public float timeStep1; //How many seconds into the match to start spawning mid-tier barrels in middle
    public int halfwayHealth; //If a player reaches this health value or lower, start spawning powerful throneRoom weapons
    public float upperRecharge; //How long it takes for a new barrel to appear in upper spawn points once taken
    public float midRecharge; //How long it takes for a new barrel to appear in mid spawn points once taken
    public float throneRecharge; //How long it takes for a new barrel to appear in throne room once taken

    //Internal State Variables:
    private float matchTimer; //Keeps track of how long this match has been running
    private float uVacantTime;
    private float mVacantTime;
    private float tVacantTime;
    internal bool ulSpawnOccupied;
    internal bool urSpawnOccupied;
    internal bool mlSpawnOccupied;
    internal bool mrSpawnOccupied;
    internal bool trSpawnOccupied;
    internal bool tlSpawnOccupied;

    void Start()
    {
        //Get Objects and Components:
        cameraRail = sceneCamera.GetComponent<CameraRail>();
            //Players:
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player"); //Get array of both players in scene
            if (players[0].GetComponent<PlayerController>().player == PlayerController.Player.Player1) //Assign players to their appropriate slots
            { player1 = players[0]; player2 = players[1]; }
            else { player1 = players[1]; player2 = players[0]; }
        p1Damager = player1.GetComponent<Damager>();
        p2Damager = player2.GetComponent<Damager>();

        //Spawn Starting Barrels:
        if (ulImmediateSpawn != null) { SpawnBarrel(ulImmediateSpawn, upperLeftSpawn); }
            else { SpawnBarrel(1, 2, upperLeftSpawn); } //If no specific prefab is given, spawn random low-teir barrel
        if (urImmediateSpawn != null) { SpawnBarrel(urImmediateSpawn, upperRightSpawn); }
            else { SpawnBarrel(1, 2, upperRightSpawn); } //If no specific prefab is given, spawn random low-teir barrel
    }

    void Update()
    {
        //CHECK SPAWN OCCUPATION:
        ulSpawnOccupied = upperLeftSpawn.GetComponent<SpawnController>().spawnOccupied;
        urSpawnOccupied = upperRightSpawn.GetComponent<SpawnController>().spawnOccupied;
        mlSpawnOccupied = middleLeftSpawn.GetComponent<SpawnController>().spawnOccupied;
        mrSpawnOccupied = middleRightSpawn.GetComponent<SpawnController>().spawnOccupied;
        tlSpawnOccupied = throneroomLeftSpawn.GetComponent<SpawnController>().spawnOccupied;
        trSpawnOccupied = throneroomRightSpawn.GetComponent<SpawnController>().spawnOccupied;

        //UPDATE TIMERS:
        if (cameraRail.titleState == CameraRail.TitleState.Play)
        {
            matchTimer += Time.deltaTime;
            if (ulSpawnOccupied == true && urSpawnOccupied == true) { uVacantTime = 0; } else { uVacantTime += Time.deltaTime; }
            if (mlSpawnOccupied == true && mrSpawnOccupied == true) { mVacantTime = 0; } else { mVacantTime += Time.deltaTime; }
            if (tlSpawnOccupied == false && trSpawnOccupied == false) { tVacantTime += Time.deltaTime; } else { tVacantTime = 0; }
        }

        //UPPER-SPAWNPOINT ACTIVATION:
        if (cameraRail.titleState == CameraRail.TitleState.Play) //Always spawning these when in Play phase
        {
            if (uVacantTime > upperRecharge) //If both spawnpoints have been vacant for long enough
            {
                if (ulSpawnOccupied == false && urSpawnOccupied == false) //If both spawns are empty, pick one at random
                {
                    int randomChoice = Random.Range(0, 2);
                    if (randomChoice == 0)         { SpawnBarrel(1, 2, upperLeftSpawn); }
                    else                           { SpawnBarrel(1, 2, upperRightSpawn); }
                }
                else if (ulSpawnOccupied == false) { SpawnBarrel(1, 2, upperLeftSpawn); }
                else if (urSpawnOccupied == false) { SpawnBarrel(1, 2, upperRightSpawn); }
                uVacantTime = 0;
            }
        }

        //MID-SPAWNPOINT ACTIVATION:
        if (matchTimer >= timeStep1) //Once enough time has passed for middle barrels to start spawning
        {
            if (mVacantTime > midRecharge) //If both spawnpoints have been vacant for long enough
            {
                if (mlSpawnOccupied == false && mrSpawnOccupied == false) //If both spawns are empty, pick one at random
                {
                    int randomChoice = Random.Range(0, 2);
                    if (randomChoice == 0)         { SpawnBarrel(3, 4, middleLeftSpawn); }
                    else                           { SpawnBarrel(3, 4, middleRightSpawn); }
                }
                else if (mlSpawnOccupied == false) { SpawnBarrel(3, 4, middleLeftSpawn); }
                else if (mrSpawnOccupied == false) { SpawnBarrel(3, 4, middleRightSpawn); }
                mVacantTime = 0;
            }
        }

        //THRONE-SPAWNPOINT ACTIVATION:
        if (p1Damager.playerHealth <= halfwayHealth || p2Damager.playerHealth <= halfwayHealth)
        {
            if (tVacantTime > throneRecharge) //If both spawnpoints have been vacant for long enough
            {
                if (tlSpawnOccupied == false && trSpawnOccupied == false) //If both spawns are empty, pick one at random
                {
                    int randomChoice = Random.Range(0, 2);
                    if (randomChoice == 0) { SpawnBarrel(5, 7, throneroomLeftSpawn); }
                    else { SpawnBarrel(5, 7, throneroomRightSpawn); }
                }
                else if (tlSpawnOccupied == false) { SpawnBarrel(5, 7, throneroomLeftSpawn); }
                else if (trSpawnOccupied == false) { SpawnBarrel(5, 7, throneroomRightSpawn); }
                tVacantTime = 0;
            }
        }

    }

    public void SpawnBarrel(GameObject prefab, GameObject spawnPoint)
    {
        //Spawns indicated barrel at indicated spawnPoint
        if (spawnPoint.GetComponent<SpawnController>().spawnOccupied == false) //Make sure spawn isn't occupied first
        {
            GameObject newBarrel = Instantiate(prefab, spawnPoint.transform); //Instantiate designated barrel and child it to spawnPoint
            newBarrel.transform.position = spawnPoint.transform.position; //Set position of barrel to spawnPoint
        }
    }
    public void SpawnBarrel(int min, int max, GameObject spawnPoint)
    {
        //Overload that spawns a random barrel within given quality range
        if (spawnPoint.GetComponent<SpawnController>().spawnOccupied == false) //Make sure spawn isn't occupied first
        {
            GameObject newBarrel = Instantiate(weaponBarrels[Random.Range(min, max + 1)], spawnPoint.transform); //Instantiate designated barrel and child it to spawnPoint
            newBarrel.transform.position = spawnPoint.transform.position; //Set position of barrel to spawnPoint
        }
    }
}
