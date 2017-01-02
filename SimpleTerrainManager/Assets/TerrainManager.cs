using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TerrainManager : MonoBehaviour
{
    const int xMax = 3; // How many tiles our north-south is
    const int zMax = 6; // How many tiles east-west is
    const float terrainSize = 500.0f; // Size of each terrain tile

    // How we remember where each terrain object is in a Dictionary
    class TerrainKey
    {
        public int x;
        public int z;

        // Unity uses z instead of y for east-west
        public TerrainKey(int x, int z)
        {
            this.x = x;
            this.z = z;
        }

        // Get key from position
        public TerrainKey(Vector3 pos)
        {
            x = Mathf.FloorToInt(pos.x / terrainSize);
            z = Mathf.FloorToInt(pos.z / terrainSize);
        }

        // Might be outside range
        public bool isValid()
        {
            if (x < 0 || x >= xMax)
                return false;
            if (z < 0 || z >= zMax)
                return false;
            return true;
        }

        // What tile keys are around us?
        public TerrainKey[] getNeighbors()
        {
            if (!isValid())
            {
                Debug.Log("Calling getNeighbors with invalid key");
                return new TerrainKey[] { };
            }

            TerrainKey left = new TerrainKey(x - 1, z);
            TerrainKey right = new TerrainKey(x + 1, z);
            TerrainKey top = new TerrainKey(x, z - 1);
            TerrainKey topleft = new TerrainKey(x - 1, z - 1);
            TerrainKey topright = new TerrainKey(x + 1, z - 1);
            TerrainKey bottom = new TerrainKey(x, z + 1);
            TerrainKey bottomleft = new TerrainKey(x - 1, z + 1);
            TerrainKey bottomright = new TerrainKey(x + 1, z + 1);

            if (left.isValid())
            {
                if (right.isValid())
                {
                    if (top.isValid())
                    {
                        if (bottom.isValid()) // Return all
                            return new TerrainKey[] { topleft, top, topright, left, this, right, bottomleft, bottom, bottomright };
                        else // Remove bottom
                            return new TerrainKey[] { topleft, top, topright, left, this, right };
                    }
                    else
                    {
                        if (bottom.isValid()) // Remove top
                            return new TerrainKey[] { left, this, right, bottomleft, bottom, bottomright };
                    }
                }
                else
                {
                    if (top.isValid())
                    {
                        if (bottom.isValid()) // Remove right
                            return new TerrainKey[] { topleft, top, left, this, bottomleft, bottom };
                        else // Remove right and bottom
                            return new TerrainKey[] { topleft, top, left, this };
                    }
                    else
                    {
                        if (bottom.isValid()) // Remove right and top
                            return new TerrainKey[] { left, this, bottomleft, bottom };
                    }
                }
            }
            else
            {
                if (right.isValid())
                {
                    if (top.isValid())
                    {
                        if (bottom.isValid()) // Remove left
                            return new TerrainKey[] { top, topright, this, right, bottom, bottomright };
                        else // Remove left and bottom
                            return new TerrainKey[] { top, topright, this, right };
                    }
                    else
                    {
                        if (bottom.isValid()) // Remove left and top
                            return new TerrainKey[] { this, right, bottom, bottomright };
                    }
                }
            }
            // Some cases are left out because we should never be less than size 3
            Debug.Log("xMax or zMax cannot be less than 3");
            return new TerrainKey[] { };
        }

        // Returns top left most anchor point
        public Vector3 getPos()
        {
            return new Vector3(x * terrainSize, 0, z * terrainSize);
        }

        public override string ToString()
        {
            // Also used when loading the terrain resource
            return string.Format("Terrain{0}_{1}", x, z);
        }

        // Needed to compare two keys
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            TerrainKey tk = obj as TerrainKey;
            return x == tk.x && z == tk.z;
        }

        // Needed to compare two keys
        public override int GetHashCode()
        {
            return x ^ z;
        }
    }

    class TerrainValue
    {
        // Our terrain data
        public TerrainValue()
        {
            gameObject = null;
            lastNeeded = 0;
            isLoading = false;
        }

        public GameObject gameObject;
        public float lastNeeded; // Last time player needed tile
        public bool isLoading; // Used with LoadAsync
    }
    Dictionary<TerrainKey, TerrainValue> terrainDictionary; // Store all terrain data here

    private static TerrainManager instance = null;
    public void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
    }

    void Start()
    {
        // Set up our dictionary
        terrainDictionary = new Dictionary<TerrainKey, TerrainValue>(xMax * zMax);
        for (int x = 0; x < xMax; x++)
            for (int z = 0; z < zMax; z++)
                terrainDictionary.Add(new TerrainKey(x, z), new TerrainValue());

        // Used in adding and removing terrain
        StartCoroutine(manageDictionary());
    }

    // As terrain is flagged as needed or not needed this routine will load or destroy terrain
    IEnumerator manageDictionary()
    {
        while (true)
        {
            foreach (var pair in terrainDictionary)
            {
                if (pair.Value.lastNeeded <= Time.timeSinceLevelLoad)
                {
                    if (pair.Value.gameObject == null)
                        continue;

                    // Don't need terrain so remove
                    Destroy(pair.Value.gameObject);
                }
                else
                {
                    if (pair.Value.gameObject != null)
                        continue;

                    // Need terrain so load
                    if (!pair.Value.isLoading)
                    {
                        pair.Value.isLoading = true;
                        StartCoroutine(loadTerrain(pair.Key));
                    }
                }
            }

            // Check every second
            yield return new WaitForSeconds(1.0f);
        }
    }

    // Loads terrain from resource and sets to correct position
    IEnumerator loadTerrain(TerrainKey key)
    {
        ResourceRequest request = Resources.LoadAsync(key.ToString());
        yield return null; // Starts again when LoadAsync is done

        TerrainData t = request.asset as TerrainData;
        terrainDictionary[key].gameObject = Terrain.CreateTerrainGameObject(t);
        terrainDictionary[key].gameObject.transform.position = key.getPos();
        terrainDictionary[key].isLoading = false;
    }

    // Called by player objects
    public static void reportLocation(Vector3 pos)
    {
        if (instance == null)
            return;
        TerrainKey key = new TerrainKey(pos);
        if (!key.isValid())
        {
            Debug.Log("Player outside terrain area");
            return;
        }

        // Mark neighbors as being needed
        TerrainKey[] neighbors = key.getNeighbors();
        for (int i = 0; i < neighbors.Length; i++)
        {
            key = neighbors[i];
            instance.terrainDictionary[key].lastNeeded = Time.timeSinceLevelLoad + 1.0f;
        }
    }
}
