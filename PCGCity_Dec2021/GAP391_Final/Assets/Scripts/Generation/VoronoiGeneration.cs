using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Delaunay;
using Delaunay.Geo;
using UnityEngine;

public struct Line
{
    public Vector2 a, b;
    public Line(Vector2 _a, Vector2 _b) { a = _a; b = _b; }
}

[System.Serializable]
public enum DistrictType
{
    kFinancial,
    kChina,
    kResidential,
    kIndustrial,
    kShopping,
    kTourist
}
public class Cell
{
	public Vector2 m_center;
	public HashSet<Edge> m_edges = new HashSet<Edge>();
	public HashSet<Vector2> m_corners = new HashSet<Vector2>();
	public List<Vector2> m_cornersOrdered = new List<Vector2>();
	public HashSet<Cell> m_neighbors = new HashSet<Cell>();
    public Vector2 m_centroid;
    public Vector3[] m_districtBoxCorners = new Vector3[4];
    public List<Line> m_blockRoads = new List<Line>();
    public List<CityBlock> m_cityBlocks = new List<CityBlock>();
    public DistrictType m_districtType;

    public void Kill()
    {
        foreach (CityBlock block in m_cityBlocks)
        {
            block.Kill();
        }
    }
    //Walks in a circle Clowckwise around centroid to find vertices in order
    public void ReorderCornersCW()
    {
        float pointDist = Vector2.Distance(GetFarthestPoint(m_centroid), m_centroid);
        Vector3 dir = Vector3.forward;
        Vector3 testPoint = dir * pointDist + new Vector3(m_centroid.x, 0, m_centroid.y);

        Vector2 lastNearest = new Vector3(0, 0);
        Vector2 currentNearest = new Vector2(0, 0);
        for (int i = 0; i < 360; i++)
        {
            if (i == 0)
            {
                currentNearest = lastNearest = GetNearestPoint(testPoint);
                m_cornersOrdered.Add(lastNearest);
                continue;
            }

            dir = (Vector3)(Quaternion.Euler(0,i,0) * Vector3.forward);
            dir = dir.normalized;
            testPoint = (dir * pointDist) + new Vector3(m_centroid.x,0,m_centroid.y);

            currentNearest = GetNearestPoint(testPoint);
            if (!Mathf.Approximately(currentNearest.x, lastNearest.x) && !Mathf.Approximately(currentNearest.y, lastNearest.y))
            {
                if (m_cornersOrdered.Contains(currentNearest))
                    continue;
                
                lastNearest = currentNearest;
                m_cornersOrdered.Add(lastNearest);
            }
        }
    }
    private Vector2 GetFarthestPoint(Vector2 from)
    {
        Vector2 farthest = new Vector2(0, 0);
        bool firstPass = true;
        foreach (Vector2 corn in m_corners)
        {
            if (firstPass)
            {
                firstPass = false;
                farthest = corn;
                continue;
            }

            if (Vector2.Distance(from, corn) > Vector2.Distance(from, farthest))
            {
                farthest = corn;
            }
        }

        return farthest;
    }
    private Vector2 GetNearestPoint(Vector3 from)
    {
        Vector2 fromv2 = new Vector2(from.x, from.z);

        Vector2 nearest = new Vector2(0, 0);
        bool firstPass = true;
        foreach (Vector2 corn in m_corners)
        {
            if (firstPass)
            {
                firstPass = false;
                nearest = corn;
                continue;
            }

            if (Vector2.Distance(fromv2, corn) < Vector2.Distance(fromv2, nearest))
            {
                nearest = corn;
            }
        }

        return nearest;
    }
    public Vector2 FindCentroid()
    {
        List<Vector2> path = m_corners.ToList();
        Vector2 result = path.Aggregate(Vector2.zero, (current, point) => current + point);
        result /= path.Count;

        return result;
    }
    public void Shrink(float shrinkFactor)
    {
        if (m_centroid == null)
            return;

        HashSet<Vector2> newCorners = new HashSet<Vector2>();

        //pull corner towards centroid
        foreach (Vector2 corner in m_corners)
        {
            Vector2 dir = corner - m_centroid;
            dir = dir.normalized;

            Vector2 newPos = corner - (dir * shrinkFactor);

            newCorners.Add(newPos);
        }
        //Update Corners
        m_corners.Clear();
        m_corners = newCorners;

        //Adjust Edges
        foreach (Edge edge in m_edges)
        {
            //pull edge vertexes towards centroid
            
            //left
            Vertex vl = edge.leftVertex;
            if (vl != null)
            {
                Vector2 l = new Vector2(vl.x, vl.y);
                Vector2 dir = l - m_centroid;
                dir = dir.normalized;
                l = l - (dir * shrinkFactor);
                edge.leftVertex = new Vertex(l.x, l.y);
            }
            else
            {
                Vector2 l = (Vector2)edge.clippedEnds[Delaunay.LR.Side.LEFT];
                Vector2 dir = l - m_centroid;
                dir = dir.normalized;
                l = l - (dir * shrinkFactor);
                edge.leftVertex = new Vertex(l.x, l.y);
            }

            //right
            Vertex vr = edge.rightVertex;
            if (vr != null)
            {
                Vector2 r = new Vector2(vr.x, vr.y);
                Vector2 dir = r - m_centroid;
                dir = dir.normalized;
                r = r - (dir * shrinkFactor);
                edge.rightVertex = new Vertex(r.x, r.y);
            }
            else
            {
                Vector2 r = (Vector2)edge.clippedEnds[Delaunay.LR.Side.RIGHT];
                Vector2 dir = r - m_centroid;
                dir = dir.normalized;
                r = r - (dir * shrinkFactor);
                edge.rightVertex = new Vertex(r.x, r.y);
            }

            //Connect to nearest points
            //find nearest corner
            Vector2 nearestLeft = new Vector2(0,0);
            Vector2 nearestRight = new Vector2(0,0);

            Vector2 lVert = new Vector2(edge.leftVertex.x, edge.leftVertex.y);
            Vector2 rVert = new Vector2(edge.rightVertex.x, edge.rightVertex.y);
            foreach (Vector2 corner in m_corners)
            {
                if (nearestLeft == new Vector2(0, 0))
                    nearestLeft = corner;
                if (nearestRight == new Vector2(0, 0))
                    nearestRight = corner;

                if (Vector2.Distance(lVert, corner) < Vector2.Distance(lVert, nearestLeft))
                {
                    nearestLeft = corner;
                }
                if (Vector2.Distance(rVert, corner) < Vector2.Distance(rVert, nearestRight))
                {
                    nearestRight = corner;
                }
            }

            edge.leftVertex = new Vertex(nearestLeft.x, nearestLeft.y);
            edge.rightVertex = new Vertex(nearestRight.x, nearestRight.y);

        }
    }
}

