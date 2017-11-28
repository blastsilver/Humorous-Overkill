﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class singleLevelDeletion : MonoBehaviour {

    public GameObject boxCollider, level, door;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {

            level.SetActive(false);
            if (door != null)
            {
                door.SetActive(true);
            }
            boxCollider.SetActive(false);

        }
    }
}