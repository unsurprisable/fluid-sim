using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpatialGrid
{
    private Dictionary<(int, int), List<int>> spatialGrid;
    private float gridSize;

    public SpatialGrid(float smoothingRadius)
    {
        gridSize = smoothingRadius;
        spatialGrid = new Dictionary<(int, int), List<int>>();
    }

    public void AddParticle(int particleIndex, Vector2 position)
    {
        (int, int) gridKey = GetGridKey(position);
        if (!spatialGrid.ContainsKey(gridKey)) {
            spatialGrid[gridKey] = new List<int>();
        }
        spatialGrid[gridKey].Add(particleIndex);
    }

    public List<int> GetNearbyParticleIndices(Vector2 position)
    {
        List<int> nearbyParticles = new List<int>();
        (int, int) centerGridKey = GetGridKey(position);
        
        for (int row = -1; row <= 1; row++) {
            for (int col = -1; col <= 1; col++) 
            {
                (int, int) neighborGridKey = (centerGridKey.Item1 + row, centerGridKey.Item2 + col);
                if (spatialGrid.ContainsKey(neighborGridKey)) {
                    nearbyParticles.AddRange(spatialGrid[neighborGridKey]);
                }
            }
        }
        return nearbyParticles;
    }

    private (int, int) GetGridKey(Vector2 position)
    {

        int row = Mathf.FloorToInt(position.x / gridSize);
        int col = Mathf.FloorToInt(position.y / gridSize);

        return (row, col);
    }

    public void Clear()
    {
        spatialGrid.Clear();
    }



}
