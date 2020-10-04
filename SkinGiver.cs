using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinGiver : MonoBehaviour
{
    //Objects and Components:
    public GameObject[] skins; //Array of all skins which can be equipped on player
    public GameObject p1StartingSkin; //Starting skin to assign player 1
    public GameObject p2StartingSkin; //Starting skin to assign player 2
    [Space()]
    public GameObject player1; //Player1 gameObject
    public GameObject player2; //Player2 gameObject

    //Internal Tracker Variables:
    internal int p1Skindex = 0; //Player 1 current skin array index number
    internal int p2Skindex = 1; //Player 2 current skin array index number

    void Start()
    {
        //Get Objects and Components:
            //Players:
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player"); //Get array of both players in scene
            if (players[0].GetComponent<PlayerController>().player == PlayerController.Player.Player1) //Assign players to their appropriate slots
            { player1 = players[0]; player2 = players[1]; }
            else { player1 = players[1]; player2 = players[0]; }
        //Pre-Emptively Assign Skins:
        if (p1StartingSkin != null) { AssignSkin(player1, p1StartingSkin); } //Assign designated start skin if specified
        else                        { AssignSkin(player1, skins[0]); }       //If not specified, assign default skin
        if (p2StartingSkin != null) { AssignSkin(player2, p2StartingSkin); } //Assign designated start skin if specified
        else                        { AssignSkin(player2, skins[1]); }       //If not specified, assign default skin
    }

    public void AssignSkin(GameObject player, GameObject skinPrefab)
    {
        /* Assigns the selected player the selected skin prefab, which will automatically do the legwork of attaching itself and
         * integrating with Player parent gameObject.
         */
        
        //Overflow/Underflow
        if (player != null && skinPrefab != null) //If both given elements are valid...
        {
            Instantiate(skinPrefab, player.transform); //Instantiate given skin and child it to given player
        }
        else //If one or both of the given elements is null...
        {
            Debug.Log("AssignSkin was unsuccessful, missing gameObject.");
        }
    }

}
