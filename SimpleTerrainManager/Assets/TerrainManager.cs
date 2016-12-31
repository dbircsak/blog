using UnityEngine;

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
    }
    TerrainStruct[] terrainArray = new TerrainStruct[xTerrainMax * zTerrainMax];
    private static TerrainManager instance = null;
    float checkNeededTime = 0;

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
            checkNeededTime = Time.timeSinceLevelLoad + 3.0f;
            for (int i = 0; i < terrainArray.Length; i++)
            {
                if (terrainArray[i].gameObject == null)
                    continue;
                // Don't need anymore so remove
                if (terrainArray[i].timeSinceNeeded < Time.timeSinceLevelLoad)
                    Destroy(terrainArray[i].gameObject);
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
        int bottom = getIndex(new Vector3(pos.x + terrainSize, 0, pos.z));

        if (left != -1)
        {
            if (right != -1)
            {
                if (top != -1)
                {
                    if (bottom != -1) // We have everything
                        return new int[] { top - 1, top, top + 1, center - 1, center, center + 1, bottom - 1, bottom, bottom + 1 };
                    else // Remove bottom
                        return new int[] { top - 1, top, top + 1, center - 1, center, center + 1 };
                }
                else
                {
                    if (bottom != -1) // Remove top
                        return new int[] { center - 1, center, center + 1, bottom - 1, bottom, bottom + 1 };
                }
            }
            else
            {
                if (top != -1)
                {
                    if (bottom != -1) // Remove right
                        return new int[] { top - 1, top, center - 1, center, bottom - 1, bottom };
                    else // Remove right and bottom
                        return new int[] { top - 1, top, center - 1, center };
                }
                else
                {
                    if (bottom != -1) // Remove right and top
                        return new int[] { center - 1, center, bottom - 1, bottom };
                }
            }
        }
        else
        {
            if (right != -1)
            {
                if (top != -1)
                {
                    if (bottom != -1) // Remove left
                        return new int[] { top, top + 1, center, center + 1, bottom, bottom + 1 };
                    else // Remove left and bottom
                        return new int[] { top, top + 1, center, center + 1 };
                }
                else
                {
                    if (bottom != -1) // Remove left and top
                        return new int[] { center, center + 1, bottom, bottom + 1 };
                }
            }
        }
        // Note function was written assuming xTerrainMax and zTerrainMax are never less than 3
        return new int[] { };
    }

    void updateArray(int[] indicies)
    {
        // Load any needed terrain
        for (int i = 0; i < indicies.Length; i++)
        {
            int index = indicies[i];
            if (index < 0 || index >= terrainArray.Length)
                continue;
            if (terrainArray[index].gameObject == null)
            {
                TerrainData t = Resources.Load("Terrain" + index) as TerrainData;
                terrainArray[index].gameObject = Terrain.CreateTerrainGameObject(t);
                terrainArray[index].gameObject.transform.position = getAnchorPos(index);
            }
            // Mark as needed so it's not deleted
            terrainArray[index].timeSinceNeeded = Time.timeSinceLevelLoad + 1.0f;
        }
    }

    public static void reportLocation(Vector3 pos)
    {
        // Player objects should be calling this
        if (instance == null)
            return;
        int[] indicies = instance.getNeighborIndicies(pos);
        instance.updateArray(indicies);
    }
}
