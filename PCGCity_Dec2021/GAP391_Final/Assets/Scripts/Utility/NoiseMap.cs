using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class NoiseMap : MonoBehaviour
{
    public RawImage debugImage;

    [Header("Dimensions")]
    public int width;
    public int height;
    public float scale;
    public Vector2 offset;

    [Header("Height Map")]
    public float[,] heightMap;


    public bool needsRefresh = false;

    private void Awake()
    {
        GenerateMap();
    }
    private void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        heightMap = NoiseGenerator.Generate(width, height, scale, offset);
    }

}
