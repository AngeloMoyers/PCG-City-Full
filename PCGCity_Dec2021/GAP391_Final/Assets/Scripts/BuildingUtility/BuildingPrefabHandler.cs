using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingPrefabHandler : MonoBehaviour
{
    [Header("Residential")]
    [SerializeField] public GameObject[] m_residentialBase;
    [SerializeField] public GameObject[] m_residentialMid;
    [SerializeField] public GameObject[] m_residentialRoof;

    [Header("Financial")]
    [SerializeField] public GameObject[] m_financialBase;
    [SerializeField] public GameObject[] m_financialMid;
    [SerializeField] public GameObject[] m_financialRoof;

    [Header("China")]
    [SerializeField] public GameObject[] m_chinaBase;
    [SerializeField] public GameObject[] m_chinaMid;
    [SerializeField] public GameObject[] m_chinaRoof;

    [Header("General")]
    [SerializeField] public GameObject[] m_generalBase;
    [SerializeField] public GameObject[] m_generalMid;
    [SerializeField] public GameObject[] m_generalRoof;

}
