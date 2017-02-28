using System.Xml;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
//Latitude = Horizontal
//Longitude = Vertical



public class parserTest : MonoBehaviour {
	
	XmlDocument doc = new XmlDocument();
	List<Transform> wayObjects = new List<Transform>();
	//public Node n;
	public float x;
	public float y;
	public float boundsX= 41;
	public float boundsY= -96;
	
	public struct Node
	{
	
		public long id;
		public float lat, lon;
		
		public Node(long ID, float LAT, float LON)
		{
			id = ID;
			lat = LAT;
			lon = LON;
			//Debug.Log("ID: " + id + ", LAT: " + lat + ", LON: " + lon);
		}
	}
	
	public struct Way
	{
		public long id;
		public List<long> wnodes;
		
		public Way(long ID)
		{
			id = ID;
			wnodes = new List<long>();
		}
	}
	
	public List<Node> nodes = new List<Node>();
	public List<Way> ways = new List<Way>();

    void Start () 
	{
		doc.Load(new XmlTextReader("Assets/uno-map.osm"));
		XmlNodeList elemList = doc.GetElementsByTagName("node");
		foreach (XmlNode attr in elemList)
		{
            Debug.Log("PARSING -- " + "ID: " + long.Parse(attr.Attributes["id"].InnerText) + ", LAT: " + float.Parse(attr.Attributes["lat"].InnerText) + ", LON: " + float.Parse(attr.Attributes["lon"].InnerText));
            nodes.Add(new Node(long.Parse(attr.Attributes["id"].InnerText), float.Parse(attr.Attributes["lat"].InnerText), float.Parse(attr.Attributes["lon"].InnerText)));
		}
		
		XmlNodeList wayList = doc.GetElementsByTagName("way");
		int ct = 0;
		foreach (XmlNode node in wayList)
		{
            //load saved data here?
            XmlNodeList wayNodes = node.ChildNodes;
			ways.Add(new Way(long.Parse(node.Attributes["id"].InnerText)));
			foreach(XmlNode nd in wayNodes)
			{
				if(nd.Attributes[0].Name == "ref")
				{
					ways[ct].wnodes.Add(long.Parse(nd.Attributes["ref"].InnerText));
					Debug.Log(ways[ct].wnodes.Count);
				}
			}
			ct++;
		}
		for (int i = 0; i < ways.Count; i++)
		{
			wayObjects.Add(new GameObject("wayObject"+ ways[i].id).transform);
			wayObjects[i].gameObject.AddComponent<LineRenderer>();
            wayObjects[i].GetComponent<LineRenderer>().startWidth = 0.05f;
            wayObjects[i].GetComponent<LineRenderer>().endWidth = 0.05f;
            wayObjects[i].GetComponent<LineRenderer>().numPositions = ways[i].wnodes.Count;
			for (int j = 0; j < ways[i].wnodes.Count; j++)
			{
				
				foreach (Node nod in nodes)
				{
					if (nod.id == ways[i].wnodes[j])
					{
						Debug.Log("MATCH!");
						x = nod.lat;
						y = nod.lon;
					}
				}
				wayObjects[i].GetComponent<LineRenderer>().SetPosition(j, new Vector3((x-boundsX)*100,0,(y-boundsY)*100));
			}
		}
        // save wayObject list to avoid reparsing and setting components to render lines
        saveWayObjects(wayObjects);
	}

    public void saveWayObjects (List<Transform> wayObjects){
        string output = "C:\\wayObjects.xml";
        XmlSerializer ser = new XmlSerializer(typeof(List<Transform>));
        TextWriter fs = new StreamWriter(output);
        ser.Serialize(fs, wayObjects);
        Debug.Log("Writing wayObjects to: " + output + " file.");
        fs.Close();
    }
}
