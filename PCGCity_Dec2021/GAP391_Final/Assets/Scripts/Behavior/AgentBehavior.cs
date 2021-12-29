using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AgentBehavior : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] Transform m_cameraAnchor;
    [SerializeField] NavMeshAgent m_agent;

    [Header("Tuning")]
    [SerializeField] float m_timeToStayHome;
    [SerializeField] float m_timeToWork;

    [Header("Visual")]
    [SerializeField] Color[] m_colors;

    public GameObject m_workBuilding;
    public GameObject m_homeBuilding;

    private bool m_isBeingPiloted = false;
    private bool m_atWork;
    private bool m_atHome;
    private bool m_inTransit;
    private GameObject m_currentTarget;

    private Vector3 m_homeLocation;
    private Vector3 m_workLocation;
    private Vector3 m_targetLocation;

    private MeshRenderer m_renderer;

    private float m_timeAtTarget;

    private void Start()
    {
        m_renderer = GetComponent<MeshRenderer>();
        m_renderer.material.color = m_colors[Random.Range(0, m_colors.Length)];
    }

    void Update()
    {
        if (m_isBeingPiloted)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    m_agent.SetDestination(hit.point);
                }
            }
        }
        else
        {
            RunAI();
        }
    }

    public void Init()
    {
        m_homeLocation = m_homeBuilding.GetComponent<CollectionBase>().m_originalPosition;
        m_workLocation = m_workBuilding.GetComponent<CollectionBase>().m_originalPosition;

        m_atHome = false;
        m_atWork = false;

        if (m_atWork)
        {
            transform.position = m_workLocation;
            m_currentTarget = m_workBuilding;
            m_targetLocation = m_workLocation;
        }
        else
        {
            transform.position = m_homeLocation;
            m_currentTarget = m_homeBuilding;
            m_targetLocation = m_homeLocation;
        }

        m_timeAtTarget = Time.time;

        m_inTransit = true;
        m_agent.SetDestination(m_workLocation);

    }

    private void ResetAI()
    {
        m_currentTarget = (Random.Range(0, 3) < 2) ? m_homeBuilding : m_workBuilding;

        if (m_currentTarget == m_workBuilding)
        {
            m_targetLocation = m_workLocation;
            m_agent.SetDestination(m_targetLocation);
        }
        else
        {
            m_targetLocation = m_homeLocation;
            m_agent.SetDestination(m_targetLocation);
        }

        m_renderer.enabled = true;
        m_atHome = false;
        m_atWork = false;
        m_inTransit = true;
    }

    private void RunAI()
    {
        if (m_atHome)
        {
            if (Time.time - m_timeAtTarget > m_timeToStayHome)
            {
                m_targetLocation = m_workLocation;

                m_atHome = false;
                m_currentTarget = m_workBuilding;
                m_agent.SetDestination(m_targetLocation);
                m_renderer.enabled = true;
                m_inTransit = true;
            }
        }
        else if (m_atWork)
        {
            if (Time.time - m_timeAtTarget > m_timeToWork)
            {
                m_targetLocation = m_homeLocation;

                m_atWork = false;
                m_currentTarget = m_homeBuilding;
                m_agent.SetDestination(m_targetLocation);
                m_renderer.enabled = true;
                m_inTransit = true;
            }
        }

        if (m_inTransit)
        {
            if (Vector3.Distance(transform.position, m_targetLocation) <= 1.1f)
            {
                m_inTransit = false;
                m_renderer.enabled = false;
                
                if (m_currentTarget == m_workBuilding)
                {
                    m_atWork = true;
                    m_timeAtTarget = Time.time;
                }
                else if (m_currentTarget == m_homeBuilding)
                {
                    m_atHome = true;
                    m_timeAtTarget = Time.time;
                }
            }
        }
    }

    public void Pilot()
    {
        m_isBeingPiloted = true;
    }

    public void Release()
    {
        m_isBeingPiloted = false;
        ResetAI();
    }
}
