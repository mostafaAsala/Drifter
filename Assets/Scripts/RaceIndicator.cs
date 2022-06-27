using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaceIndicator : MonoBehaviour
{
    public GameObject playerCar;
    public GameObject raceInfoButton;

    

    //This code requires a collider attached to its gameObject to work and the player has to have the Player tag
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {


    }

    IEnumerator DelayedPopup ()
    {
        yield return new WaitForSeconds(1);
        raceInfoButton.SetActive(true);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            StartCoroutine(DelayedPopup());
        }
    }
}
