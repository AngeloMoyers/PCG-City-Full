using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIManagerScript : MonoBehaviour
{
    [SerializeField] GameObject m_agentPrefab;
    [SerializeField] int m_agentCount = 100;

    public List<AgentBehavior> m_agents = new List<AgentBehavior>();
    public List<GameObject> m_workBuildings = new List<GameObject>();
    public List<GameObject> m_homeBuildings = new List<GameObject>();

    public void SpawnAgents()
    {
        for (int i = 0; i < m_agentCount; ++i)
        {
            var home = m_homeBuildings[Random.Range(0, m_homeBuildings.Count)];
            var work = m_workBuildings[Random.Range(0, m_workBuildings.Count)];

            var obj = Instantiate(m_agentPrefab, home.GetComponent<CollectionBase>().m_originalPosition, Quaternion.identity, null);
            obj.transform.parent = this.transform;
            var behavior = obj.GetComponent<AgentBehavior>();
            behavior.m_homeBuilding = home;
            behavior.m_workBuilding = work;

            behavior.Init();

            m_agents.Add(behavior);
        }
    }
}
