using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum Connector
{
    INVALID = -1,
    R = 0,
    G = 1,
    B
}

public enum TileConnectorType
{
	INVALID = -1,
	EQUILAT = 0,
	ISOSCEL = 1,
	SCALENE
}

public struct Side
{
	public List<Connector> connectors;

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return base.ToString();
    }
}

public class Tile : MonoBehaviour
{
	[Tooltip("Uses types of triangles to determine how the connectors are oriented. Equilateral means all sides have same connectors etc.")]
	public TileConnectorType connectorDescription;

	[Header("Side Connectors")]
	public List<Connector> topConnectors;
	public List<Connector> rightConnectors;
	public List<Connector> bottomConnectors;
	public List<Connector> leftConnectors;

	//private List<Side> sides;

	// Start is called before the first frame update
	void Start()
	{
		// create sides using the connectors
		//sides = new List<Side>();
		//Side temp;
		//// create north/top side
		//temp = new Side();
		//northConnectors.CopyTo(temp.connectors, 0);
		//sides.Add(temp);
		//// create east/right side
		//temp = new Side();
		//eastConnectors.CopyTo(temp.connectors, 0);
		//sides.Add(temp);
		//// create south/bottom side
		//temp = new Side();
		//southConnectors.CopyTo(temp.connectors, 0);
		//sides.Add(temp);
		//// create west/left side
		//temp = new Side();
		//westConnectors.CopyTo(temp.connectors, 0);
		//sides.Add(temp);
	}

	// rotate tile n times
	public void Rotate(int n)
	{
		for (int i = 0; i < n; i++)
		{
			// hold one of the lists
			List<Connector> holder = topConnectors;

			// rotate connectors
			topConnectors = leftConnectors;
			leftConnectors = bottomConnectors;
			bottomConnectors = rightConnectors;
			rightConnectors = holder;
		}
	}
}
