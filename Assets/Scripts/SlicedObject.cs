using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlicedObject : MonoBehaviour
{
    bool checkSize = true;

    private void Update()
    {
        if (checkSize)
        {
            checkSize = false;
            float volume = Volume() * 100;
        }
    }


    float VolumeOfTri(Vector3 pA, Vector3 pB, Vector3 pC)
    {
        float scale = transform.localScale.x * transform.localScale.y * transform.localScale.z;

        float vCBA = pC.x * pB.y * pA.z;
        float vBCA = pB.x * pC.y * pA.z;
        float vCAB = pC.x * pA.y * pB.z;
        float vACB = pA.x * pC.y * pB.z;
        float vBAC = pB.x * pA.y * pC.z;
        float vABC = pA.x * pB.y * pC.z;

        return (1.0f / 6.0f) * (-vCBA + vBCA + vCAB - vACB - vBAC + vABC) * scale;
    }

    float Volume()
    {
        float vol = 0.0f;
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 pointA = verts[tris[i]];
            Vector3 pointB = verts[tris[i + 1]];
            Vector3 pointC = verts[tris[i + 2]];

            vol += VolumeOfTri(pointA, pointB, pointC);
        }

        return Mathf.Abs(vol);
    }
}
