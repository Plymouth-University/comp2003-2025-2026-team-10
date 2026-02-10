using System.Collections.Generic;
using Unity.Properties;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class PlaneGenerator : MonoBehaviour
{
    //references
    Mesh myMesh;
    MeshFilter meshFilter;

    //plane settings
    [SerializeField] Vector2 planeSize = new Vector2(1,1);
    [SerializeField] int planeResolution = 1;

    //mesh values
    List<Vector3> vertices;
    List<int> triangles;

    //sdafag
    void Awake()
    {
        myMesh = new Mesh();
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = myMesh;
    }

    void Update()
    {
        //aggshd
        planeResolution = Mathf.Clamp(planeResolution,1,50);

        GeneratePlane(planeSize,planeResolution);
        AssignMesh();
    }

    void GeneratePlane (Vector2 size, int resolution)
    {
        //Create vertices
        vertices = new List<Vector3>();
        float xPerStep = size.x/resolution;
        float yPerStep = size.y/resolution;
        for(int y = 0; y<resolution+1; y++)
        {
            for(int x = 0; x<resolution+1; x++)
            {
                vertices.Add(new Vector3(x*xPerStep,0,y*yPerStep));
            }
        }

        //Create triangles
        triangles = new List<int>();
        for(int row = 0; row<resolution; row++)
        {
            for(int column = 0; column<resolution; column++)
            {
                int i = (row*resolution) + row + column;
                //first triangle 
                triangles.Add(i);
                triangles.Add(i+(resolution)+1); 
                triangles.Add(i+(resolution)+2);

                //second triangle 
                triangles.Add(i); 
                triangles.Add(i+resolution+2); 
                triangles.Add(i+1);
            }
        }
    }

    void AssignMesh()
    {
        myMesh.Clear();
        myMesh.vertices = vertices.ToArray();
        myMesh.triangles = triangles.ToArray();
    }
}
