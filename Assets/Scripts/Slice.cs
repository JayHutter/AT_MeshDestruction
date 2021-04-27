using System.Collections.Generic;
using UnityEngine;

public class Slice : MonoBehaviour
{
    Vector3[] vertsOrig;
    Vector3[] normalsOrig;
    int[] trisOrig;
    Vector2[] uvsOrig;
    Plane intersectionPlane;

    List<Vector3> newVerts;

    public bool fillHole = true;
    public float minSize = 20.0f;
    public bool cullSmallSizes = true;

    private float deleteTimer = 0;
    public float deletionDelay = 2.0f;
    private bool deleting = false;

    public Material destroyParticle;

    private void Start()
    {
       
    }

    private void Update()
    {
        CheckTooSmall();
    }

    public void SliceObject(Plane intersection)
    {
        intersectionPlane = intersection;
        Mesh ogMesh = gameObject.GetComponent<MeshFilter>().mesh;
        vertsOrig = ogMesh.vertices;
        normalsOrig = ogMesh.normals;
        trisOrig = ogMesh.triangles;
        uvsOrig = ogMesh.uv;
        newVerts = new List<Vector3>();


        List<int> trisPos = new List<int>();
        List<int> trisNeg = new List<int>();
        List<Vector3> vertsPos = new List<Vector3>();
        List<Vector3> vertsNeg = new List<Vector3>();
        List<Vector3> normalsPos = new List<Vector3>();
        List<Vector3> normalsNeg = new List<Vector3>();
        List<Vector2> uvsPos = new List<Vector2>();
        List<Vector2> uvsNeg = new List<Vector2>();

        for (int i = 0; i < trisOrig.Length; i += 3)
        {
            int tri0 = trisOrig[i];
            int tri1 = trisOrig[i + 1];
            int tri2 = trisOrig[i + 2];

            Vector3 vert0 = vertsOrig[tri0];
            Vector3 vert1 = vertsOrig[tri1];
            Vector3 vert2 = vertsOrig[tri2];

            bool side0 = intersectionPlane.GetSide(vert0);
            bool side1 = intersectionPlane.GetSide(vert1);
            bool side2 = intersectionPlane.GetSide(vert2);

            if (side0 && side1 && side2)
            {
                CopyTri(i, trisPos, vertsPos, normalsPos, uvsPos);
            }

            else if (!side0 && !side1 && !side2)
            {
                CopyTri(i, trisNeg, vertsNeg, normalsNeg, uvsNeg);
            }

            else
            {
                SliceTri(i, side0, side1, side2, 
                    trisPos, vertsPos, normalsPos, uvsPos,
                    trisNeg, vertsNeg, normalsNeg, uvsNeg);
                //CopyValues(i, trisPos, vertsPos, normalsPos, uvsPos);
            }
        }

        if (fillHole)
        {
            FillGap(trisPos, vertsPos, normalsPos, uvsPos);
            FillGap(trisNeg, vertsNeg, normalsNeg, uvsNeg);
        }

        CreateSliceObject(trisPos, vertsPos, normalsPos, uvsPos);
        CreateSliceObject(trisNeg, vertsNeg, normalsNeg, uvsNeg);
    }

    private void CopyTri(int i, List<int> tris, List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs)
    {
        for (int j=0; j<3; j++)
        {
            verts.Add(vertsOrig[trisOrig[i + j]]);
            tris.Add(verts.Count - 1);

            normals.Add(normalsOrig[trisOrig[i + j]]);
            uvs.Add(uvsOrig[trisOrig[i + j]]);
        }
    }

