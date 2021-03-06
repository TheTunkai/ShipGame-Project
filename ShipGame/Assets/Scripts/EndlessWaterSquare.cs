using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class EndlessWaterSquare : MonoBehaviour
{
    public GameObject boatObj;

    public GameObject waterSquareObj;

    private float squareWidth = 50f;
    private float innerSquareRes = 2f;
    
    private List<WaterSquare> waterSquares = new List<WaterSquare>();

    private float secondsSinceStart;
    private Vector3 boatPos;
    private Vector3 oceanPos;

    private bool hasThreadUpdatedWater;


    // Start is called before the first frame update
    void Start()
    {
        CreateSea();

        secondsSinceStart = Time.time;

        ThreadPool.QueueUserWorkItem(new WaitCallback(UpdateWaterWithThreadPooling));

        StartCoroutine(UpdateWater());
    }

    // Update is called once per frame
    void Update()
    {
        secondsSinceStart = Time.time;

        boatPos = boatObj.transform.position;
    }

    void UpdateWaterWithThreadPooling(object state)
    {
        MoveWaterToBoat();

        for (int j = 0; j < waterSquares.Count; j++)
        {
            Vector3 centerPos = waterSquares[j].centerPos;
            Vector3[] vertices = waterSquares[j].vertices;


            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertexPos = vertices[i];

                // can't use transform point in thread
                Vector3 vertexGlobal = vertexPos + centerPos + oceanPos;
                vertexPos.y = WaterController.instance.GetWaveYPos(vertexGlobal, secondsSinceStart);
                vertices[i] = vertexPos;
            }
        }

        hasThreadUpdatedWater = true;

        Debug.Log("Thread updated");
    }

    private void MoveWaterToBoat()
    {
        float x = innerSquareRes * (int) Mathf.Round(boatPos.x / innerSquareRes);
        float z = innerSquareRes * (int) Mathf.Round(boatPos.z / innerSquareRes);

        if (oceanPos.x != x || oceanPos.z != z)
        {
            Debug.Log("Moved Sea");
            oceanPos = new Vector3(x, oceanPos.y, z);
        }
    }

    IEnumerator UpdateWater()
    {
        Debug.Log("UpdateWater called");
        while (true)
        {
            if (hasThreadUpdatedWater)
            {
                // move water to boat position
                transform.position = oceanPos;

                for (int i = 0; i < waterSquares.Count; i++)
                {
                    waterSquares[i].terrainMeshFilter.mesh.vertices = waterSquares[i].vertices;
                    waterSquares[i].terrainMeshFilter.mesh.RecalculateNormals();
                }

                hasThreadUpdatedWater = false;

                ThreadPool.QueueUserWorkItem(new WaitCallback(UpdateWaterWithThreadPooling));
            }
            
            yield return new WaitForSeconds(Time.deltaTime * 3f);
        }
    }

    private void CreateSea()
    {
        AddWaterPlane(0f, 0f, 0f, squareWidth, innerSquareRes);
    }

    private void AddWaterPlane(float xCoordinate, float zCoordinate, float yPosition, float squareWidth, float spacing)
    {
        GameObject waterPlane = Instantiate(waterSquareObj, transform.position, transform.rotation);

        waterPlane.SetActive(true);

        // change water plane position
        Vector3 centerPos = transform.position;

        centerPos.x += xCoordinate;
        centerPos.z += zCoordinate;
        centerPos.y = yPosition;

        waterPlane.transform.position = centerPos;

        // parent water plane to sea
        waterPlane.transform.parent = transform;

        WaterSquare newWaterSquare = new WaterSquare(waterPlane, squareWidth, spacing);

        waterSquares.Add(newWaterSquare);
    }
}