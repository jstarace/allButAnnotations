using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Highlighter : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("We run");

        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[4];
        Vector2[] uv = new Vector2[4];
        int[] triangles = new int[6];

        vertices[0] = new Vector3(-100.5f, 48.5f, 0.0f);
        vertices[1] = new Vector3(-100.5f, 51.5f, 0.0f);
        vertices[2] = new Vector3(-97.5f, 51.5f, 0.0f);
        vertices[3] = new Vector3(-97.5f, 48.5f, 0.0f);

        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(0, 1);
        uv[2] = new Vector2(1, 1);
        uv[3] = new Vector2(1, 0);

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        GetComponent<MeshFilter>().mesh = mesh;


    }
}