    private void SliceTri(int i, bool side0, bool side1, bool side2,
        List<int> trisPos, List<Vector3> vertsPos, List<Vector3> normalsPos, List<Vector2> uvsPos,
        List<int> trisNeg, List<Vector3> vertsNeg, List<Vector3> normalsNeg, List<Vector2> uvsNeg)
    {
        int aloneIndex = side1 == side2 ? 0 : side0 == side2 ? 1 : 2;
        int index1 = i + ((aloneIndex + 1) % 3);
        int index2 = i + ((aloneIndex + 2) % 3);
        aloneIndex += i;

        //Get original vertices, normals and uvs
        Vector3 aloneVert = vertsOrig[trisOrig[aloneIndex]];
        Vector3 vert1 = vertsOrig[trisOrig[index1]];
        Vector3 vert2 = vertsOrig[trisOrig[index2]];

        Vector3 aloneNormal = normalsOrig[trisOrig[aloneIndex]];
        Vector3 normal1 = normalsOrig[trisOrig[index1]];
        Vector3 normal2 = normalsOrig[trisOrig[index2]];

        Vector2 aloneUv = uvsOrig[trisOrig[aloneIndex]];
        Vector2 uv1 = uvsOrig[trisOrig[index1]];
        Vector2 uv2 = uvsOrig[trisOrig[index2]];

        //Calculate the new vertices, normals and uvs at the point of intersection
        float Lerp1;
        float Lerp2;
        Vector3 cutVert1 = PointOnPlane(aloneVert, vert1, out Lerp1);
        Vector3 cutVert2 = PointOnPlane(aloneVert, vert2, out Lerp2);

        newVerts.Add(cutVert1);
        newVerts.Add(cutVert2);

        Vector3 cutNormal1 = Vector3.Lerp(aloneNormal, normal1, Lerp1);
        Vector3 cutNormal2 = Vector3.Lerp(aloneNormal, normal2, Lerp2);

        Vector2 cutUv1 = Vector2.Lerp(aloneUv, uv1, Lerp1);
        Vector2 cutUv2 = Vector2.Lerp(aloneUv, uv2, Lerp2);


        if (intersectionPlane.GetSide(aloneVert))
        {
            AddTri(trisPos, vertsPos, normalsPos, uvsPos, aloneVert, aloneNormal, aloneUv);
            AddTri(trisPos, vertsPos, normalsPos, uvsPos, cutVert1, cutNormal1, cutUv1);
            AddTri(trisPos, vertsPos, normalsPos, uvsPos, cutVert2, cutNormal2, cutUv2);

            AddTri(trisNeg, vertsNeg, normalsNeg, uvsNeg, cutVert1, cutNormal1, cutUv1);
            AddTri(trisNeg, vertsNeg, normalsNeg, uvsNeg, vert1, normal1, uv1);
            AddTri(trisNeg, vertsNeg, normalsNeg, uvsNeg, vert2, normal2, uv2);
   
            AddTri(trisNeg, vertsNeg, normalsNeg, uvsNeg, cutVert1, cutNormal1, cutUv1);
            AddTri(trisNeg, vertsNeg, normalsNeg, uvsNeg, vert2, normal2, uv2);
            AddTri(trisNeg, vertsNeg, normalsNeg, uvsNeg, cutVert2, cutNormal2, cutUv2);
        }
        else
        {
            AddTri(trisNeg, vertsNeg, normalsNeg, uvsNeg, aloneVert, aloneNormal, aloneUv);
            AddTri(trisNeg, vertsNeg, normalsNeg, uvsNeg, cutVert1, cutNormal1, cutUv1);
            AddTri(trisNeg, vertsNeg, normalsNeg, uvsNeg, cutVert2, cutNormal2, cutUv2);

            AddTri(trisPos, vertsPos, normalsPos, uvsPos, cutVert1, cutNormal1, cutUv1);
            AddTri(trisPos, vertsPos, normalsPos, uvsPos, vert1, normal1, uv1);
            AddTri(trisPos, vertsPos, normalsPos, uvsPos, vert2, normal2, uv2);
                                                     
            AddTri(trisPos, vertsPos, normalsPos, uvsPos, cutVert1, cutNormal1, cutUv1);
            AddTri(trisPos, vertsPos, normalsPos, uvsPos, vert2, normal2, uv2);
            AddTri(trisPos, vertsPos, normalsPos, uvsPos, cutVert2, cutNormal2, cutUv2);
        }
    }

    private void AddTri(List<int> tris, List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs,
        Vector3 vert, Vector3 normal, Vector2 uv)
    {
        verts.Add(vert);
        normals.Add(normal);
        uvs.Add(uv);
        tris.Add(verts.Count - 1);
    }

