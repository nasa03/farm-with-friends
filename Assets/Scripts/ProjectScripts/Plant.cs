﻿using UnityEngine;
using System.Collections;
using System;

/**
 * Handles generic plant logic for growing, withering, watering,
 * and killing plants.
 */
public class Plant : MonoBehaviour
{
    int nightsOld;
    int nightsSinceGrowth;
    PlantStates plantState;
    const int MIN_LIFE = 0;
    int curLife;

    // Public Attributes
    public int nightsPerGrowth;
    public int maxLife;

    // Materials
    public Material seedMat;
    public Material sproutMat;
    public Material adultMat;
    public Material witheredMat;

    public enum PlantStates
    {
        Seed = 0,
        Sprout = 1,
        Adult = 2,
        Withered = 3
    }

    // Use this for initialization, catch unset public vars.
    void Start ()
    {
        if (nightsPerGrowth == 0) {
            Debug.LogError ("nightsPerGrowth is 0 which is invalid. Setting to 1.");
            nightsPerGrowth = 1;
        }
        if (maxLife == 0) {
            Debug.LogError ("maxLife is 0 which is invalid. Setting to 1.");
            maxLife = 1;
        }
        nightsOld = 0;
        nightsSinceGrowth = 0;
        // Set HP to one less than full so watering can make a dif on first day.
        curLife = maxLife - 1;
        if (curLife == MIN_LIFE) {
            RenderAsDry ();
        }
    }

    /**
  * If the plant is already watered, set it as watered and
  * make it visually obvious.
  */
    public void Water ()
    {
        curLife = maxLife;
        RenderAsWatered ();
    }

    /**
  * Perform our nightly aging of the plant. All logic for withering,
  * growing, and aging the plant should go here.
  */
    public void NightlyUpdate ()
    {
        // Tick down plant health and warn user if health low
        curLife--;
        if (curLife < MIN_LIFE) {
            Wither ();
        } else if (curLife == MIN_LIFE) {
            RenderAsDry ();
        }
     
        nightsSinceGrowth++;
        // If it was watered recently enough and it's old enough for the stage, grow.
        if (nightsSinceGrowth >= nightsPerGrowth && !isWithered ()) {
            Grow ();
        }
     
        nightsOld++;
        RenderPlantState ();
    }

    /**
     * Put the plant in it's next stage and reset the days since last growth.
     */
    private void Grow ()
    {
        // If plant can continue to grow, grow.
        if ((int)plantState < Enum.GetValues (typeof(PlantStates)).Length - 1) {
            Debug.Log (String.Format ("GROWING ({0})): DaysSinceGrowth ({1}) GrowthSpeed ({2}) new PlantState ({3})", 
             name, nightsSinceGrowth, nightsPerGrowth, (int)plantState + 1));
            nightsSinceGrowth = 0;
            plantState++;
        }
    }
 
    /**
     * Set the plant to withered state if not already.
     */
    private void Wither ()
    {
        if (!isWithered ()) {
            Debug.Log (String.Format ("WITHERING ({0}): CurLife reached {1}.", name, curLife));
            plantState = PlantStates.Withered;
            RenderAsWatered ();
        }
    }

    /**
     * Return if the plant is withered.
  */
    public bool isWithered ()
    {
        return plantState == PlantStates.Withered;
    }
 
    /**
  * Return if the plant is ready to be picked.
  */
    public bool isRipe ()
    {
        return plantState == PlantStates.Adult;
    }
 
    /**
  * Update the plant state to the appropriate material.
  */
    void RenderPlantState ()
    {
        if (plantState == PlantStates.Seed) {
            renderer.material = seedMat;
        } else if (plantState == PlantStates.Sprout) {
            renderer.material = sproutMat;
        } else if (plantState == PlantStates.Adult) {
            renderer.material = adultMat;
            RenderAsRipe ();
        } else if (plantState == PlantStates.Withered) {
            renderer.material = witheredMat;
            RenderAsDead ();
        }
    }

    /*
     * Change the text to indicate need to pick and turn off water effect.
     */
    void RenderAsRipe ()
    {
        SetPlantText ("pick\nme");
        light.enabled = false;
    }

    /*
     * Remove the water effect and change the text to normal.
     */
    void RenderAsWatered ()
    {
        if (!isRipe ()) {
            SetPlantText (this.name.Split ('(') [0]);
            light.enabled = false;
        }
    }
 
    /*
     * Remove the water effect and change the text to 'dead'.
     */
    void RenderAsDead ()
    {
        if (!isRipe ()) {
            SetPlantText ("dead");
            light.enabled = false;
        }
    }

    /**
     * Add the water effect and change the text to indicate need to water.
     */
    void RenderAsDry ()
    {
        SetPlantText ("water\nme");
        light.enabled = true;
    }
    
    /*
     * Set the plant textmesh to a given value.
     */
    void SetPlantText (string text)
    {
        TextMesh textMesh = (TextMesh)GetComponentInChildren<TextMesh> ();
        textMesh.text = text;
    }
}