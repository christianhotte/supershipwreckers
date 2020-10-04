using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    /*  This object persists between scenes and carries data such as player skin choices.  It also spawns in objects in levels when
     *  necessary
     */
    
    [Header("Character Selection Data:")] //Data From Character Selection Menu:
    public string targetScene; //The scene this object will cause the menu to load into
    public GameObject player1SelectedSkin; //Player1 skin from character selection
    public GameObject player2SelectedSkin; //Player2 skin from character selection
    [Header("Level Data:")] //Data for loaded level:
    public GameObject player1; //Player1 GameObject in loaded level
    public GameObject player2; //Player2 GameObject in loaded level
    internal bool sceneBuilt; //Tracks whether or not a level has been loaded
    internal bool skinsAssigned; //Tracks whether or not this program has already assigned skins on players

    public void FixedUpdate()
    {
        //Push skins on players until assigned:
        if (skinsAssigned != true && sceneBuilt == true) //Run this continuously while scene is built until skins are assigned
        {
            //Re-check for players in scene, and assign skins if possible
            if (CheckForPlayers() == true)
            {
                AssignSkin(player1, player1SelectedSkin);
                AssignSkin(player2, player2SelectedSkin);
                skinsAssigned = true; //Tell program it no longer has to run this function
            }
        }
    }

    public void BuildScene(string scene)
    {
        //Collects all necessary data from player select screen and builds it into new level

        if (sceneBuilt != true)
        {
            //Collect Data:
            player1SelectedSkin = GameObject.Find("Player1Select").GetComponentInChildren<P2Selectingscript>().currentSkin; //Get player1 skin
            player2SelectedSkin = GameObject.Find("Player2Select").GetComponentInChildren<P2Selectingscript>().currentSkin; //Get player1 skin

            //Load New Scene:
            StartCoroutine(LoadYourAsyncScene(scene)); //Start coroutine that loads new scene, transfers this gameObject to it, then removes prev scene

            //Set Data:
            if (CheckForPlayers() == true) //Do preliminary check for players, and immediately assign skins if possible:
            {
                AssignSkin(player1, player1SelectedSkin);
                AssignSkin(player2, player2SelectedSkin);
                skinsAssigned = true; //Tell program it no longer has to run this function
            }
        }
    }

    public bool CheckForPlayers()
    {
        //Tries to find player gameObjects when in loaded level

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player"); //Get both players in scene and assign them as contents of array
        if (players.Length > 0) //If array is no longer empty (players have been found):
        {
            if (players[0].GetComponent<PlayerController>().player == PlayerController.Player.Player1) //Assign players to their appropriate slots
            { player1 = players[0]; player2 = players[1]; }
            else { player1 = players[1]; player2 = players[0]; }

            Debug.Log(gameObject.ToString() + ": Players found, assigning skins.");
            return true; //Tell program that function was successful
        }
        else //If array is still empty (players have not been found):
        {
            Debug.Log(gameObject.ToString() + ": Players not found, checking next update...");
            return false; //Tell program that function was unsuccessful
        }

    }

    public void AssignSkin(GameObject player, GameObject skinPrefab)
    {
        /* Assigns the selected player the selected skin prefab, which will automatically do the legwork of attaching itself and
         * integrating with Player parent gameObject.
         */
         
        if (player != null && skinPrefab != null) //If both given elements are valid...
        {
            Instantiate(skinPrefab, player.transform); //Instantiate given skin and child it to given player
        }
        else //If one or both of the given elements is null...
        {
            Debug.Log("AssignSkin was unsuccessful, missing gameObject.");
        }
    }

    IEnumerator LoadYourAsyncScene(string scene)
    {
        //Loads target scene, moves this object to that scene, then removes previous scene

            sceneBuilt = true; //Make sure scene only gets built once
            // Set the current Scene to be able to unload it later
            Scene currentScene = SceneManager.GetActiveScene();

            // The Application loads the Scene in the background at the same time as the current Scene.
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);

            // Wait until the last operation fully loads to return anything
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // Move the GameObject (you attach this in the Inspector) to the newly loaded Scene
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetSceneByName(scene));
            // Unload the previous Scene
            SceneManager.UnloadSceneAsync(currentScene);
    }

}
