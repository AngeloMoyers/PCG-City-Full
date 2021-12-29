using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingBase : MonoBehaviour
{
    [SerializeField] Transform m_doorSpawnTransform;
    [SerializeField] Transform[] m_windowSpawnTransforms;

    [SerializeField] public Transform m_nextLevelTransform;

    public Dictionary<Color, List<GameObject>> m_objectsByColorLists = new Dictionary<Color, List<GameObject>>();

    public void SpawnDoor(GameObject doorPrefab)
    {
        if (m_doorSpawnTransform == null || doorPrefab == null)
            return;

        var obj = Instantiate(doorPrefab, m_doorSpawnTransform.position, m_doorSpawnTransform.rotation, m_doorSpawnTransform);
        obj.transform.parent = null;

        AddObjectToDictionary(obj);
        GetChildRecursive(obj);
    }

    public void SpawnWindows(GameObject windowPrefab)
    {
        if (windowPrefab == null)
            return;

        for (int i = 0; i < m_windowSpawnTransforms.Length; ++i)
        {
            var obj = Instantiate(windowPrefab, m_windowSpawnTransforms[i].position, m_windowSpawnTransforms[i].rotation, m_windowSpawnTransforms[i]);
            obj.transform.parent = null;

            AddObjectToDictionary(obj);
            GetChildRecursive(obj);
        }
    }

    public void GetChildRecursive(GameObject obj)
    {
        if (obj == null)
            return;

        foreach (Transform child in obj.transform)
        {
            child.parent = null;

            var mR = child.GetComponent<MeshRenderer>();
            if (mR != null)
            {
                if (m_objectsByColorLists.ContainsKey(mR.material.color))
                {
                    m_objectsByColorLists[mR.material.color].Add(child.gameObject);
                }
                else
                {
                    m_objectsByColorLists.Add(mR.material.color, new List<GameObject>());
                    m_objectsByColorLists[mR.material.color].Add(child.gameObject);
                }
            }

            GetChildRecursive(child.gameObject);
        }
    }

    public void AddObjectToDictionary(GameObject obj)
    {
        obj.transform.parent = null;

        var mR = obj.GetComponent<MeshRenderer>();
        if (mR != null)
        {
            if (m_objectsByColorLists.ContainsKey(mR.material.color))
            {
                m_objectsByColorLists[mR.material.color].Add(obj.gameObject);
            }
            else
            {
                m_objectsByColorLists.Add(mR.material.color, new List<GameObject>());
                m_objectsByColorLists[mR.material.color].Add(obj.gameObject);
            }
        }
    }
}
