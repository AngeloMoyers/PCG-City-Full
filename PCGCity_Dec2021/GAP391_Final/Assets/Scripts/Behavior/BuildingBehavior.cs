using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingBehavior : MonoBehaviour
{
    [SerializeField] Transform[] m_spawnpoints;
    [SerializeField] GameObject[] m_objects;

    private void Start()
    {
    }

    private void Awake()
    {
        SpawnObjects();
    }

    private void SpawnObjects()
    {
        if (m_spawnpoints.Length <= 0 || m_objects.Length <= 0)
            return;

        foreach (Transform t in m_spawnpoints)
        {
            var obj = Instantiate(m_objects[Random.Range(0, m_objects.Length)], t.position, t.rotation, null);
            obj.layer = 31;
        }
    }
}
