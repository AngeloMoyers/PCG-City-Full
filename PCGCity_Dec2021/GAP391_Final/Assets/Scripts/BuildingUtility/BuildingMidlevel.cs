using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingMidlevel : MonoBehaviour
{
    [SerializeField] Transform[] m_windowSpawnTransforms;

    [SerializeField] public Transform m_nextLevelTransform;

    public BuildingBase m_buildingBase;

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
