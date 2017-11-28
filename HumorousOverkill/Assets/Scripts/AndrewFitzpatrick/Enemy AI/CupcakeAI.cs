﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CupcakeAI : MonoBehaviour
{
    #region variables

    public DroneEnemyInfo myInfo;
    public bool showGizmos = false;

    private Vector3 currentTarget;
    private RaycastHit wanderHitInfo;

    // health / attacking
    [Header("Health / Attacking")]
    private GameObject player; // reference to the player
    public GameObject projectile; // projectile prefab
    private float shotTimer = 0;

    // freeze for when the game is paused
    public bool freeze = false;

    private bool dead = false;

    private float startHealth;

    [Tooltip("Color to flash when taking damage")]
    public Color flashColor;
    [Tooltip("How fast to flash when taking damage")]
    public float flashSpeed;
    // flash timer
    private float flashTimer = 0;
    private bool flashing = false;

    // what types of pickups to drop
    public List<GameObject> pickupPrefabs;

    private LayerMask pickupSpawnLayerMask;

    // reference to scoremanager
    private scoreManager scoremanager;

    __eHandle<object, __eArg<GameEvent>> events;

    #endregion

    public void Awake()
    {
        events = new __eHandle<object, __eArg<GameEvent>>(HandleEvent);
        __event<GameEvent>.HandleEvent += events;

        // find the player
        player = GameObject.FindGameObjectWithTag("Player");

        // get default info from EnemyManager (if it exists)
        if(GameObject.FindObjectOfType<EnemyManager>() != null)
        {
            myInfo = GameObject.FindObjectOfType<EnemyManager>().defaultDroneInfo;
        }
        else
        {
            Debug.Log("Cupcake could not find an EnemyManager");
        }

        startHealth = myInfo.health;

        pickTarget();

        pickupSpawnLayerMask = LayerMask.GetMask("Player", "Enemy");

        // find score manager (if it exists)
        if (GameObject.FindObjectsOfType<scoreManager>().Length != 0)
        {
            scoremanager = GameObject.FindObjectsOfType<scoreManager>()[0];
        }
    }

    void Update()
    {
        if(!freeze)
        {
            wander();
        }
	}

    #region functions

    void wander()
    {
        // if we are within the margin of error pick a new target
        if ((transform.position - currentTarget).sqrMagnitude < Mathf.Pow(myInfo.errorMargin, 2))
        {
            pickTarget();
        }

        // avoid walls and other enemies
        if (Physics.Raycast(transform.position, transform.forward, out wanderHitInfo, myInfo.avoidRadius))
        {
            if (wanderHitInfo.collider.gameObject.tag == "Avoid" || wanderHitInfo.collider.gameObject.tag == "Enemy")
            {
                currentTarget += wanderHitInfo.normal;
            }
        }

        Vector3 direction = currentTarget - transform.position;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(direction), myInfo.turnSpeed * Time.deltaTime);

        transform.Translate(transform.forward * myInfo.wanderSpeed * Time.deltaTime, Space.World);

        if(nearPlayer())
        {
            shootPlayer();
        }
        else
        {
            shotTimer = 0;
        }

        // flash if needed
        if(flashing)
        {
            flash();
        }
    }

    void pickTarget()
    {
        currentTarget = transform.position + getRandomVector(myInfo.targetRadius);
    }

    Vector3 getRandomVector(float radius)
    {
        Vector2 vec2D = Random.insideUnitCircle.normalized * radius;
        return new Vector3(vec2D.x, 0, vec2D.y);
    }

    // returns true if the player is within range
    bool nearPlayer()
    {
        Vector3 playerOffset = player.transform.position - transform.position;
        playerOffset.y = 0;

        return playerOffset.magnitude < Mathf.Pow(myInfo.attackRange, 2);
    }

    // shoot at player
    void shootPlayer()
    {
        // increase shot timer
        shotTimer += Time.deltaTime;

        // when shot timer reaches 1 / fire rate
        if(shotTimer > (1 / myInfo.fireRate))
        {
            // pick a point around the player using accuracy
            Vector3 accuracyOffset = Random.insideUnitCircle * myInfo.accuracy;
            accuracyOffset.z = accuracyOffset.y;
            accuracyOffset.y = 0;

            Vector3 shotPoint = player.transform.position + accuracyOffset;

            // create the projectile
            GameObject currentProjectile = Instantiate(projectile, transform.position + transform.forward, Quaternion.LookRotation(shotPoint - (transform.position + transform.forward)) * Quaternion.Euler(0, 90, 0)) as GameObject;
            Debug.DrawRay(transform.position + transform.forward, shotPoint - (transform.position + transform.forward), Color.cyan, 0.5f);
            // add impulse force to the projectile towards the player at shotForce
            currentProjectile.GetComponent<Rigidbody>().AddForce((shotPoint - currentProjectile.transform.position).normalized * myInfo.shotForce, ForceMode.Impulse);
            // destroy the projectile after a set time
            Destroy(currentProjectile, myInfo.projectileLifetime);

            // reset shot timer
            shotTimer = 0;
        }
    }

    // disable all AI and explode into pieces
    void die()
    {
        // stop flashing
        changeColor(Color.white);

        // tell enemy manager that an enemy has died
        EventManager<GameEvent>.InvokeGameState(this, null, null, typeof(EnemyManager), GameEvent.ENEMY_SPAWNER_REMOVE);

        // tell score manager that a cupcake has died (if it exists)
        if (scoremanager != null)
        {
            scoremanager.updatePoints(scoreManager.ENEMYTYPE.CUPCAKE);
        }

        // disable animation
        if (GetComponent<Animator>() != null)
        {
            GetComponent<Animator>().enabled = false;
        }

        dropPickup();

        GetComponent<BoxCollider>().enabled = false;

        // loop through children
        Transform[] childTransforms = GetComponentsInChildren<Transform>();
        foreach (Transform child in childTransforms)
        {
            // add a collider
            MeshCollider newCollider = child.gameObject.AddComponent<MeshCollider>();
            newCollider.convex = true;

            // add a RigidBody
            child.gameObject.AddComponent<Rigidbody>();

            // add explosion force to launch the model
            child.GetComponent<Rigidbody>().AddExplosionForce(myInfo.explosionForce, transform.position - Vector3.up * myInfo.explosionRadius, myInfo.explosionRadius);
        }

        // disable this script to prevent any more actions
        this.enabled = false;

        // fully delete this gameObject after a set number of seconds
        Destroy(this.gameObject, 5);
    }

    public void OnDestroy()
    {
        __event<GameEvent>.HandleEvent -= events;
    }

    void dropPickup()
    {
        // randomly drop a pickup (on the floor)
        if (Random.Range(0, 100) < myInfo.pickupDropRate * 100)
        {
            RaycastHit ammoDropRay;
            if (Physics.Raycast(transform.position, -Vector3.up, out ammoDropRay, pickupSpawnLayerMask))
            {
                Instantiate(pickupPrefabs[Random.Range(0, pickupPrefabs.Count)], ammoDropRay.point, Quaternion.identity);
            }
        }
    }

    void changeColor(Color newColor)
    {
        foreach (MeshRenderer child in GetComponentsInChildren<MeshRenderer>())
        {
            child.material.SetColor("_Color", newColor);
        }
    }

    public void HandleEvent(object sender, __eArg<GameEvent> e)
    {
        // if we are not the sender or we are not the target return
        if (sender == (object)this)
        {
            return;
        }
        if(e.target != (object)this.gameObject)
        {
            return;
        }

        // Health is depleted
        if (e.arg == GameEvent.ENEMY_DAMAGED)
        {
            if(!dead)
            {
                // subtract health
                myInfo.health -= (float)e.value;

                // flash
                flashTimer = 0.0f;
                flashing = true;

                // if the health is now 0 die
                if (myInfo.health <= 0)
                {
                    dead = true;
                    die();
                }
            }
        }
        // game paused
        else if (e.arg == GameEvent.STATE_PAUSE)
        {
            freeze = true;
        }
        // game unpaused
        else if(e.arg == GameEvent.STATE_CONTINUE)
        {
            freeze = false;
        }
    }

    public void flash()
    {
        flashTimer += flashSpeed * Time.deltaTime;

        // change color
        changeColor(Color.Lerp(flashColor, Color.white, flashTimer));

        if(flashTimer >= 1.0f)
        {
            flashing = false;
            flashTimer = 0.0f;
        }
    }

    #endregion

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (showGizmos)
        {
            // display targetRadius
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, myInfo.targetRadius);

            // display margin of error
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, myInfo.errorMargin);

            // show currentVelocity
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * myInfo.wanderSpeed);

            // display current target
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentTarget, 0.5f);
        }
    }
#endif
}