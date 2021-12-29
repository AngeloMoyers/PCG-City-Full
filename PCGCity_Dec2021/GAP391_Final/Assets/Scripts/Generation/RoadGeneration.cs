using System.Collections;
using System.Collections.Generic;
using Delaunay;
using Delaunay.Geo;
using UnityEngine;
using UnityEngine.AI;

public class MainRoad
{
    public LineSegment leftSide, rightSide, centerLine;
}
public class RoadGeneration : VoronoiGeneration
{
    [Header("Noise")]
    [SerializeField] GameObject m_noiseMapObject;
    private NoiseMap m_noiseMap;

    [Header("Prefabs")]
    [SerializeField] GameObject m_cityBlockPrefab;
    [SerializeField] GameObject m_mainRoadPrefab;
    [Header("Tuning")]
    [SerializeField] float m_districtShrinkFactor = 1.0f;
    [SerializeField] float m_smallBlockWidth = 3f;
    [SerializeField] float m_smallRoadWidth = 1f;
    [Header("DistrictColors")]
    [SerializeField] Color[] m_fincancialColors;
    [SerializeField] Color[] m_residentialColors;
    [SerializeField] Color[] m_touristColors;
    [SerializeField] Color[] m_shoppingColors;
    [SerializeField] Color[] m_industrialColors;
    [SerializeField] Color[] m_chinaColors;

    [Header("AI")]
    [SerializeField] GameObject m_AiManagerObject;
    private AIManagerScript m_AiManager;

    private bool toggleBlocks = false;
    private bool toggleDistrictGrids = false;

    Dictionary<DistrictType, List<Color>> m_districtColorMap = new Dictionary<DistrictType, List<Color>>();
    private List<GameObject> m_polyCreatorObjs = new List<GameObject>();

    private BuildingPrefabHandler m_prefabHandler;
    public override void Awake()
    {
        m_noiseMap = m_noiseMapObject.GetComponent<NoiseMap>();
        m_noiseMap.GenerateMap();

        m_prefabHandler = GetComponent<BuildingPrefabHandler>();

        m_AiManager = m_AiManagerObject.GetComponent<AIManagerScript>();

        //Assign colors to types, mainly for debug
        m_districtColorMap.Add(DistrictType.kFinancial, new List<Color>());
        m_districtColorMap.Add(DistrictType.kResidential, new List<Color>());
        m_districtColorMap.Add(DistrictType.kTourist, new List<Color>());
        m_districtColorMap.Add(DistrictType.kShopping, new List<Color>());
        m_districtColorMap.Add(DistrictType.kIndustrial, new List<Color>());
        m_districtColorMap.Add(DistrictType.kChina, new List<Color>());

        foreach (Color c in m_fincancialColors)
        {
            m_districtColorMap[DistrictType.kFinancial].Add(c);
        }
        foreach (Color c in m_residentialColors)
        {
            m_districtColorMap[DistrictType.kResidential].Add(c);
        }
        foreach (Color c in m_touristColors)
        {
            m_districtColorMap[DistrictType.kTourist].Add(c);
        }
        foreach (Color c in m_shoppingColors)
        {
            m_districtColorMap[DistrictType.kShopping].Add(c);
        }
        foreach (Color c in m_industrialColors)
        {
            m_districtColorMap[DistrictType.kIndustrial].Add(c);
        }
        foreach (Color c in m_chinaColors)
        {
            m_districtColorMap[DistrictType.kChina].Add(c);
        }

        GenerateVoronoiDiagram();

        m_AiManager.SpawnAgents();
    }

    private void Start()
    {
        //
    }

