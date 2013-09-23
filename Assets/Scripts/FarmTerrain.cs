﻿using UnityEngine;
using System.Collections;

public class FarmTerrain : MonoBehaviour {
	
	public GameObject groundTilePrefab;
	public int grassPercent = 75;
	
	void Start () {
		GenerateGroundTiles();
	}
	
	/*
	 * Spawn ground tiles all over this terrain
	*/
	private void GenerateGroundTiles()
	{
		// Get size of the terrain
		float TERRAIN_X = collider.bounds.extents.x * 2;
		float TERRAIN_Z = collider.bounds.extents.z * 2;
		
		// Start spawning from the bottom left corner, moving up and out until we reach the top right
		Vector3 bottomLeftCorner = transform.position - new Vector3(TERRAIN_X / 2, 0, TERRAIN_Z / 2);
		float NUM_ROWS = (TERRAIN_X) / GroundTile.SIZE;
		float NUM_COLS = (TERRAIN_Z) / GroundTile.SIZE;
		for(int row = 0; row < NUM_ROWS; row++)
		{
			for(int col = 0; col < NUM_COLS; col++)
			{
				SpawnGroundTileAtRowAndCol(bottomLeftCorner, row, col);
			}
		}
	}
	
	/*
	 * Spawn the tile, set it up according to the Terrain parameters,
	 * and parent it to the FarmTerrain for grouping purposes	
	*/
	private void SpawnGroundTileAtRowAndCol(Vector3 localOrigin, float row, float col)
	{
		// Tile edges should align with the terrain, not their centers, so we must shift them by their "radius"
		Vector3 TILE_OFFSET = new Vector3(GroundTile.SIZE / 2.0f, 0, GroundTile.SIZE / 2.0f);
		// Convert row and column to local x and z coordinates
		Vector3 xyzPosition = new Vector3(row * GroundTile.SIZE, 0, col * GroundTile.SIZE);
		
		GameObject tile = (GameObject) Instantiate(groundTilePrefab,
			(localOrigin + TILE_OFFSET + xyzPosition), 
			Quaternion.identity);
		tile.transform.parent = transform;
		
		// Turn this tile into a grass or dirt tile based on settings\
		GroundTile tileScript = tile.GetComponent<GroundTile>();
		float rand = Random.Range(0, 100);
		if(rand < grassPercent) {
			tileScript.SetState (GroundTile.GroundState.Grass);
		}
		else {
			tileScript.SetState (GroundTile.GroundState.Dirt);
		}
	}
}
