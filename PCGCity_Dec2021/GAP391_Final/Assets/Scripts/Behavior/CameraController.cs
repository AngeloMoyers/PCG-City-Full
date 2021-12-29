using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement Tuning")]
    [SerializeField] float speed;
    [SerializeField] float speedH;
    [SerializeField] float speedV;

    [SerializeField] float cullDistance = 100;

    [SerializeField] AIManagerScript m_aiManager;
    private Transform m_followTransform;
    private int m_currentAgentIndex = 0;

    float yaw = 0.0f;
    float pitch = 0.0f;

    bool freeCam = true;

    private void Start()
    {
        var cam = GetComponent<Camera>();
        float[] distances = new float[32];
        distances[31] = cullDistance;
        cam.layerCullDistances = distances;
    }

    void Update()
    {
        yaw += speedH * Input.GetAxis("Mouse X");
        pitch -= speedV * Input.GetAxis("Mouse Y");

        if (freeCam)
        {
            transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);

            if (Input.GetKey(KeyCode.D))
            {
                transform.Translate(new Vector3(speed * Time.deltaTime, 0, 0));
            }
            if (Input.GetKey(KeyCode.A))
            {
                transform.Translate(new Vector3(-speed * Time.deltaTime, 0, 0));
            }
            if (Input.GetKey(KeyCode.S))
            {
                transform.Translate(new Vector3(0, 0, -speed * Time.deltaTime));
            }
            if (Input.GetKey(KeyCode.W))
            {
                transform.Translate(new Vector3(0, 0, speed * Time.deltaTime));
            }
            if (Input.GetKey(KeyCode.Space))
            {
                transform.Translate(new Vector3(0, speed * Time.deltaTime, 0));
            }
            if (Input.GetKey(KeyCode.Q))
            {
                freeCam = false;
            }
        }
        else
        {
            transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);

            if (Input.GetKey(KeyCode.D))
            {
                GetNextTarget();
            }
            if (Input.GetKey(KeyCode.A))
            {
                GetPreviousTarget();
            }
            if (Input.GetKey(KeyCode.Q))
            {
                freeCam = true;
            }

            FollowTarget();
        }

    }

    private void FollowTarget()
    {
        if (m_followTransform == null)
            m_followTransform = m_aiManager.m_agents[0].transform;

        transform.position = m_followTransform.position;
    }

    private void GetNextTarget()
    {
        m_aiManager.m_agents[m_currentAgentIndex].Release();

        m_currentAgentIndex++;
        if (m_currentAgentIndex >= m_aiManager.m_agents.Count)
            m_currentAgentIndex = 0;
        
        m_followTransform = m_aiManager.m_agents[m_currentAgentIndex].transform;
        m_aiManager.m_agents[m_currentAgentIndex].Pilot();
    }

    private void GetPreviousTarget()
    {
        m_aiManager.m_agents[m_currentAgentIndex].Release();

        m_currentAgentIndex--;
        if (m_currentAgentIndex < 0)
            m_currentAgentIndex = m_aiManager.m_agents.Count - 1;

        m_followTransform = m_aiManager.m_agents[m_currentAgentIndex].transform;
        m_aiManager.m_agents[m_currentAgentIndex].Pilot();
    }
}