    public override void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
            toggleBlocks = !toggleBlocks;
    }

    public override void GenerateVoronoiDiagram()
    {
        base.GenerateVoronoiDiagram();

        GenerateDistricts();
    }

    private void GenerateDistricts()
    {
        foreach (Cell cell in m_cells)
        {
            //Setup the cell
            cell.Shrink(m_districtShrinkFactor);
            cell.ReorderCornersCW();
            cell.m_districtType = (DistrictType)Random.Range(0, 6);

            //Setup block generation
            SetCellBlockCorners(cell);

            //Create Actual Block Grids
            float cellWidth = cell.m_districtBoxCorners[1].x - cell.m_districtBoxCorners[0].x;
            float cellHeight = cell.m_districtBoxCorners[3].z - cell.m_districtBoxCorners[0].z;

            int numBlocksHor = (int)(cellWidth / m_smallBlockWidth);
            int numBlocksVer = (int)(cellHeight / m_smallBlockWidth);
            
            //Generate CityBlock Objects
            CreateCityBlocks(numBlocksVer, numBlocksHor, cell);
            
            //Create polygons under districts
            CreatePolygonDrawer(cell);
        }

        SpawnMainRoads();
    }


    public override void DrawGizmos()
    {
        base.DrawGizmos();

        foreach (Cell cell in m_cells)
        {
            if (toggleDistrictGrids)
            {
                Gizmos.color = Color.cyan;

                Gizmos.DrawLine(cell.m_districtBoxCorners[0], cell.m_districtBoxCorners[1]);
                Gizmos.DrawLine(cell.m_districtBoxCorners[0], cell.m_districtBoxCorners[2]);
                Gizmos.DrawLine(cell.m_districtBoxCorners[2], cell.m_districtBoxCorners[3]);
                Gizmos.DrawLine(cell.m_districtBoxCorners[1], cell.m_districtBoxCorners[3]);
            }

            if (toggleBlocks)
            {
                Gizmos.color = Color.blue;
                //draw blocks
                foreach (Line line in cell.m_blockRoads)
                {
                    Vector3 temp1 = new Vector3(line.a.x, 0, line.a.y);
                    Vector3 temp2 = new Vector3(line.b.x, 0, line.b.y);

                    Gizmos.DrawLine(temp1, temp2);
                }
            }


        }
    }

    private void CreatePolygonDrawer(Cell cell)
    {
        //Create polygons under districts
        var poly = new GameObject();
        var beh = poly.AddComponent<DistrictPolygonBehavior>();
        beh.m_owningCell = cell;
        poly.name = "PolyDrawer";
        var polyDraw = poly.AddComponent<PolygonDrawer>();
        polyDraw.Init();
        polyDraw.m_extrudeDistance = 1f;
        polyDraw.isVisible = false;

        m_polyCreatorObjs.Add(poly);

        polyDraw.vertices = new Vector3[cell.m_cornersOrdered.Count];
        //Create Polygons in shape of district for testing
        for (int i = 0; i < cell.m_cornersOrdered.Count; ++i)
        {
            var vert = cell.m_cornersOrdered[i];
            polyDraw.vertices[i] = new Vector3(vert.x, 0, vert.y);
        }
        poly.GetComponent<MeshRenderer>().material.color = m_districtColorMap[cell.m_districtType][Random.Range(0, m_districtColorMap[cell.m_districtType].Count)];
        polyDraw.isActive = true;
        polyDraw.convex = true;
        poly.transform.parent = this.transform;
    }
    private void CreateCityBlocks(int numBlocksVer, int numBlocksHor, Cell cell)
    {
        for (int y = 0; y < numBlocksVer; ++y)
        {
            for (int x = 0; x < numBlocksHor; ++x)
            {
                Vector3[] tempList = new Vector3[4];


                Vector3 bottomLeft = new Vector3((cell.m_districtBoxCorners[0].x + ((x) * (m_smallBlockWidth))), 0, (cell.m_districtBoxCorners[0].z + ((y) * m_smallBlockWidth)));
                tempList[0] = bottomLeft;
                tempList[1] = new Vector3(tempList[0].x + m_smallBlockWidth - m_smallRoadWidth, 0, tempList[0].z);
                tempList[2] = new Vector3(tempList[0].x, 0, tempList[0].z + m_smallBlockWidth - m_smallRoadWidth);
                tempList[3] = new Vector3(tempList[0].x + m_smallBlockWidth - m_smallRoadWidth, 0, tempList[0].z + m_smallBlockWidth - m_smallRoadWidth);

                var obj = Instantiate(m_cityBlockPrefab, this.transform);

                CityBlock temp = obj.GetComponent<CityBlock>(); ;

                temp.m_noiseMap = m_noiseMap;
                temp.m_districtColorMap = m_districtColorMap;
                temp.m_prefabHandler = m_prefabHandler;
                temp.m_aiManager = m_AiManager;
                temp.m_type = cell.m_districtType;
                temp.m_owningCell = cell;


                if (x > 0)
                    bottomLeft.x += m_smallRoadWidth;
                if (y > 0)
                    bottomLeft.z += m_smallRoadWidth;


                if (x > 0)
                {
                    tempList[1].x -= m_smallRoadWidth;
                    tempList[3].x -= m_smallRoadWidth;
                }
                if (y > 0)
                {
                    tempList[2].z -= m_smallRoadWidth;
                    tempList[3].z -= m_smallRoadWidth;
                }

                temp.SetCorners(tempList);
                cell.m_cityBlocks.Add(temp);
                temp.Init();
            }
        }
    }

    private void SetCellBlockCorners(Cell cell)
    {
        //Create box around the cell for block Grid
        Vector2 farthestLeft = new Vector2(0, 0);
        Vector2 farthestRight = new Vector2(0, 0);
        Vector2 farthestUp = new Vector2(0, 0);
        Vector2 farthestDown = new Vector2(0, 0);

        bool firstPass = true;
        foreach (Vector2 corner in cell.m_corners)
        {
            //if first pass, set all to corner
            if (firstPass)
            {
                firstPass = false;
                farthestLeft = farthestRight = farthestUp = farthestDown = corner;
                continue;
            }

            if (corner.x < farthestLeft.x)
            {
                farthestLeft = corner;
            }
            if (corner.x > farthestRight.x)
            {
                farthestRight = corner;
            }
            if (corner.y < farthestDown.y)
            {
                farthestDown = corner;
            }
            if (corner.y > farthestUp.y)
            {
                farthestUp = corner;
            }

        }

        cell.m_districtBoxCorners[0] = new Vector3(farthestLeft.x, 0, farthestDown.y);
        cell.m_districtBoxCorners[1] = new Vector3(farthestLeft.x + (farthestRight.x - farthestLeft.x), 0, farthestDown.y);
        cell.m_districtBoxCorners[2] = new Vector3(farthestLeft.x, 0, farthestDown.y + (farthestUp.y - farthestDown.y));
        cell.m_districtBoxCorners[3] = new Vector3(farthestLeft.x + (farthestRight.x - farthestLeft.x), 0, farthestDown.y + (farthestUp.y - farthestDown.y));
    }

    private void SpawnMainRoads()
    {
        if (m_mainRoadPrefab != null)
        {
            foreach (LineSegment edge in m_edges)
            {
                //Spawn Roads using m_edges

                Vector2 l = (Vector2)edge.p0;
                Vector2 r = (Vector2)edge.p1;
                Vector3 l3 = new Vector3(l.x, 0, l.y);
                Vector3 r3 = new Vector3(r.x, 0, r.y);

                float dist = Vector3.Distance(l3, r3);

                Vector3 midpoint = new Vector3(l.x + (r.x - l.x) / 2, 0, l.y + (r.y - l.y) / 2);

                var obj = Instantiate(m_mainRoadPrefab, this.transform);
                obj.transform.position = midpoint;
                obj.transform.LookAt(r3, Vector3.up);

                obj.transform.localScale = new Vector3(obj.transform.localScale.x, obj.transform.localScale.y, dist);

            }
        }
    }
}
