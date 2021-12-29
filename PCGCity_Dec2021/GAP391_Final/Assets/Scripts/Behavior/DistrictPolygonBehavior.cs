using UnityEngine;

public class DistrictPolygonBehavior : MonoBehaviour
{
    public Cell m_owningCell;

    private bool isActive = true;
    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        if (other.transform.tag == "Building")
        {
            var cityBlock = other.transform.parent.transform.parent.gameObject.GetComponent<CityBlock>();

            if (cityBlock != null && cityBlock.m_owningCell != m_owningCell)
            {
                cityBlock.Kill();
            }
        }
    }

    public void SetActive(bool newActive) { isActive = newActive; }
}
