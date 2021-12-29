using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingRoof : MonoBehaviour
{
    [SerializeField] Transform m_doorSpawnTransform;
    [SerializeField] Transform[] m_windowSpawnTransforms;

    public BuildingBase m_buildingBase;

    public void SpawnDoor(GameObject doorPrefab)
    {
        if (m_doorSpawnTransform == null || doorPrefab == null)
            return;

        var obj = Instantiate(doorPrefab, m_doorSpawnTransform.position, m_doorSpawnTransform.rotation, m_doorSpawnTransform);
        obj.transform.parent = null;
        m_buildingBase.AddObjectToDictionary(obj);
        m_buildingBase.GetChildRecursive(obj);
    }

    public void SpawnWindows(GameObject windowPrefab)
    {
        if (windowPrefab == null)
            return;

        for (int i = 0; i < m_windowSpawnTransforms.Length; ++i)
        {
            var obj = Instantiate(windowPrefab, m_windowSpawnTransforms[i].position, m_windowSpawnTransforms[i].rotation, m_windowSpawnTransforms[i]);
            obj.transform.parent = null;

            m_buildingBase.AddObjectToDictionary(obj);
            m_buildingBase.GetChildRecursive(obj);
        }
    }
}