public class VoronoiGeneration : MonoBehaviour
{
    [Header("Voronoi Tuning")]
	[SerializeField] private int m_pointCount = 300;
    [SerializeField] protected float m_mapWidth = 100;
    [SerializeField] protected float m_mapHeight = 50;


	protected List<Site> m_sites = null;
	protected List<Vector2> m_centers = null;
	protected List<LineSegment> m_edges = null;
	protected List<Vector2> m_corners = null;
	protected List<LineSegment> m_spanningTree;
	protected List<LineSegment> m_delaunayTriangulation;

	protected List<Cell> m_cells = new List<Cell>();
	protected Dictionary<Site, Cell> m_cellMap = new Dictionary<Site, Cell>();

	public virtual void Awake()
	{
		GenerateVoronoiDiagram();
	}

    public virtual void Update()
	{
        //
	}

    public virtual void GenerateVoronoiDiagram()
	{
        List<uint> colors = new List<uint>();
        m_centers = new List<Vector2>();
        m_corners = new List<Vector2>();
        m_sites = new List<Site>();
        m_cells.Clear();
        m_cellMap.Clear();

        for (int i = 0; i < m_pointCount; i++)
        {
            colors.Add(0);
            m_centers.Add(new Vector2(
                    UnityEngine.Random.Range(0, m_mapWidth),
                    UnityEngine.Random.Range(0, m_mapHeight))
            );
        }
        Delaunay.Voronoi v = new Delaunay.Voronoi(m_centers, colors, new Rect(0, 0, m_mapWidth, m_mapHeight));
        m_edges = v.VoronoiDiagram();

        //Get Cells
        m_sites = v._sites._sites;

        foreach (Site site in m_sites)
        {
            Cell temp = new Cell();
            temp.m_center = site.Coord;
            foreach (Edge line in site.edges)
            {
                if (!line.visible)
                    continue;
                line.ClipVertices(v.plotBounds); // clip ends
                temp.m_edges.Add(line);

                temp.m_corners.Add((Vector2)line.clippedEnds[Delaunay.LR.Side.LEFT]);
                temp.m_corners.Add((Vector2)line.clippedEnds[Delaunay.LR.Side.RIGHT]);

            }
            temp.m_centroid = temp.FindCentroid();

            m_cells.Add(temp);
            m_cellMap.Add(site, temp);
        }

        foreach (Cell cell in m_cells)
        {
            foreach (Edge line in cell.m_edges)
            {
                cell.m_neighbors.Add(m_cellMap[line.leftSite]);
                cell.m_neighbors.Add(m_cellMap[line.rightSite]);
            }
        }
        //End Get Cells

        m_spanningTree = v.SpanningTree(KruskalType.MAXIMUM);
        m_delaunayTriangulation = v.DelaunayTriangulation();
    }

    public virtual void DrawGizmos()
    {

        if (m_edges != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < m_edges.Count; i++)
            {
                Vector2 left = (Vector2)m_edges[i].p0;
                Vector3 left3 = new Vector3(left.x, 0, left.y);
                Vector2 right = (Vector2)m_edges[i].p1;
                Vector3 right3 = new Vector3(right.x, 0, right.y);
                Gizmos.DrawLine(left3, right3);
            }
        }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(0, 0, 0), new Vector3(0, 0, m_mapHeight));
        Gizmos.DrawLine(new Vector3(0, 0, 0), new Vector3(m_mapWidth, 0, 0));
        Gizmos.DrawLine(new Vector3(m_mapWidth, 0, 0), new Vector3(m_mapWidth, 0, m_mapHeight));
        Gizmos.DrawLine(new Vector3(0, 0, m_mapHeight), new Vector3(m_mapWidth, 0, m_mapHeight));
    }

    void OnDrawGizmos()
    {
        DrawGizmos();
    }

    private void ConnectCellNeighbors()
    {
        foreach (Cell cell in m_cells)
        {
            foreach (Edge line in cell.m_edges)
            {
                cell.m_neighbors.Add(m_cellMap[line.leftSite]);
                cell.m_neighbors.Add(m_cellMap[line.rightSite]);
            }
        }
    }

}
