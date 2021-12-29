using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadBehavior : MonoBehaviour
{
    [SerializeField] Transform m_spawnpoint;
    [SerializeField] GameObject m_objectPrefab;
    private void Start()
    {
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag == "Building")
        {
            var cityBlock = other.transform.parent.transform.parent.gameObject.GetComponent<CityBlock>();
            if (cityBlock != null)

                cityBlock.Shrink();
        }
    }

    private void SpawnObj()
    {
        var obj = Instantiate(m_objectPrefab, m_spawnpoint.position, m_spawnpoint.rotation, null);
        obj.layer = 31;
    }
}
