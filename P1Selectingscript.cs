using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class P1Selectingscript : MonoBehaviour
{
    public Sprite[] skins;
    public GameObject[] skinPrefabs;
    private int skinCounter;
    private SpriteRenderer sr;
    private Menu menu;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        menu = GameObject.Find("Canvas").GetComponent<Menu>();
    }

    
    void Update()
    {

        if (Input.GetButtonDown("P1Right") == true)
        {
            skinCounter++;
            if (skinCounter > skins.Length - 1) { skinCounter = 0; }
            Debug.Log(skinCounter);
        }
        else if (Input.GetButtonDown("P1Left") == true)
        {
            skinCounter--;
            if (skinCounter <= -1) { skinCounter = skins.Length - 1; }
            Debug.Log(skinCounter);
        }
        sr.sprite = skins[skinCounter];
    }
}
