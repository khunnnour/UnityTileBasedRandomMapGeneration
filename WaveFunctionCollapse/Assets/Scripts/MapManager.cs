using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TreeEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public struct Record
{
	// index of origin
	public int index;
	// tiles
	public Tile origin, to;
	// map coord
	public Vector2 orCoord, toCoord;

	public Record(int i, Tile o, Tile t, Vector2 oC, Vector2 tC)
	{
		index = i;

		origin = o;
		to = t;

		orCoord = oC;
		toCoord = tC;
	}
}

public struct TileSpawnRecord
{
	// tile prefab to spawn
	public GameObject tilePrefab;
	// how many times it was rotated
	public int rotations;
}

public class MapManager : MonoBehaviour
{
	public Transform TileHolder;
	public List<GameObject> tiles;
	public int dim = 5;

	private Tile[] map;
	float tileDim, worldToCoordCoeff;

	// Start is called before the first frame update
	void Start()
	{
		tileDim = tiles[0].transform.localScale.x * 10f;
		worldToCoordCoeff = 1f / tileDim;

		// initialize the map's tile array
		map = new Tile[dim * dim];

		// if no seed tiles, then generate a random one
		if (TileHolder.childCount == 0)
		{
			GameObject temp = Instantiate(tiles[Random.Range(0, tiles.Count)], TileHolder);
			temp.transform.position = Vector3.zero;
			map[0] = temp.GetComponent<Tile>();
		}
		else
		{
			// get seed tiles (and put them where they're supposed 
			//     to be in list) from children in the tile holder
			//Debug.Log("Seed tiles: " + TileHolder.childCount);
			for (int i = 0; i < TileHolder.childCount; i++)
			{
				Transform tran = TileHolder.GetChild(i);
				Vector2 mapCoord = new Vector2(tran.position.x, tran.position.z) * worldToCoordCoeff;
				int index = MapCoord_To_Index(mapCoord.x, mapCoord.y);
				map[index] = tran.GetComponent<Tile>();
				//Debug.Log("Added seed tile at " + mapCoord + " in slot " + index);
			}
		}

		//float stTime = Time.realtimeSinceStartup;
		//Debug.Log(stTime.ToString("F3"));
		// Generate the rest of the map
		GenerateMap();
		//float finTime = Time.realtimeSinceStartup;
		//Debug.Log(finTime.ToString("F3")+" ("+(finTime-stTime).ToString("F5")+"s)");
	}

	// Update is called once per frame
	private void Update()
	{

	}

	private void GenerateMap()
	{
		List<Record> open = new List<Record>();
		List<Record> closed = new List<Record>();

		// find indexes of open spaces around seed tiles
		for (int i = 0; i < TileHolder.childCount; i++)
		{
			// get map coord of seed tile
			Vector2 mapCoord = new Vector2(TileHolder.GetChild(i).position.x, TileHolder.GetChild(i).position.z) * worldToCoordCoeff;

			// add the open indexes around that coord
			GetOpenNeighbors(ref open, ref closed, mapCoord);
		}

		//Debug.Log("initial open size: "+open.Count);

		// Process open list
		List<Record> neighbors = new List<Record>();
		while (open.Count > 0)
		{
			// get map coord of current tile
			Vector2 mapCoord = Index_To_MapCoord(open[0].index);
			//Debug.Log(" == Processing open tile at coord: " + mapCoord.ToString("F1") + " == ");

			// add the closed indexes around that coord
			neighbors.Clear();
			GetClosedNeighbors(ref neighbors, mapCoord);

			//Debug.Log(neighbors.Count+" tiles neighbor "+mapCoord);

			// start with list of available tiles
			List<TileSpawnRecord> available = new List<TileSpawnRecord>();
			// cycle thru each tile prefab
			foreach (GameObject tile in tiles)
			{
				bool fits = true;

				// create instance of the current tile
				GameObject temp = tile;

				// determine max rotations based on tile types
				int maxRotations;
				// if all sides of tile are the same, theres no need to rotate
				switch (tile.GetComponent<Tile>().connectorDescription)
				{
					case TileConnectorType.EQUILAT:
						maxRotations = 1;
						break;
					case TileConnectorType.ISOSCEL:
						maxRotations = 2;
						break;
					case TileConnectorType.SCALENE:
					case TileConnectorType.INVALID:
					default:
						maxRotations = 4;
						break;
				}

				// try every rotation for each tile
				for (int i = 0; i < maxRotations; i++)
				{
					// cycle thru every neighbor
					foreach (Record rec in neighbors)
					{
						fits = CheckIfFit(temp.GetComponent<Tile>(), rec.to, rec.toCoord - rec.orCoord);
						//Debug.Log(tile + " fit with " + rec.to + ": " + fits);
						// if side does not fit return out
						if (!fits)
							break;
					}

					// if the tile fits then add it to potential list
					if (fits)
					{
						// create a new tile spawn struct
						TileSpawnRecord newSpawnRec = new TileSpawnRecord();

						// populate
						newSpawnRec.tilePrefab = temp;
						newSpawnRec.rotations = i;

						// add to the list
						available.Add(newSpawnRec);
						//Debug.Log("Added tile to available | total=" + available.Count);
					}

					// rotate the tile to see if another potential connection
					temp.GetComponent<Tile>().Rotate(1);
				}
			}

			// if no available tiles then log error
			if (available.Count == 0)
			{
				Debug.LogWarning("Tile unable to be created");
			}
			else
			{
				// select random tile from available
				int t = Random.Range(0, available.Count);

				// create it
				GameObject temp = Instantiate(available[t].tilePrefab, TileHolder);
				temp.transform.position = new Vector3(mapCoord.x, 0f, mapCoord.y) * tileDim;
				map[open[0].index] = temp.GetComponent<Tile>();

				// handle rotation
				temp.transform.Rotate(0f, 90f * available[t].rotations, 0f);

				//Debug.Log("Created tile at " + open[0].toCoord + " from " + open[0].orCoord+", with "+ available[t].rotations+" rotations");
			}

			// remove from open list
			closed.Add(open[0]);
			open.RemoveAt(0);

			// add the open indexes around that coord
			GetOpenNeighbors(ref open, ref closed, mapCoord);
		}
	}

