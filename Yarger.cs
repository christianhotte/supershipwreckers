using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Yarger : MonoBehaviour
{
    /* This script facilitates all player yarging. It recieves input from playerController about type of yarg, then
     * executes based on factors such as skin and duration.
     * All yarging mechanics go here, and should be universal between player characters
     * This script also contains all the necessary code to attach itself as a skin
     */

    public enum YargType { none, shortYarg, mediumYarg, longYarg, tallYarg, special }; //Types of yargs that can be done (depending on combination of inputs)
    public enum YargQuan { none, single, multi }; //Types of yarg combination used for yarg-related memory
    internal YargType currentYarg; //Current yarg type (based on inputs) sent from PlayerController
    internal YargType prevYarg = YargType.none; //Previous update's state of currentYarg

    //Objects and Components:
    public AudioClip[] shortYargs; //Pool of shortYargs for this skin
    public AudioClip[] mediumYargs; //Pool of mediumYargs for this skin
    public AudioClip[] longYargs; //Pool of longYargs for this skin
    public AudioClip[] tallYargs; //Pool of tallYargs for this skin
    private AudioSource mouth; //Audio source for yargs
    private Animator animator; //Yarg animator (on current skin)
    internal MultiYarger multiYarger; //Special multiyarg codes, sent here by skin.  Multiyargers contain methods for executing special yargs

    //Yarg Input Variables:
    internal float[] yargPack = { 0, 0, 0, 0 }; //Yarg input array from playerController

    //Yarg Types:
    //Single Yargs: These codes must be different, otherwise code will default to highest one on list
    private float[] shortYarg =   { 1, 0, 0, 0 };
    private float[] mediumYarg =  { 0, 1, 0, 0 };
    private float[] longYarg =    { 0, 0, 1, 0 };
    private float[] tallYarg =    { 0, 0, 0, 1 };
    //MultiYargs: These yarg combos can be set on a skin-to-skin basis (if the skin has a custom "MultiYarg" script)
    private float[] multiYarg1 =  { 1, 1, 0, 0 };
    private float[] multiYarg2 =  { 1, 0, 1, 0 };
    private float[] multiYarg3 =  { 1, 0, 0, 1 };
    private float[] multiYarg4 =  { 0, 1, 1, 0 };
    private float[] multiYarg5 =  { 0, 1, 0, 1 };
    private float[] multiYarg6 =  { 0, 0, 1, 1 };
    private float[] multiYarg7 =  { 1, 1, 1, 0 };
    private float[] multiYarg8 =  { 1, 1, 0, 1 };
    private float[] multiYarg9 =  { 1, 0, 1, 1 };
    private float[] multiYarg10 = { 0, 1, 1, 1 };
    private float[] multiYarg11 = { 1, 1, 1, 1 };

    //Yarg Memory Variables:
    private bool yargDetected; //Used to tell meta program loop a yarg is ongoing
    private YargQuan yarging; //Used to tell program that a yarg is ongoing
    private YargQuan prevYarging; //Used to check if a new yarg is being initiated
    public int MemCount; //Max number of yargs to keep in memory. Should be same as longest code length
    private List<string> yargMem = new List<string>(); //Remembers last  yargs for secret yarg combinations

    void Start()
    {
        UpdateSkin(); //Get starting skin animator
    }

    private void Update()
    {
        //Check for yarg input (and type of yarg input):
        if (ArrayTotal(yargPack) == 0)      { yarging = YargQuan.none; }
        else if (ArrayTotal(yargPack) == 1) { yarging = YargQuan.single; }
        else if (ArrayTotal(yargPack) >= 0)  { yarging = YargQuan.multi; }
        //Debug.Log(yarging);

        //Execute Yarg Output:
        if (yarging == YargQuan.single)
        {

            if (ArraysEqual(yargPack, shortYarg)) { currentYarg = YargType.shortYarg; animator.SetBool("ShortYarg", true); } //Execute shortYarg
            else if (ArraysEqual(yargPack, mediumYarg)) { currentYarg = YargType.mediumYarg; animator.SetBool("MediumYarg", true); } //Execute mediumYarg
            else if (ArraysEqual(yargPack, longYarg)) { currentYarg = YargType.longYarg; animator.SetBool("LongYarg", true); } //Execute longYarg
            else if (ArraysEqual(yargPack, tallYarg)) { currentYarg = YargType.tallYarg; animator.SetBool("TallYarg", true); } //Execute tallYarg

            UpdateYargMem(); //Commit yarg to memory
            prevYarging = YargQuan.single;
        }
        else if (yarging == YargQuan.multi)
        {
            ClearYargs(); //Set default yarg parameters to false
                if (multiYarger != null) //If the current skin has a multiYarger...
                {
                    //Execute special yarg
                    currentYarg = YargType.special;
                }
                else //If there is no multiYarger...
                {
                    Debug.Log("No multiyarger setup for this skin");
                }

            prevYarging = YargQuan.multi;
        }
        else
        {
            ClearYargs(); //Set default yarg parameters to false
            currentYarg = YargType.none; //Clear currentYarg
            prevYarging = YargQuan.none; //Clear yargMemory
        }

        //Update Yarg Audio:
        if (currentYarg == YargType.shortYarg  && prevYarg == YargType.none) { PlayYarg(shortYargs,  false); } //Initiate new shortYarg
        if (currentYarg == YargType.mediumYarg && prevYarg == YargType.none) { PlayYarg(mediumYargs, false); } //Initiate new mediumYarg
        if (currentYarg == YargType.longYarg   && prevYarg == YargType.none) { PlayYarg(longYargs,   false); } //Initiate new longYarg
        if (currentYarg == YargType.tallYarg   && prevYarg == YargType.none) { PlayYarg(tallYargs,   false); } //Initiate new tallYarg
        if (currentYarg == YargType.none) { PlayYarg(null, false); } //Cancel yargs if there is no input
        prevYarg = currentYarg; //Update prevYarg
    }

    public void UpdateSkin()
    {
        //Updates skin on player (automatically overriding old skin), should be changed IMMEDIATELY every time player skin is assigned or changed
        //Add multiYarger integration on other end (in multiYarger)
        PlayerController playerCont = gameObject.GetComponentInParent<PlayerController>(); //Temporarily get PlayerController
        if (playerCont.skin != null) { playerCont.skin.GetComponent<SpriteRenderer>().enabled = false; } //"Kill" previous skin
        playerCont.skin = gameObject; //Set player skin to this gameObject
        playerCont.yarger = this; //Set player yarger to this
        playerCont.GetComponent<Damager>().skinRen = GetComponent<SpriteRenderer>(); //Set player damager skin renderer to this
        if (GetComponent<MultiYarger>() != null) //Get multiYarger from skin (if it has one)
            { multiYarger = GetComponent<MultiYarger>(); }
        if (playerCont.player == PlayerController.Player.Player1)      GetComponent<SpriteRenderer>().sortingLayerName = "Player1 Sprites"; //Set sorting layer
        else if (playerCont.player == PlayerController.Player.Player2) GetComponent<SpriteRenderer>().sortingLayerName = "Player2 Sprites"; //Set sorting layer
        animator = GetComponent<Animator>(); //Get yarg animator from that skin
        mouth = GetComponent<AudioSource>(); //Get mouth
    }

    public void PlayYarg(AudioClip[] yargType, bool playOver)
    {
        //Called once per yarg, leaving yargType null will cancel all current yargs
        if (yargType == null || playOver == false) //For cancelling previous yarg or just cancelling yarg and playing new one
        {
            mouth.Stop(); //Stop last yarg
        }
        if (yargType != null && yargType.Length != 0 && GetComponent<SpriteRenderer>().enabled == true) //For playing a yarg
        {
            mouth.clip = yargType[Random.Range(0, yargType.Length)]; //Give source new random clip
            mouth.Play(); //Play new clip
        }
    }

    public void ClearYargs() //Clears all default yarg animations
    {
        animator.SetBool("ShortYarg", false);
        animator.SetBool("MediumYarg", false);
        animator.SetBool("LongYarg", false);
        animator.SetBool("TallYarg", false);
    }

    public bool ArraysEqual(float[] array1, float[] array2) //Compares given arrays and returns if all elements are true or not
    {
        bool result = true; //What to return (assumes arrays are equal unless proven otherwise)
        if (array1.Length != array2.Length)
        {
            Debug.Log("Error, comparing arrays of inequal length");
            result = false; //Signal function to return false
        }
        else //If arrays are comparable...
        {
            for (int y = array1.Length; y > 0; y--)
            {
                if (array1[y - 1] != array2[y - 1]) { result = false; } //Inequality detected
            }
        }
        return result; //Return result of check
    }

    public bool ArraysEqual(string[] array1, string[] array2) //Compares given arrays and returns if all elements are true or not (overload for strings)
    {
        bool result = true; //What to return (assumes arrays are equal unless proven otherwise)
        if (array1.Length != array2.Length)
        {
            Debug.Log("Error, comparing arrays of inequal length");
            result = false; //Signal function to return false
        }
        else //If arrays are comparable...
        {
            for (int y = array1.Length; y > 0; y--)
            {
                if (array1[y - 1] != array2[y - 1]) { result = false; } //Inequality detected
            }
        }
        return result; //Return result of check
    }

    public float ArrayTotal(float[] array) //Checks total of elements of array (to discern between single- and multi- yarg)
    {
        float total = 0; //Total to return (default is 0)
        for (int y = array.Length; y > 0; y--) //Parse through array...
        {
            total += array[y - 1]; //Add contents of each array element to total
        }
        return total; //Return total of elements in array
    }

    public void UpdateYargMem()
    {
        //Shifts data down yarg memory register
        //Player must fully release all yarg buttons and enter each new yarg separately in order to input codes cleanly
        if (prevYarging == YargQuan.none) //Standard function, updates for new single yargs once per yarg input
        {
            string newMem = currentYarg.ToString(); //Get string of yarg type to add to memory
            yargMem.Add(newMem); //Add yarg to memory
            //Debug.Log("NewMem = " + newMem);

            if (yargMem.Count > MemCount) //If number of yargs in memory exceeds memory count...
            {
                yargMem.RemoveAt(0); //Remove first (oldest) item in memory
            }
            if (yargMem.Count == MemCount) { CheckYargMem(); } //Start checking yarg mem with cheats
        }
    }

    public void CheckYargMem()
    {
        //Checks if the current yargMem list matches any secret codes
        //REGISTER OF CHEAT CODES HERE:
        string[] giveCannon = { "ShortYarg", "LongYarg", "ShortYarg", "LongYarg", "ShortYarg", "LongYarg" }; //Instantiates a cannonFist on player
            if (ArraysEqual(yargMem.ToArray(), giveCannon)) { Debug.Log("Cheat Used"); }
    }

    //CHEATS-------------------------------------------------------------------------------------------------------------------


}