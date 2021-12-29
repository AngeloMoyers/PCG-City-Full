using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class CityBlock : MonoBehaviour
{
    [Header("BuldingPrefabs")]
    GameObject[] m_baseOptions;
    GameObject[] m_midLevelOptions;
    GameObject[] m_roofOptions;
    [SerializeField] GameObject[] m_doorOptions;
    [SerializeField] GameObject[] m_windowOptions;
    [Header("Tuning")]
    [SerializeField] float m_sidewalkOffset = 0.1f;
    [SerializeField] float m_buildingOffset = 0.0f;
    [SerializeField] float  m_minBuildingHeight = 1f;
    [SerializeField] float  m_maxBuildingHeight = 6f;
    [SerializeField] float m_financialScaleFactor = 1.5f;
    [SerializeField] float m_residentialScaleFactor = 0.6f;
    [SerializeField] float m_industrialScaleFactor = 0.4f;

    [Header("OtherPrefabs")]
    [SerializeField] GameObject m_sidewalkPrefab;
    [Header("Debug")]
    [SerializeField] bool drawGizmos = false;
    [Header("BuildingMaterial")]
    [SerializeField] Material m_buildingBaseColorMat;
    [SerializeField] Material m_grassMat;
    [Header("AI")]
    public AIManagerScript m_aiManager;

    //0 = BL, 1 = BR, 2 = TL, 3 = TR
    Vector3[] m_corners = new Vector3[4];
    Vector3[] m_workingCorners = new Vector3[4];

    public NoiseMap m_noiseMap;
    public DistrictType m_type;
    public Cell m_owningCell;

    public List<GameObject> m_buildings = new List<GameObject>();

    public Dictionary<DistrictType, List<Color>> m_districtColorMap = new Dictionary<DistrictType, List<Color>>();
    public BuildingPrefabHandler m_prefabHandler;

    private bool m_shrinkAlreadyCalled = false;
    private PolygonDrawer m_polyDrawer = null;
    private GameObject m_sidewalkObj;
    private GameObject m_polyObj;

    //To use, Set corners and init

    public void SetCorners(Vector3[] _corners) { m_corners = _corners; }
    public void Init()
    {
        SpawnSidewalk();

        //Working corners are slightly offset from sidewalk
        SetUpWorkingCorners();

        SpawnBuildings();
    }

    public void Kill()
    {
        Destroy(this.gameObject);
    }

    private void DestroyAllBuildings()
    {
        foreach (var building in m_buildings)
        {
            Destroy(building);
        }
        Destroy(m_sidewalkObj);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = Color.white;
        Gizmos.DrawLine(m_corners[0], m_corners[1]);
        Gizmos.DrawLine(m_corners[0], m_corners[2]);
        Gizmos.DrawLine(m_corners[2], m_corners[3]);
        Gizmos.DrawLine(m_corners[1], m_corners[3]);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(m_workingCorners[0], m_workingCorners[1]);
        Gizmos.DrawLine(m_workingCorners[0], m_workingCorners[2]);
        Gizmos.DrawLine(m_workingCorners[2], m_workingCorners[3]);
        Gizmos.DrawLine(m_workingCorners[1], m_workingCorners[3]);
    }

    private void GenerateBuilding(Vector3 position, Quaternion rotation, int levels, float scale)
    {
        //assign proper selections
        switch (m_type)
        {
            case DistrictType.kFinancial:
                m_baseOptions = m_prefabHandler.m_financialBase;
                m_midLevelOptions = m_prefabHandler.m_financialMid;
                m_roofOptions = m_prefabHandler.m_financialRoof;
                break;
            case DistrictType.kResidential:
                m_baseOptions = m_prefabHandler.m_residentialBase;
                m_midLevelOptions = m_prefabHandler.m_residentialMid;
                m_roofOptions = m_prefabHandler.m_residentialRoof;
                break;
            case DistrictType.kChina:
                m_baseOptions = m_prefabHandler.m_chinaBase;
                m_midLevelOptions = m_prefabHandler.m_chinaMid;
                m_roofOptions = m_prefabHandler.m_chinaRoof;
                break;
            default:
                m_baseOptions = m_prefabHandler.m_generalBase;
                m_midLevelOptions = m_prefabHandler.m_generalMid;
                m_roofOptions = m_prefabHandler.m_generalRoof;
                break;
        }


        GameObject door = m_doorOptions[Random.Range(0, m_doorOptions.Length)];
        GameObject window = m_windowOptions[Random.Range(0, m_windowOptions.Length)];
        GameObject buildingBase = m_baseOptions[Random.Range(0, m_baseOptions.Length)];
        GameObject midLevel = m_midLevelOptions[Random.Range(0, m_midLevelOptions.Length)];
        GameObject roof = m_roofOptions[Random.Range(0, m_roofOptions.Length)];

        GameObject parentObj = new GameObject();
        parentObj.name = "Building";
        parentObj.transform.parent = this.transform;
        parentObj.AddComponent<MeshFilter>();
        parentObj.AddComponent<MeshRenderer>();


        var baseObj = Instantiate(buildingBase, position, rotation, parentObj.transform);
        baseObj.transform.localScale *= scale;
        Vector3 newPos = baseObj.transform.position;
        newPos.y += baseObj.transform.localScale.y / 2;
        baseObj.transform.position = newPos;

        var baseScript = baseObj.GetComponent<BuildingBase>();
        baseScript.AddObjectToDictionary(baseObj);
        baseScript.GetChildRecursive(baseObj);
        baseScript.SpawnDoor(door);
        baseScript.SpawnWindows(window);

        //Do math to spawn midLevels on top of base(?)
        var midLevelObj = Instantiate(midLevel, baseScript.m_nextLevelTransform.position, baseScript.m_nextLevelTransform.rotation, parentObj.transform);
        midLevelObj.transform.localScale *= scale;
        var midLevelScript = midLevelObj.GetComponent<BuildingMidlevel>();
        baseScript.AddObjectToDictionary(midLevelObj);
        baseScript.GetChildRecursive(midLevelObj);
        midLevelScript.m_buildingBase = baseScript;
        midLevelScript.SpawnWindows(window);

        GameObject tempMidLevelObj = midLevelObj;
        BuildingMidlevel tempMidLevelScript = midLevelScript;
        for (int i = 0; i < levels; ++i)
        {
            var midLevelObjTemp = Instantiate(midLevel, tempMidLevelScript.m_nextLevelTransform.position, tempMidLevelScript.m_nextLevelTransform.rotation, parentObj.transform);
            midLevelObjTemp.transform.localScale *= scale;
            var midLevelScriptTemp = midLevelObjTemp.GetComponent<BuildingMidlevel>();
            baseScript.AddObjectToDictionary(midLevelObjTemp);
            baseScript.GetChildRecursive(midLevelObjTemp);
            midLevelScriptTemp.m_buildingBase = baseScript;
            midLevelScriptTemp.SpawnWindows(window);

            tempMidLevelObj = midLevelObjTemp;
            tempMidLevelScript = midLevelScriptTemp;
        }

        //Spawn Roof
        var roofObj = Instantiate(roof, tempMidLevelScript.m_nextLevelTransform.position, tempMidLevelScript.m_nextLevelTransform.rotation, parentObj.transform);
        roofObj.transform.localScale *= scale;
        var roofScript = roofObj.GetComponent<BuildingRoof>();
        baseScript.AddObjectToDictionary(roofObj);
        baseScript.GetChildRecursive(roofObj);
        roofScript.m_buildingBase = baseScript;
        roofScript.SpawnDoor(door);
        roofScript.SpawnWindows(window);

        //Sort and combine meshes
        foreach (var pair in baseScript.m_objectsByColorLists)
        {
            Color temp = pair.Key;

            GameObject collectionParent = new GameObject();
            collectionParent.name = "CollectionParent";
            collectionParent.transform.parent = parentObj.transform;
            collectionParent.AddComponent<MeshFilter>();
            collectionParent.AddComponent<MeshRenderer>();

            bool isBuildingBase = false;
            if (temp == m_buildingBaseColorMat.color)
                isBuildingBase = true;

            foreach (var item in pair.Value)
            {
                item.transform.parent = collectionParent.transform;
            }

            CombineMeshes(collectionParent, isBuildingBase);
            collectionParent.GetComponent<MeshRenderer>().material = parentObj.GetComponent<MeshRenderer>().material = baseObj.GetComponent<MeshRenderer>().material;
            collectionParent.GetComponent<MeshRenderer>().material.color = (isBuildingBase) ? m_districtColorMap[m_type][Random.RandomRange(0, m_districtColorMap[m_type].Count)] : temp;
            collectionParent.transform.tag = "Building";

            if (isBuildingBase)
            {
                var rb = collectionParent.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.freezeRotation = true;

                //navMesh
                var obs = collectionParent.AddComponent<NavMeshObstacle>();
                obs.shape = NavMeshObstacleShape.Box;
                obs.carving = true;
                obs.carveOnlyStationary = true;

                collectionParent.layer = 11;

                if (m_type == DistrictType.kResidential || m_type == DistrictType.kChina)
                {
                    m_aiManager.m_homeBuildings.Add(collectionParent);
                }
                else if (m_type != DistrictType.kResidential)
                {
                    m_aiManager.m_workBuildings.Add(collectionParent);
                }

                var collecionScript = collectionParent.AddComponent<CollectionBase>();
                collecionScript.m_originalPosition = position;
            }
            else
            {
                //Set to Cull layer
                collectionParent.layer = 31;
            }

            m_buildings.Add(parentObj);
        }
    }

    private void CombineMeshes(GameObject obj, bool addCollider)
    {
        //Termp set positio to zero for matrix math simplicity
        Vector3 position = obj.transform.position;
        obj.transform.position = Vector3.zero;

        //Get al mesh filters and combine
        MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] comb = new CombineInstance[meshFilters.Length];

        for (int i = 0; i < meshFilters.Length; ++i)
        {
            comb[i].mesh = meshFilters[i].sharedMesh;
            comb[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);
        }

        obj.transform.GetComponent<MeshFilter>().mesh = new Mesh();
        obj.transform.GetComponent<MeshFilter>().mesh.CombineMeshes(comb, true, true);
        obj.transform.gameObject.SetActive(true);

        //return to original position
        obj.transform.position = position;

        //Add collider to mesh
        if (addCollider)
            obj.AddComponent<MeshCollider>();
    }

    private void SpawnSidewalk()
    {
        if (m_sidewalkPrefab != null)
        {
            m_sidewalkObj = Instantiate(m_sidewalkPrefab, transform);
            m_sidewalkObj.transform.position = new Vector3(
                m_corners[0].x + (m_corners[1].x - m_corners[0].x) / 2,
                m_corners[0].y,
                m_corners[0].z + (m_corners[2].z - m_corners[0].z) / 2);
        }
    }
    private void SetUpWorkingCorners()
    {
        m_workingCorners[0] = new Vector3(m_corners[0].x + m_sidewalkOffset, 0, m_corners[0].z + m_sidewalkOffset); ;
        m_workingCorners[1] = new Vector3(m_corners[1].x - m_sidewalkOffset, 0, m_corners[1].z + m_sidewalkOffset); ;
        m_workingCorners[2] = new Vector3(m_corners[2].x + m_sidewalkOffset, 0, m_corners[2].z - m_sidewalkOffset); ;
        m_workingCorners[3] = new Vector3(m_corners[3].x - m_sidewalkOffset, 0, m_corners[3].z - m_sidewalkOffset); ;
    }

    private void SpawnBuildings()
    {
        //divide area by building size + building offset, spawn buildings
        float width = m_workingCorners[1].x - m_workingCorners[0].x;
        float height = m_workingCorners[2].z - m_workingCorners[0].z;

        int buildingWidth = (int)((width / 2) - m_sidewalkOffset);

        //Spawn buildings starting from bottom left
        Vector3 currentSpawnPointBottom = m_workingCorners[0];
        currentSpawnPointBottom.x += ((buildingWidth + m_buildingOffset) / 2) + m_sidewalkOffset + m_buildingOffset;
        currentSpawnPointBottom.z += ((buildingWidth + m_buildingOffset) / 2) + m_sidewalkOffset;

        Vector3 currentSpawnPointTop = m_workingCorners[2];
        currentSpawnPointTop.x += ((buildingWidth + m_buildingOffset) / 2) + m_sidewalkOffset + m_buildingOffset;
        currentSpawnPointTop.z -= ((height / 2) - m_buildingOffset) / 2 + m_sidewalkOffset;

        for (int j = 0; j < 2; ++j)
        {
            float modifier = 1f;
            if (m_type == DistrictType.kFinancial)
                modifier = m_financialScaleFactor;
            else if (m_type == DistrictType.kResidential)
                modifier = m_residentialScaleFactor;
            else if (m_type == DistrictType.kIndustrial)
                modifier = m_industrialScaleFactor;

            GenerateBuilding(currentSpawnPointBottom, Quaternion.identity, (int)((m_noiseMap.heightMap[(int)currentSpawnPointBottom.x, (int)currentSpawnPointBottom.z] * (m_maxBuildingHeight - m_minBuildingHeight) + m_minBuildingHeight) * modifier), buildingWidth);

            currentSpawnPointBottom.x += (buildingWidth + m_buildingOffset);

            GenerateBuilding(currentSpawnPointTop, Quaternion.AngleAxis(180, Vector3.up) * Quaternion.identity, (int)((m_noiseMap.heightMap[(int)currentSpawnPointTop.x, (int)currentSpawnPointTop.z] * (m_maxBuildingHeight - m_minBuildingHeight) + m_minBuildingHeight) * modifier), buildingWidth);

            currentSpawnPointTop.x += (buildingWidth + m_buildingOffset);
        }
    }

    public void Shrink()
    {
        //check all of block lines vs cell lines if intersect. if no, just delete, is fully enveloped
        // if yes, create points at both intersections. 
        // using sets of new points, create new lines
        // check all points if on bad side of new lines and delete
        //reorder new points CW
        //draw poly
        DestroyAllBuildings();

        if (m_shrinkAlreadyCalled)
            return;

        if (!m_shrinkAlreadyCalled)
            m_shrinkAlreadyCalled = true;

        List<Line> lines = new List<Line>();
        //4 box lines
        lines.Add(new Line(new Vector2(m_corners[0].x, m_corners[0].z), new Vector2(m_corners[1].x, m_corners[1].z)));
        lines.Add(new Line(new Vector2(m_corners[0].x, m_corners[0].z), new Vector2(m_corners[2].x, m_corners[2].z)));
        lines.Add(new Line(new Vector2(m_corners[3].x, m_corners[3].z), new Vector2(m_corners[2].x, m_corners[2].z)));
        lines.Add(new Line(new Vector2(m_corners[3].x, m_corners[3].z), new Vector2(m_corners[1].x, m_corners[1].z)));

        //Cell border lines
        var cellCorners = m_owningCell.m_cornersOrdered;
        List<Line> cellLines = new List<Line>();
        for (int i = 0; i < cellCorners.Count; ++i)
        {
            Vector2 vec = cellCorners[i];
            Vector2 vec2 = (i == cellCorners.Count - 1) ? cellCorners[0] : cellCorners[i + 1];
            cellLines.Add(new Line(vec, vec2));
        }

        int numIntersections = 0;
        List<Vector2> newPoints = new List<Vector2>();
        List<Line> intersectingLines = new List<Line>();
        //Check all box lines vs cell lines
        foreach (Line cellLine in cellLines)
        {
            List<Vector2> intersections = new List<Vector2>();
            foreach (Line blockLine in lines)
            {
                //if lines intersecct, create point at intersection
                //https://www.geeksforgeeks.org/check-if-two-given-line-segments-intersect/
                if (LinesIntersect(cellLine, blockLine))
                {
                    numIntersections++;
                    //find where lines intersect
                    bool linesIntersect = false;
                    bool segsIntersect = false;
                    Vector2 intersection = Vector2.zero;
                    Vector2 closestCell = Vector2.zero;
                    Vector2 closestBlock = Vector2.zero;
                    FindIntersection(cellLine.a, cellLine.b, blockLine.a, blockLine.b, out linesIntersect, out segsIntersect, out intersection, out closestCell, out closestBlock);

                    if (segsIntersect)
                    {
                        newPoints.Add(intersection);
                        intersectingLines.Add(cellLine);
                    }
                }
            }

        }
        //if no intersections at all, just kill, its fully outside of cell
        if (numIntersections == 0)
        {
            Kill();
            return;
        }

        //Check all points against all lines, deleting ones on "bad" side
        foreach (Vector3 corner in m_corners)
        {
            newPoints.Add((new Vector2(corner.x, corner.z)));           
        }

        //Get good side of line, remove points on bad sides
        List<Vector2> pointsToRemove = new List<Vector2>();
        int it = 0;
        foreach (Vector2 point in newPoints)
        {
            foreach (Line line in  intersectingLines)
            {
                bool centroidIsLeft = isLeft(line.a, line.b, m_owningCell.m_centroid);
                bool pointIsLeft = isLeft(line.a, line.b, point);

                if (centroidIsLeft != pointIsLeft)
                {
                    pointsToRemove.Add(point);
                    break;
                }

            }
            it++;
        }
        //remove points
        foreach (var p in pointsToRemove)
        {
            newPoints.Remove(p);
        }

        //newPoints now contains all good points.
        //Reorder new points clockwise, generate polygon drawer, and draw
        if (newPoints.Count < 4)
            return;

        var orderedGoodPoints = ReorderCornersCW(newPoints);
        if (orderedGoodPoints.Count != newPoints.Count)
        {
            Debug.Log("lists are not same size");
        }


        m_polyObj = new GameObject();
        m_polyObj.name = "Grass Patch";
        
        
        m_polyDrawer = m_polyObj.AddComponent<PolygonDrawer>();
        m_polyDrawer.Init();
        m_polyDrawer.m_extrudeDistance = 0.2f;
        m_polyDrawer.isVisible = true;
        m_polyDrawer.convex = true;
        m_polyDrawer.vertices = new Vector3[orderedGoodPoints.Count];
        for (int i = 0; i < orderedGoodPoints.Count; ++i)
        {
            m_polyDrawer.vertices[i] = new Vector3(orderedGoodPoints[i].x, -.28f, orderedGoodPoints[i].y);
        }
        m_polyObj.GetComponent<MeshRenderer>().material = m_grassMat;
        if (Random.RandomRange(0,2) < 1)
        {
            m_polyObj.GetComponent<MeshRenderer>().material.color = Color.grey;
        }
        m_polyDrawer.isActive = true;
        m_polyDrawer.deactivateAfterTime = false;

        m_polyObj.transform.parent = this.transform;
    }

    //check centerpoint first
    private List<Vector2> ReorderCornersCW(List<Vector2> points)
    {
        Vector2 centroid = points.Aggregate(Vector2.zero, (current, point) => current + point);
        centroid /= points.Count;

        List<Vector2> orderedPoints = new List<Vector2>();

        float pointDist = Vector2.Distance(GetFarthestPoint(centroid, points), centroid);
        Vector3 dir = Vector3.forward;
        Vector3 testPoint = dir * pointDist + new Vector3(centroid.x, 0, centroid.y);

        Vector2 lastNearest = new Vector3(0, 0);
        Vector2 currentNearest = new Vector2(0, 0);
        for (int i = 0; i < 360; i++)
        {
            if (i == 0)
            {
                currentNearest = lastNearest = GetNearestPoint(testPoint, points);
                orderedPoints.Add(lastNearest);
                continue;
            }

            dir = (Vector3)(Quaternion.Euler(0, i, 0) * Vector3.forward);
            dir = dir.normalized;
            testPoint = (dir * pointDist) + new Vector3(centroid.x, 0, centroid.y);

            currentNearest = GetNearestPoint(testPoint, points);
            if (!Mathf.Approximately(currentNearest.x, lastNearest.x) || !Mathf.Approximately(currentNearest.y, lastNearest.y))
            {
                if (orderedPoints.Contains(currentNearest))
                    continue;

                lastNearest = currentNearest;
                orderedPoints.Add(lastNearest);
            }
        }
        return orderedPoints;
    }

    private Vector2 GetFarthestPoint(Vector2 from, List<Vector2> pointsList)
    {
        Vector2 farthest = new Vector2(0, 0);
        bool firstPass = true;
        foreach (Vector2 corn in pointsList)
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

    private Vector2 GetNearestPoint(Vector3 from, List<Vector2> pointsList)
    {
        Vector2 fromv2 = new Vector2(from.x, from.z);

        Vector2 nearest = new Vector2(0, 0);
        bool firstPass = true;
        foreach (Vector2 corn in pointsList)
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

    bool LinesIntersect(Line a, Line b)
    {
        float o1 = orientation(a.a, a.b, b.a);
        float o2 = orientation(a.a, a.b, b.b);
        float o3 = orientation(b.a, b.b, a.a);
        float o4 = orientation(b.a, b.b, a.b);

        // General case
        if (o1 != o2 && o3 != o4)
            return true;

        // Special Cases
        // p1, q1 and p2 are collinear and p2 lies on segment p1q1
        if (o1 == 0 && onSegment(a.a, b.a, a.b)) return true;

        // p1, q1 and q2 are collinear and q2 lies on segment p1q1
        if (o2 == 0 && onSegment(a.a, b.b, a.b)) return true;

        // p2, q2 and p1 are collinear and p1 lies on segment p2q2
        if (o3 == 0 && onSegment(b.a, a.a, b.b)) return true;

        // p2, q2 and q1 are collinear and q1 lies on segment p2q2
        if (o4 == 0 && onSegment(b.a, a.b, b.b)) return true;

        return false; // Doesn't fall in any of the above cases
    }

    float orientation(Vector2 p, Vector2 q, Vector2 r)
    {
        // See https://www.geeksforgeeks.org/orientation-3-ordered-points/
        // for details of below formula.
        float val = (q.y - p.y) * (r.x - q.x) -
                (q.x - p.x) * (r.y - q.y);

        if (val == 0) return 0; // collinear

        return (val > 0) ? 1 : 2; // clock or counterclock wise
    }
    static bool onSegment(Vector2 p, Vector2 q, Vector2 r)
    {
        if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) &&
            q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y))
            return true;

        return false;
    }

    private void FindIntersection(
    Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4,
    out bool lines_intersect, out bool segments_intersect,
    out Vector2 intersection,
    out Vector2 close_p1, out Vector2 close_p2)
    {
        // Get the segments' parameters.
        float dx12 = p2.x - p1.x;
        float dy12 = p2.y - p1.y;
        float dx34 = p4.x - p3.x;
        float dy34 = p4.y - p3.y;

        // Solve for t1 and t2
        float denominator = (dy12 * dx34 - dx12 * dy34);

        float t1 =
            ((p1.x - p3.x) * dy34 + (p3.y - p1.y) * dx34)
                / denominator;
        if (float.IsInfinity(t1))
        {
            // The lines are parallel (or close enough to it).
            lines_intersect = false;
            segments_intersect = false;
            intersection = new Vector2(float.NaN, float.NaN);
            close_p1 = new Vector2(float.NaN, float.NaN);
            close_p2 = new Vector2(float.NaN, float.NaN);
            return;
        }
        lines_intersect = true;

        float t2 =
            ((p3.x - p1.x) * dy12 + (p1.y - p3.y) * dx12)
                / -denominator;

        // Find the point of intersection.
        intersection = new Vector2(p1.x + dx12 * t1, p1.y + dy12 * t1);

        // The segments intersect if t1 and t2 are between 0 and 1.
        segments_intersect =
            ((t1 >= 0) && (t1 <= 1) &&
             (t2 >= 0) && (t2 <= 1));

        // Find the closest points on the segments.
        if (t1 < 0)
        {
            t1 = 0;
        }
        else if (t1 > 1)
        {
            t1 = 1;
        }

        if (t2 < 0)
        {
            t2 = 0;
        }
        else if (t2 > 1)
        {
            t2 = 1;
        }

        close_p1 = new Vector2(p1.x + dx12 * t1, p1.y + dy12 * t1);
        close_p2 = new Vector2(p3.x + dx34 * t2, p3.y + dy34 * t2);
    }

    //Equal if colinear
    private bool isLeft(Vector2 a, Vector2 b, Vector2 c)
    {
        return ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) >= 0;
    }
}