	// will check if t0 can fit next to t1
	private bool CheckIfFit(Tile t0, Tile t1, Vector2 dir)
	{
		t0.Rotate((int)(t0.transform.rotation.eulerAngles.y / 90f));
		t1.Rotate((int)(t1.transform.rotation.eulerAngles.y / 90f));

		if (dir == Vector2.up)
			return HasConnection(t0.topConnectors, t1.bottomConnectors);
		if (dir == Vector2.right)
			return HasConnection(t0.rightConnectors, t1.leftConnectors);
		if (dir == -Vector2.up)
			return HasConnection(t0.bottomConnectors, t1.topConnectors);
		if (dir == -Vector2.right)
			return HasConnection(t0.leftConnectors, t1.rightConnectors);

		// catch just in case; should return out earlier
		Debug.LogWarning("CheckIfFit() was passed an invalid direction");
		return false;
	}

	private bool HasConnection(List<Connector> tileSide, List<Connector> other)
	{
		// check if there are overlapping connectors
		// check if both sides have the triangle
		if (tileSide.Contains(Connector.R) && other.Contains(Connector.R))
			return true;
		// check if both sides have the square
		if (tileSide.Contains(Connector.G) && other.Contains(Connector.G))
			return true;
		// check if both sides have the circle
		if (tileSide.Contains(Connector.B) && other.Contains(Connector.B))
			return true;

		// if it hasn't returned true yet, then its false
		return false;
	}

	/* - MAP HELPERS - */
	// Adds open indexes to provided list
	private void GetOpenNeighbors(ref List<Record> list, ref List<Record> closed, Vector2 origin)
	{
		// - Check coords around it to see if they're open - //
		int tempIndex;
		Vector2 testCoord;
		Record tempRec;

		// check above origin is inside grid
		testCoord = new Vector2(origin.x, origin.y + 1);
		tempIndex = MapCoord_To_Index(testCoord.x, testCoord.y);
		if (testCoord.y < dim)
		{
			// check its not already in the list AND not already in the closed list AND has no tile yet
			if (!InList(list, tempIndex) && !InList(closed, tempIndex) && map[tempIndex] == null)
			{
				// make new record
				tempRec = new Record(tempIndex, null, null, origin, testCoord);
				// add to list
				list.Add(tempRec);

				//Debug.Log("Added " + testCoord.ToString("F1") + " to open list while processing " + origin.ToString("F1"));
			}
		}

		// check below origin is inside grid
		testCoord = new Vector2(origin.x, origin.y - 1);
		tempIndex = MapCoord_To_Index(testCoord.x, testCoord.y);
		if (testCoord.y >= 0f)
		{
			// check its not already in the list AND has no tile yet
			if (!InList(list, tempIndex) && !InList(closed, tempIndex) && map[tempIndex] == null)
			{
				// make new record
				tempRec = new Record(tempIndex, null, null, origin, testCoord);
				// add to list
				list.Add(tempRec);

				//Debug.Log("Added " + testCoord.ToString("F1") + " to open list  while processing " + origin.ToString("F1"));
			}
		}

		// check right origin is inside grid
		testCoord = new Vector2(origin.x + 1, origin.y);
		tempIndex = MapCoord_To_Index(testCoord.x, testCoord.y);
		if (testCoord.x < dim)
		{
			// check its not already in the list AND has no tile yet
			if (!InList(list, tempIndex) && !InList(closed, tempIndex) && map[tempIndex] == null)
			{
				// make new record
				tempRec = new Record(tempIndex, null, null, origin, testCoord);
				// add to list
				list.Add(tempRec);

				//Debug.Log("Added " + testCoord.ToString("F1") + " to open list  while processing " + origin.ToString("F1"));
			}
		}

		// check left origin is inside grid
		testCoord = new Vector2(origin.x - 1, origin.y);
		tempIndex = MapCoord_To_Index(testCoord.x, testCoord.y);
		if (testCoord.x >= 0f)
		{
			// check its not already in the list AND has no tile yet
			if (!InList(list, tempIndex) && !InList(closed, tempIndex) && map[tempIndex] == null)
			{
				// make new record
				tempRec = new Record(tempIndex, null, null, origin, testCoord);
				// add to list
				list.Add(tempRec);

				//Debug.Log("Added " + testCoord.ToString("F1") + " to open list  while processing " + origin.ToString("F1"));
			}
		}
	}

