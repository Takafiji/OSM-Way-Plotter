using System.Xml;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using System.Xml.XPath;
//Latitude = Horizontal
//Longitude = Vertical


public class parserTest : MonoBehaviour {

    private const string wayNS = "OSM";     //ip of server and dir (later)
    private const string assetsDir = "Assets/";
    private const string cacheDir = assetsDir + "parsedMaps/";
    private const string map = "map-south_campus.osm";
    private const string greenMat = "OSM_Green-Path";
	//public Node n;
	public float x;
	public float y;
	public float boundsX = 41;
	public float boundsY = -96;
    

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
        public List<Node> nodeList;
        public string wayType;

		public Way(long ID, string TYPE)
		{
			id = ID;
			wnodes = new List<long>();
            nodeList = new List<Node>();
            wayType = TYPE;
        }
	}

    public Font font;
    public List<Node> nodes = new List<Node>();
	public List<Way> ways = new List<Way>();
    List<Transform> wayObjects = new List<Transform>();

    void Start ()
    {
        //parses exported OpenStreetMaps XML file and creates global list "ways" and "nodes"
        ways = parseOSM(assetsDir + map);

        nodes = ways[0].nodeList;
        //parseOSM_XML(assetsDir + map);
        //saves "ways" and "nodes" lists into custom XML file as a local cache
        saveWayList(ways, cacheDir + map + "-wayList.xml");
        //creates "wayObjects" based on way[x].id, does vector transform to create visual
        createWayObjects();
        //Debug.Log("COMPLEATE: ALL wayObjects CREATED!!");
        //setWayColorMaterial(greenMat, 0);

        createBuilding();
        // save wayObject list to avoid reparsing and setting components to render lines
        //saveWayObjects(wayObjects);
	}

    /* parseOSM(string)
     * Uses an XML document from OpenStreetMaps and parses "way" and "node" data using XPath.
     * Populates Way struct with ID, type of way (structure, street, sidewalk, etc...), latitude and longitude
     * of each node based on ID, and number of nodes in the Way.
     * @param string input: takes a string that identifies the directory and XML file to parse
     * @return wd: Way struct type, 
     * 
     */
    public List<Way> parseOSM(string input)
    {
        List<Way> wd = new List<Way>();
        List<Node> nd = new List<Node>();
        XmlDocument doc = new XmlDocument();
        doc.Load(input);
        XPathNavigator nav = doc.CreateNavigator();
        XPathNodeIterator xpni_node = nav.Select("/osm/node");

        //gather all the "nodes" with ID, LAT, LON
        int ct = 0;
        while (xpni_node.MoveNext())
        {
            nd.Add(new Node(
                long.Parse(xpni_node.Current.GetAttribute("id", xpni_node.Current.GetNamespace("id"))),
                float.Parse(xpni_node.Current.GetAttribute("lat", xpni_node.Current.GetNamespace("lat"))),
                float.Parse(xpni_node.Current.GetAttribute("lon", xpni_node.Current.GetNamespace("lon"))))
             );
            Debug.Log(ct + "_node --ID: " + nd[ct].id + " Lat: " + nd[ct].lat + " Lon: " + nd[ct].lon);
            ct++;
        }

        //gather all the "way" data 
        XPathNodeIterator xpni_way = nav.Select("/osm/way");
        ct = 0;
        while (xpni_way.MoveNext())
        {
            long wayID = long.Parse(xpni_way.Current.GetAttribute("id", xpni_way.Current.GetNamespace("id")));
            Debug.Log("Way ID: " + wayID);
            if (xpni_way.Current.HasChildren)
            {
                XPathNodeIterator xpni_nd = xpni_way.Current.SelectChildren("nd", xpni_way.Current.GetNamespace("way"));
                List<long> llnd = new List<long>();
                while (xpni_nd.MoveNext())
                {
                    llnd.Add(long.Parse(xpni_nd.Current.GetAttribute("ref", xpni_nd.Current.GetNamespace("ref"))));
                    //wd[ct].wnodes.Add(long.Parse(xpni_nd.Current.GetAttribute("ref", xpni_nd.Current.GetNamespace("ref"))));
                    Debug.Log(llnd.Count);
                }
                XPathNodeIterator xpni_tag = xpni_way.Current.SelectChildren("tag", xpni_way.Current.GetNamespace("way"));
                string wt = "";
                while (xpni_tag.MoveNext())
                {
                    if (xpni_tag.Current.GetAttribute("k", xpni_tag.Current.GetNamespace("k")).Contains("building"))
                    {
                        wt = "Structure";
                        Debug.Log(wt);
                    }
                    else if (xpni_tag.Current.GetAttribute("k", xpni_tag.Current.GetNamespace("k")).Contains("highway"))
                    {
                        wt = "Street";
                        Debug.Log(wt);
                    }
                }
                wd.Add(new Way(wayID, wt));
                wd[ct].wnodes.AddRange(llnd);
            }
            ct++;
        }

        //store "nodes" into the first "way" object in "nodeList"
        wd[0].nodeList.AddRange(nd);

        return wd; 
    }

    // store list of ways (Way struct) to XML document. Switch to JSON???!
    public bool saveWayList (List<Way> way, string output) {
        XmlWriter xwriter = null;
        XmlWriterSettings xWsettings = new XmlWriterSettings();
        xWsettings.Encoding = Encoding.UTF8;
        xWsettings.Indent = true;
        xWsettings.IndentChars = "\t";
        try
        {
            xwriter = XmlWriter.Create(output, xWsettings);
            
            using (xwriter)
            {
                xwriter.WriteStartElement("ListOfWays");
                for (int i = 0; i < way.Count; i++){
                    xwriter.WriteStartElement("xl", "Way", wayNS);
                    xwriter.WriteString(way[i].id.ToString());

                    for (int j = 0; j < way[i].wnodes.Count; j++)
                    {
                        for (int k = 0; k < nodes.Count; k++)
                        {
                            Node nd = nodes[k];
                            if (way[i].wnodes[j].Equals(nd.id))
                            {
                                xwriter.WriteStartElement("wnode");
                                xwriter.WriteAttributeString("xs", "nodeID", wayNS, nd.id.ToString());
                                xwriter.WriteAttributeString("xs", "nodeType", wayNS, way[i].wayType);
                                xwriter.WriteStartElement("Coords");
                                xwriter.WriteAttributeString("xl", "lat", wayNS, nd.lat.ToString());
                                xwriter.WriteAttributeString("xl", "lon", wayNS, nd.lon.ToString());
                                xwriter.WriteEndElement();
                                xwriter.WriteEndElement();
                            }
                        }
                        
                    }
                    xwriter.WriteFullEndElement();
                }
                xwriter.WriteFullEndElement();
            }
            xwriter.Close();
        }
        finally
        {
            if (xwriter != null)
            {
                xwriter.Close();
            }
        }
        Debug.Log("Writing ways to: " + output + " file.");
        return true;
    }

    /*public List<Way> loadWayList(string input)
    {
        List<Way> wd = new List<Way>();
        List<Node> nd = new List<Node>();
        //XmlReader xreader = XmlReader.Create(input);
        XPathDocument doc = new XPathDocument(input);
        XPathNavigator nav_wayID = doc.CreateNavigator();

        //XPathNodeIterator xpni_

        return wd;

    }*/

    public void createWayObjects()
    {
        Material waypointMat = Resources.Load("OSM_Node_Yellow.mat", typeof(Material)) as Material;
        Texture2D waypoint = new Texture2D(128, 128);
        //GetComponent<Renderer>().material.mainTexture = waypoint;
        for (int i = 0; i < ways.Count; i++)
        {
            wayObjects.Add(new GameObject("wayObject" + ways[i].id).transform);
            wayObjects[i].gameObject.AddComponent<LineRenderer>();
            wayObjects[i].GetComponent<LineRenderer>().startWidth = 0.005f;
            wayObjects[i].GetComponent<LineRenderer>().endWidth = 0.005f;
            wayObjects[i].GetComponent<LineRenderer>().numPositions = ways[i].wnodes.Count;

            //loop through all nodes in each way
            for (int j = 0; j < ways[i].wnodes.Count; j++)
            {
                foreach (Node nod in nodes)
                {
                    if (nod.id == ways[i].wnodes[j])
                    {
                        
                        x = nod.lat;
                        y = nod.lon;
                        Debug.Log("WayID: " + ways[i].id + " wnode: " + ways[i].wnodes[j] + " X: " + x.ToString() + " Y: " + y.ToString());
                        // works and optimal, needs to only draw material between nodes.
                        switch (ways[i].wnodes[j])
                        {
                            case 1669126349:
                                setWayColorMaterial(i, greenMat);
                                break;
                            case 134196309:
                                setWayColorMaterial(i, greenMat);
                                break;
                            case 1129105971:
                                setWayColorMaterial(i, greenMat);
                                break;
                        }
                    }
                }
                wayObjects[i].GetComponent<LineRenderer>().SetPosition(j, new Vector3((x - boundsX) * 100, 0, (y - boundsY) * 100));
                /*foreach (Node nd in ways[0].nodeList)
                {
                    wayObjects[i].GetComponent<GUIText>().HitTest(new Vector3((ways[0].nodeList[j].lat - boundsX) * 100, 0, ((ways[0].nodeList[j].lon - boundsY) * 100)));
                    j++;
                }*/
            }
        }
    }

    public void setWayColorMaterial(int i, string colorMat)
    {
        Material greenPathMat = Resources.Load(colorMat, typeof(Material)) as Material;

        wayObjects[i].GetComponent<LineRenderer>().material = greenPathMat;
        /*for (int i = 0; i < ways.Count; i++)
        {
            Debug.Log("Count --" + i + ": wayObject" + ways[i].id);
            switch (ways[i].id) {
                case 352607471:
                    Debug.Log("APPLYING GREEN MATERIAL!");
                    wayObjects[i].GetComponent<LineRenderer>().material = greenPathMat;
                    break;
                case 175843858:
                    
                    if (nodeIntersection(i, 134196309))
                    {
                        Debug.Log("APPLYING GREEN MATERIAL!");
                        wayObjects[i].GetComponent<LineRenderer>().material = greenPathMat;
                    }
                    break;
            }
        }*/
    }

    public void createBuilding()
    {
        Material greyMat = Resources.Load("OSM_Grey", typeof(Material)) as Material;

        for (int i = 0; i < ways.Count; i++)
        {
            for (int j = 0; j < ways[i].wnodes.Count; j++)
            {
                if (ways[i].id == 415686865)
                {
                    foreach (Node nd in nodes)
                    {
                        //scott court building
                        //  bottom right           mid right              mid right corner       top right              top left               bottom left            close point
                        if (nd.id == 4166768860 || nd.id == 4166768881 || nd.id == 4166768880 || nd.id == 4166768884 || nd.id == 4166768885 || nd.id == 4166768861 || nd.id == 4166768860)
                        {
                            Debug.Log("Count --" + i + ": wayObject" + ways[i].id);
                            wayObjects[i].GetComponent<LineRenderer>().material = greyMat;
                            //wayObjects[i].
                        }
                    }
                }
            }
        }
    }

    public bool nodeIntersection(int i, long node)
    {
        for (int j = 0; j < ways[i].wnodes.Count; j++)
        {
            foreach (Node nod in nodes)
            {
                if (nod.id == node)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
