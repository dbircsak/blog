using UnityEngine;
using System.Collections.Generic;

public class TerrainManager : MonoBehaviour
{
    // x goes top to bottom incrementally
    // z goes from left to right incrementally
    const int xTerrainMax = 3; // How many terrains high
    const int zTerrainMax = 6; // How many terrains wide
    const float terrainSize = 200.0f; // Size of each terrain

    // Used to keep track of what terrain is loaded
    struct TerrainStruct
    {
        public GameObject gameObject;
        public float timeSinceNeeded;
        public int[] neighborIndicies;
    }
    TerrainStruct[] terrainArray = new TerrainStruct[xTerrainMax * zTerrainMax];
    private static TerrainManager instance = null;
    float checkNeededTime = 0;

    public void Start()
    {
        // Precompute values that will never change
        for (int i = 0; i < terrainArray.Length; i++)
        {
            Vector3 pos = getAnchorPos(i);
            terrainArray[i].neighborIndicies = getNeighborIndicies(pos);
        }
    }

    public void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
    }

    public void Update()
    {
        // Every three seconds check to see if we need the loaded terrains
        if (checkNeededTime < Time.timeSinceLevelLoad)
        {
            checkNeededTime = Time.timeSinceLevelLoad + 1.0f;
            updateArray();
        }
    }

    // Fixme: move terrain loading to seperate thread
    void updateArray()
    {
        for (int i = 0; i < terrainArray.Length; i++)
        {
            if (terrainArray[i].timeSinceNeeded < Time.timeSinceLevelLoad)
            {
                if (terrainArray[i].gameObject == null)
                    continue;
                // Don't need anymore so remove
                Destroy(terrainArray[i].gameObject);
            }
            else
            {
                if (terrainArray[i].gameObject != null)
                    continue;

                // Load any needed terrain
                TerrainData t = Resources.Load("Terrain" + i) as TerrainData;
                terrainArray[i].gameObject = Terrain.CreateTerrainGameObject(t);
                terrainArray[i].gameObject.transform.position = getAnchorPos(i);
            }
        }
    }

    int getIndex(Vector3 pos)
    {
        // Index with xTerrainMax of 2 and zTerrainMax of 5 looks like this:
        // 0 1 2 3 4
        // 5 6 7 8 9
        // If terrainSize is 100 and we're at Vector3(100, 0, 250) then index is 7
        if (pos.x < 0 || pos.x >= terrainSize * xTerrainMax)
            return -1;
        if (pos.z < 0 || pos.z >= terrainSize * zTerrainMax)
            return -1;
        return ((Mathf.FloorToInt(pos.x / terrainSize) % xTerrainMax) * zTerrainMax) + (Mathf.FloorToInt(pos.z / terrainSize) % zTerrainMax);
    }

    Vector3 getAnchorPos(int index)
    {
        // Index with xTerrainMax of 2 and zTerrainMax of 5 looks like this:
        // 0 1 2 3 4
        // 5 6 7 8 9
        // If index is 7 and terrainSize is 100 then return Vector3(100, 0, 200)
        // Always return top left most coordinate
        Vector3 pos = new Vector3();
        pos.x = Mathf.FloorToInt(index / zTerrainMax) * terrainSize;
        pos.z = (index % zTerrainMax) * terrainSize;
        return pos;
    }

    int[] getNeighborIndicies(Vector3 pos)
    {
        // Index with xTerrainMax of 2 and zTerrainMax of 5 looks like this:
        // 0 1 2 3 4
        // 5 6 7 8 9
        // If we are at 7 then we want to return 1, 2, 3, 6, 7, 8
        // If we are at 0 then we want to return 0, 1, 5, 6
        int center = getIndex(pos);
        if (center == -1)
            return new int[] { };
        int left = getIndex(new Vector3(pos.x, 0, pos.z - terrainSize));
        int right = getIndex(new Vector3(pos.x, 0, pos.z + terrainSize));
        int top = getIndex(new Vector3(pos.x - terrainSize, 0, pos.z));
        int topleft = getIndex(new Vector3(pos.x - terrainSize, 0, pos.z - terrainSize));
        int topright = getIndex(new Vector3(pos.x - terrainSize, 0, pos.z + terrainSize));
        int bottom = getIndex(new Vector3(pos.x + terrainSize, 0, pos.z));
        int bottomleft = getIndex(new Vector3(pos.x + terrainSize, 0, pos.z - terrainSize));
        int bottomright = getIndex(new Vector3(pos.x + terrainSize, 0, pos.z + terrainSize));
        List<int> ans = new List<int>();
        ans.Add(center);
        if (left != -1)
            ans.Add(left);
        if (right != -1)
            ans.Add(right);
        if (top != -1)
            ans.Add(top);
        if (topleft != -1)
            ans.Add(topleft);
        if (topright != -1)
            ans.Add(topright);
        if (bottom != -1)
            ans.Add(bottom);
        if (bottomleft != -1)
            ans.Add(bottomleft);
        if (bottomright != -1)
            ans.Add(bottomright);
        return ans.ToArray();
    }

    void updateArray(int[] indicies)
    {
        for (int i = 0; i < indicies.Length; i++)
        {
            int index = indicies[i];
            if (index < 0 || index >= terrainArray.Length)
                continue;
            // Mark as needed so it's not deleted
            terrainArray[index].timeSinceNeeded = Time.timeSinceLevelLoad + 1.0f;
        }
    }

    public static void reportLocation(Vector3 pos)
    {
        // Player objects should be calling this
        if (instance == null)
            return;
        int center = instance.getIndex(pos);
        if (center == -1)
            return;
        instance.updateArray(instance.terrainArray[center].neighborIndicies);
    }
}