	// Adds closed indexes to provided list
	private void GetClosedNeighbors(ref List<Record> list, Vector2 origin)
	{
		// - Check coords around it to see if they're open - //
		int tempIndex;
		Vector2 testCoord;
		Record tempRec;

		// check above origin is inside grid
		testCoord = new Vector2(origin.x, origin.y + 1);
		tempIndex = MapCoord_To_Index(testCoord.x, testCoord.y);
		if (testCoord.y < dim)
		{
			//Debug.Log("value of map coord" + testCoord + ": " + map[tempIndex]);
			// check its not already in the list AND has no tile yet
			if (!InList(list, tempIndex) && map[tempIndex] != null)
			{
				// make new record
				tempRec = new Record(tempIndex, null, map[tempIndex], origin, testCoord);
				// add to list
				list.Add(tempRec);
			}
		}

		// check below origin is inside grid
		testCoord = new Vector2(origin.x, origin.y - 1);
		tempIndex = MapCoord_To_Index(testCoord.x, testCoord.y);
		if (testCoord.y >= 0f)
		{
			//Debug.Log("value of map coord" + testCoord + ": " + map[tempIndex]);
			// check its not already in the list AND has no tile yet
			if (!InList(list, tempIndex) && map[tempIndex] != null)
			{
				// make new record
				tempRec = new Record(tempIndex, null, map[tempIndex], origin, testCoord);
				// add to list
				list.Add(tempRec);
			}
		}

		// check right origin is inside grid
		testCoord = new Vector2(origin.x + 1, origin.y);
		tempIndex = MapCoord_To_Index(testCoord.x, testCoord.y);
		if (testCoord.x < dim)
		{
			//Debug.Log("value of map coord" + testCoord + ": " + map[tempIndex]);
			// check its not already in the list AND has no tile yet
			if (!InList(list, tempIndex) && map[tempIndex] != null)
			{
				// make new record
				tempRec = new Record(tempIndex, null, map[tempIndex], origin, new Vector2(origin.x + 1, origin.y));
				// add to list
				list.Add(tempRec);
			}
		}

		// check right origin is inside grid
		testCoord = new Vector2(origin.x - 1, origin.y);
		tempIndex = MapCoord_To_Index(testCoord.x, testCoord.y);
		if (testCoord.x >= 0f)
		{
			//Debug.Log("value of map coord" + testCoord + ": " + map[tempIndex]);
			// check its not already in the list AND has no tile yet
			if (!InList(list, tempIndex) && map[tempIndex] != null)
			{
				// make new record
				tempRec = new Record(tempIndex, null, map[tempIndex], origin, new Vector2(origin.x - 1, origin.y));
				// add to list
				list.Add(tempRec);
			}
		}
	}

	bool InList(List<Record> list, int searchIndex)
	{
		for (int i = 0; i < list.Count; i++)
			if (list[i].index == searchIndex)
				return true;

		return false;
	}

	/* - CONVERSION HELPERS - */
	private int MapCoord_To_Index(float xCoord, float yPos)
	{
		return (int)(xCoord + yPos * dim);
	}

	private Vector2 Index_To_MapCoord(int index)
	{
		Vector2 coord = Vector2.zero;
		while (index >= dim)
		{
			index -= dim;
			coord.y++;
		}
		coord.x = index;

		return coord;
	}
}
