using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class P2Selectingscript : MonoBehaviour
{
    public enum Player { Player1, Player2 }; //Creates an enum for establishing which player an object is on
    public Player player; //Allows player number to be set in inspector
    public GameObject[] skinPrefabs;
    internal GameObject currentSkin; //The current skin player has chosen
    private int skinCounter;
    private SpriteRenderer sr;
    private Menu menu;
    private bool nerdLocked; //Locks skin selection based on readyUp state

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        menu = GameObject.Find("Canvas").GetComponent<Menu>();
    }


    void Update()
    {
        //Update Nerdlocked
        if (player == Player.Player1) { nerdLocked = menu.nerd; }
        else { nerdLocked = menu.nerd2; }

        if (Input.GetButtonDown(PlayerInputs(player, "Right")) == true && nerdLocked == false)
        {
            skinCounter++; //Increment skinCounter
            if (skinCounter > skinPrefabs.Length - 1) { skinCounter = 0; } //Overflow and wrap around if needed
        }
        else if (Input.GetButtonDown(PlayerInputs(player, "Left")) == true && nerdLocked == false)
        {
            skinCounter--; //Increment skinCounter
            if (skinCounter <= -1) { skinCounter = skinPrefabs.Length - 1; } //Underflow and wrap around if needed
        }

        sr.sprite = skinPrefabs[skinCounter].GetComponent<SpriteRenderer>().sprite; //Get sprite from skin prefab and assign to skin standin
        currentSkin = skinPrefabs[skinCounter]; //Set currentSkin to displayed skin (for sceneLoader)
    }

    public string PlayerInputs(Player player, string input)
    {
        /* Method for storing and easily changing player inputs
         * There will be one of these methods local to each player, so be careful not to set two players to same input scheme
         */

        string playerSet = "P1"; //Addendum at the beginning of every control, depends on player (default is Player1)
        if (player == Player.Player1) { playerSet = "P1"; }      //Set all controls to P1
        else if (player == Player.Player2) { playerSet = "P2"; } //Set all controls to P2
        string useInput = playerSet + input; //Build input name based on request parameters
        return (useInput); //Return constructed input name
    }

}
