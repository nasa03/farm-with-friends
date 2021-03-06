﻿using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public float movespeed;
    public float sprintspeed;
    public GameObject reticulePrefab;
    public GameObject waterFXPrefab;
    public AudioClip waterSound;
    public AudioClip digSound;
    public AudioClip digSoundFail;
    public AudioClip backpackSound;
    private GameObject reticule;
    private GameObject actionTile;
    private Vector3 moveDirection;
    private float gravity = -20.0f;
    private float verticalSpeed = 0.0f;
    private CollisionFlags collisionFlags;
    public int PlayerIndex { get; private set; }
    public InputDevice playerDevice { get; private set; }
    bool isPlayerBound;
    PlayerState playerState;

    enum PlayerState {
        Shopping,
        Normal
    }

    void Awake ()
    {
        SetState (PlayerState.Normal);
        isPlayerBound = false;
        moveDirection = transform.TransformDirection (Vector3.forward);
    }

    void Start ()
    {
        SpawnReticule ();
    }
 
    void Update ()
    {
        if(!isPlayerBound)
        {
            return;
        }

        ApplyGravity ();

        if (playerState == PlayerState.Normal) {
            Move ();
            TryHoe ();
            TryPlanting ();
            TryPicking ();
            TryWatering ();
            TryCycleItems ();
            TryDebugs ();
        }
    }
 
    private void LateUpdate ()
    {
        if (reticule != null)
            SnapReticuleToActionTile ();
    }

    // Snaps the Player's reticule to the current action tile
    private void SnapReticuleToActionTile ()
    {
        GameObject tile = GetActionTile ();
        actionTile = tile;
        if (tile != null) {
            if (!reticule.activeInHierarchy)
                reticule.SetActive (true);
            SnapReticuleToTarget (actionTile);
        } else {
            if (reticule.activeInHierarchy)
                reticule.SetActive (false);
        }
    }
 
    // Snaps the reticule's position and rotation to match a target
    private void SnapReticuleToTarget (GameObject target)
    {
        reticule.transform.position = target.transform.position;
        reticule.transform.rotation = target.transform.rotation;
     
        // Get YOffset based on what is highlighted
        float reticuleHeight = reticule.transform.lossyScale.y;
        float plantOffset = 0.0f;
        Plant plantOnTile = target.GetComponent<GroundTile>().getPlant();
        if(plantOnTile != null)
        {
            plantOffset = plantOnTile.transform.lossyScale.y;
        }
        float targetYOffset = ((GroundTile.SIZE_Y + reticuleHeight) / 2) + plantOffset;
        reticule.transform.position = target.transform.position + Vector3.up * targetYOffset;
    }
 
    // Spawns a reticule using the reticule Prefab
    private void SpawnReticule ()
    {
        reticule = (GameObject)Instantiate (reticulePrefab, Vector3.zero, Quaternion.identity);
        reticule.transform.parent = transform;
    }

    /*
     * Sets vertical speed to the expected value based on whether or not the Player is grounded.
     */
    private void ApplyGravity ()
    {
        if (IsGrounded ()) {
            verticalSpeed = 0.0f;
        } else {
            verticalSpeed += gravity * Time.deltaTime;
        }
    }

    /*
     * Checks to see if the Player is grounded by checking collision flags.
     */
    private bool IsGrounded ()
    {
        return (collisionFlags & CollisionFlags.CollidedBelow) != 0;
    }

    /*
     * Apply movement in the Player's desired directions according to the various speed
     * and movement variables.
     */
    void Move ()
    {
        // Get input values
        float horizontal = 0.0f, vertical = 0.0f;
        horizontal = RBInput.GetAxisRawForPlayer (InputStrings.HORIZONTAL, PlayerIndex, playerDevice);
        vertical = RBInput.GetAxisRawForPlayer (InputStrings.VERTICAL, PlayerIndex, playerDevice);

        // Determine move direction from target values
        float targetSpeed = 0.0f;
        Vector3 targetDirection = new Vector3 (horizontal, 0.0f, vertical);
        if (targetDirection != Vector3.zero) {
            moveDirection = Vector3.RotateTowards (moveDirection, targetDirection, Mathf.Infinity, 1000);
            moveDirection = moveDirection.normalized;

            if(RBInput.GetButtonForPlayer(InputStrings.SPRINT, PlayerIndex, playerDevice))
            {
                targetSpeed = sprintspeed;
            }
            else
            {
                targetSpeed = movespeed;
            }
        }

        // Get movement vector
        Vector3 movement = (moveDirection * targetSpeed) + new Vector3 (0.0f, verticalSpeed, 0.0f);
        movement *= Time.deltaTime;

        // Apply movement vector
        CharacterController biped = GetComponent<CharacterController> ();
        collisionFlags = biped.Move (movement);
     
        // Rotate to face the direction of movement immediately
        if (moveDirection != Vector3.zero) {
            transform.rotation = Quaternion.LookRotation (moveDirection);
        }
    }    
 
    /*
     * Attempts to hoe the action tile
     */
    void TryHoe ()
    {
        bool isUsingWeapon = RBInput.GetButtonDownForPlayer (InputStrings.WEAPON1, PlayerIndex, playerDevice);
        if (isUsingWeapon) {
            if (actionTile != null) {
                GroundTile tile = (GroundTile)actionTile.GetComponent<GroundTile> ();
                tile.Hoe ();
                AudioSource.PlayClipAtPoint (digSound, transform.position);
            } else {
                AudioSource.PlayClipAtPoint (digSoundFail, transform.position);
            }
        }
    }
 
    /*
     * Check if the user tried planting and if so, check that location is
     * a valid place to plant. Then plant it and handle inventory changes.
     */
    void TryPlanting ()
    {
        bool isUsingItem = RBInput.GetButtonDownForPlayer (InputStrings.ITEM, PlayerIndex, playerDevice);
        if (isUsingItem) {
            if (actionTile != null) {
                GroundTile tile = (GroundTile)actionTile.GetComponent<GroundTile> ();
                //TODO This violates MVC, fix it
                if (tile.isSoil () && tile.getPlant() == null) {
                    Inventory inventory = (Inventory)GetComponent<Inventory> ();
                    if (inventory.GetEquippedItem () != null) {
                        GameObject plant = inventory.GetEquippedItem ().plantPrefab;
                        tile.Plant (plant);
                        inventory.RemoveItem (inventory.GetEquippedItem ().id, 1);
                    }
                }
            }
        }
    }

    /*
     * Check if the tile is a valid tile to be picked and if so, pick it
     * and handle inventory changes.
    */
    void TryPicking ()
    {
        bool isAction = RBInput.GetButtonDownForPlayer (InputStrings.ACTION, PlayerIndex, playerDevice);
        if (isAction) {
            //.TODO This violates MVC, fix it
            if (actionTile != null) {
                GroundTile tile = (GroundTile)actionTile.GetComponent<GroundTile> ();
                Plant plant = tile.getPlant ();
                if (plant != null && plant.isRipe ()) {
                    Inventory inventory = (Inventory)GetComponent<Inventory> ();
                    int pickedItemID = tile.Pick ();
                    inventory.AddItem (pickedItemID, 1);
                    AudioSource.PlayClipAtPoint (backpackSound, transform.position);
                }
            }
        }
    }
 
    /*
     * If tile has a plant and player isn't out of water, water it.
     */
    void TryWatering ()
    {
        bool isWatering = RBInput.GetButtonDownForPlayer (InputStrings.WEAPON2, PlayerIndex, playerDevice);
        if (isWatering) {
            if (actionTile != null) {
                GroundTile tile = (GroundTile)actionTile.GetComponent<GroundTile> ();
                Plant plant = tile.getPlant ();
                if (plant != null) {
                    plant.Water ();
                }
                SpawnWaterFX ();
            }
        }
    }

    /*
     * Spawns water fx with default orientation.
     */
    void SpawnWaterFX ()
    {
        AudioSource.PlayClipAtPoint (waterSound, transform.position);
        GameObject fx = (GameObject)Instantiate (waterFXPrefab, reticule.transform.position,
                Quaternion.LookRotation (Vector3.up, Vector3.back));
        Destroy (fx, 2.0f);
    }

    /*
     * Reads input and handles action for cycling through the players items in his inventory
     */
    void TryCycleItems ()
    {
        bool isCycleItems = RBInput.GetButtonDownForPlayer (InputStrings.SWAPITEM, PlayerIndex, playerDevice);
        if (isCycleItems) {
            CycleItems ();
        }
    }

    /*
     * Cycles to the next item in the player's inventory
     */
    void CycleItems ()
    {
        Inventory inventory = (Inventory)GetComponent<Inventory> ();
        inventory.EquipNextItem ();
    }

    /*
     * Reads input and handles action for all debug functions
     */
    void TryDebugs ()
    {
        bool isAtShop = Input.GetKeyDown (InputStrings.DEBUG_INVENTORY[PlayerIndex]);
        if (isAtShop) {
            Shop shop = (Shop)GameObject.FindGameObjectWithTag ("Shop").GetComponent<Shop> ();
            shop.StartBuying (PlayerIndex);
        }
    }
 
    GameObject GetActionTile ()
    {
        // The Player tries to act on the tile in front of him - assumes no tiles overlap in Y
        float zOffset = 1.0f;
        Vector3 actionOffset = new Vector3 (0.0f, 0.0f, zOffset);
        Vector3 actionPosition = transform.position + transform.forward * actionOffset.magnitude;
     
        // Should use a constant for a grid size...
        const float TILESIZE_HALF = 1.0f / 2;
     
        // From all tiles, find the that our action position overlaps
        GameObject[] tiles = GameObject.FindGameObjectsWithTag ("Tile");
        GameObject actionTile = null;
        foreach (GameObject tile in tiles) {
            if (Mathf.Abs ((actionPosition.x - tile.transform.position.x)) < TILESIZE_HALF &&
             Mathf.Abs ((actionPosition.z - tile.transform.position.z)) < TILESIZE_HALF) {
                actionTile = tile;
            }
        }

        return actionTile;
    }

    void SetState (PlayerState state)
    {
        playerState = state;
    }

    public void SetNormalState ()
    {
        SetState (PlayerState.Normal);
    }

    public void SetShoppingState ()
    {
        SetState (PlayerState.Shopping);
    }

    public Item GetEquippedItem ()
    {
        Inventory inventory = (Inventory)GetComponent<Inventory> ();
        if (inventory.GetEquippedItem () == null)
            return null;

        return inventory.GetEquippedItem ();
    }

    public void SnapToPoint (Transform point)
    {
        transform.position = point.transform.position;
    }

    public void BindPlayer (int index, InputDevice device)
    {
        isPlayerBound = true;

        PlayerIndex = index;
        playerDevice = device;

        // Equip something by default
        CycleItems();
    }
}