    private Vector3 PointOnPlane(Vector3 vert0, Vector3 vert1, out float lerp)
    {
        Vector3 dir = vert1 - vert0;
        Ray ray = new Ray(vert0, dir.normalized);

        float dist;
        intersectionPlane.Raycast(ray, out dist);
        Vector3 vert2 = vert0 + (dir.normalized * dist);
        lerp = dist / dir.magnitude;
        return vert2;
    }

    private void FillGap(List<int> tris, List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs)
    {
        Vector3 origin = newVerts[0];
        for (int i=1; i < newVerts.Count-1; i++)
        {
            AddTri(tris, verts, normals, uvs, origin, Vector3.zero, Vector2.zero);
            AddTri(tris, verts, normals, uvs, newVerts[i], Vector3.zero, Vector2.zero);
            AddTri(tris, verts, normals, uvs, newVerts[i+1], Vector3.zero, Vector2.zero);
        }
    }

    private void CreateSliceObject(List<int> tris, List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs)
    {
        GameObject sliceObj = new GameObject("Slice");
        Mesh mesh = new Mesh();
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.normals = normals.ToArray();
        mesh.RecalculateTangents();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var filter = sliceObj.AddComponent<MeshFilter>();
        filter.mesh = mesh;
        var renderer = sliceObj.AddComponent<MeshRenderer>();
        renderer.material = gameObject.GetComponent<MeshRenderer>().material;

        sliceObj.transform.position = gameObject.transform.position;
        sliceObj.transform.rotation = gameObject.transform.rotation;
        sliceObj.transform.localScale = gameObject.transform.localScale;

        var rb = sliceObj.AddComponent<Rigidbody>();

        var ogRb = gameObject.GetComponent<Rigidbody>();
        if (ogRb)
        {
            rb.mass = ogRb.mass;
            rb.angularDrag = ogRb.angularDrag;
            rb.drag = ogRb.drag;
        }
        else
        {
            rb.mass = 100;
        }

        var collider = sliceObj.AddComponent<MeshCollider>();
        collider.convex = true;
        rb.AddExplosionForce(1000, sliceObj.transform.position, 10);

        var slice = sliceObj.AddComponent<Slice>();
        slice.deletionDelay = deletionDelay;
        slice.destroyParticle = destroyParticle;
        slice.fillHole = fillHole;
        slice.minSize = minSize;

        sliceObj.layer = LayerMask.NameToLayer("Destruction");
        Destroy(gameObject);
    }

    //Mange small objects

    private void CheckTooSmall()
    {
        if (cullSmallSizes)
        {
            cullSmallSizes = false;

            float vol = CalculateVolume() * 100;

            Debug.Log(vol);

            if (vol < minSize)
            {
                deleting = true;
                deleteTimer = deletionDelay;
            }
        }

        if (deleting)
        {
            deleteTimer -= Time.deltaTime;
            if (deleteTimer <= 0)
            {
                DestroySlice();
            }
        }
    }

    private float VolumeOfTri(Vector3 pA, Vector3 pB, Vector3 pC)
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

    private float CalculateVolume()
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

    private void DestroySlice()
    {
        GameObject particle = (GameObject)Instantiate(Resources.Load("Particles/DestroyParticle"));
        var emitter = particle.GetComponent<ParticleSystem>();
        var shape = emitter.shape;
        shape.mesh = GetComponent<MeshFilter>().mesh;

        //Scale the mesh emittor to the object size
        Vector3[] verts;
        verts = shape.mesh.vertices;

        Vector3[] scaledVerts = new Vector3[verts.Length];

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 vertex = verts[i];
            vertex.x = vertex.x * transform.localScale.x;
            vertex.y = vertex.y * transform.localScale.y;
            vertex.z = vertex.z * transform.localScale.z;
            scaledVerts[i] = vertex;
        }

        shape.mesh.vertices = scaledVerts;

        if (destroyParticle == null)
        {
            particle.GetComponent<ParticleSystemRenderer>().material = gameObject.GetComponent<Renderer>().material;
        }
        else
        {
            particle.GetComponent<ParticleSystemRenderer>().material = destroyParticle;
        }


        particle.transform.position = transform.position;
        particle.transform.rotation = transform.rotation;
        //particle.transform.localScale = transform.localScale;

        Debug.Log("BOOM");

        Destroy(gameObject);
    }
}
