using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slicer : MonoBehaviour
{
    private bool edgeSet = false;
    private Vector3 edgeVertex = Vector3.zero;
    private Vector2 edgeUV = Vector2.zero;
    private Plane edgePlane = new Plane();

    public int CutCascades = 1;
    public float ExplodeForce = 10000;
    public float minSize = 20.0f;

    public bool checkSize = false;
    private bool destroy = false;
    private float deleteTimer = 0;

    private float volume = 0;

    public bool fillHole = true;
    public bool checkOverlap = true;

    public Material destroyParticleMaterial;

    public float mass = 10;
    public bool carryTag = true;

    private void Start()
    {
        gameObject.layer = 8;
        checkSize = true;
    }

    private void Update()
    {
        if (checkSize)
        {
            checkSize = false;
            volume = CalculateVolume() * 100;

            if (volume < minSize)
            {
                //GetComponent<MeshRenderer>().material = (Material)Resources.Load("Materials/Hit", typeof(Material));
                destroy = true;
                deleteTimer = 2.0f;
            }

            //Debug.Log(volume);
        }

        if (destroy)
        {
            deleteTimer -= Time.deltaTime;
            if (deleteTimer <= 0)
            {
                DeleteSlicedObject();
            }
        } 
    }

    public bool BeingDestroyed()
    {
        return destroy;
    }

    private void DeleteSlicedObject()
    {
        GameObject particle = (GameObject)Instantiate(Resources.Load("Particles/DestroyParticle"));
        var emitter = particle.GetComponent<ParticleSystem>();
        var shape = emitter.shape;
        shape.mesh = GetComponent<MeshFilter>().mesh;

        //Scale the mesh emittor to the object size
        Vector3[] verts;
        verts = shape.mesh.vertices;

        Vector3[] scaledVerts = new Vector3[verts.Length];
        
        for(int i=0; i< verts.Length; i++)
        {
            Vector3 vertex = verts[i];
            vertex.x = vertex.x * transform.localScale.x;
            vertex.y = vertex.y * transform.localScale.y;
            vertex.z = vertex.z * transform.localScale.z;
            scaledVerts[i] = vertex;
        }

        shape.mesh.vertices = scaledVerts;

        if (destroyParticleMaterial == null)
        {
            particle.GetComponent<ParticleSystemRenderer>().material = gameObject.GetComponent<Renderer>().material;
        }
        else
        {
            particle.GetComponent<ParticleSystemRenderer>().material = destroyParticleMaterial;
        }
        

        particle.transform.position = transform.position;
        particle.transform.rotation = transform.rotation;
        //particle.transform.localScale = transform.localScale;

        Destroy(gameObject);
    }

    public void DestroyMesh(Plane cutPlane)
    {
        Transform[] children = GetComponentsInChildren<Transform>();

        foreach (Transform child in children)
        {
            if (child.parent == transform)
            {
                child.transform.parent = null;

                if (!child.gameObject.GetComponent<Rigidbody>())
                {
                    Debug.Log("REMOVED + ADDED RB");
                    child.gameObject.AddComponent<Rigidbody>();
                }
            } 
        }

        transform.parent = null;

        checkSize = false;

        var originalMesh = GetComponent<MeshFilter>().mesh;
        originalMesh.RecalculateBounds();
        var parts = new List<PartMesh>();
        var subParts = new List<PartMesh>();

        var mainPart = new PartMesh()
        {
            UV = originalMesh.uv,
            Vertices = originalMesh.vertices,
            Normals = originalMesh.normals,
            Triangles = new int[originalMesh.subMeshCount][],
            Bounds = originalMesh.bounds
        };
        for (int i = 0; i < originalMesh.subMeshCount; i++)
            mainPart.Triangles[i] = originalMesh.GetTriangles(i);

        parts.Add(mainPart);

        for (var c = 0; c < CutCascades; c++)
        {
            for (var i = 0; i < parts.Count; i++)
            {
                subParts.Add(GenerateMesh(parts[i], cutPlane, true));
                subParts.Add(GenerateMesh(parts[i], cutPlane, false));
            }
            parts = new List<PartMesh>(subParts);
            subParts.Clear();
        }

        for (var i = 0; i < parts.Count; i++)
        {
            parts[i].MakeGameobject(this);
            //parts[i].GameObject.GetComponent<Rigidbody>().AddForceAtPosition(parts[i].Bounds.center * ExplodeForce, transform.position);
            Vector3 force = cutPlane.normal * ExplodeForce * (i - 1);
            parts[i].GameObject.GetComponent<Rigidbody>().AddForce(force);
        }

        Destroy(gameObject);
    }

    private PartMesh GenerateMesh(PartMesh original, Plane plane, bool left)
    {
        var partMesh = new PartMesh() { };
        var ray1 = new Ray();
        var ray2 = new Ray();


        List<Vector3> vertices = new List<Vector3>();

        for (var i = 0; i < original.Triangles.Length; i++)
        {
            var triangles = original.Triangles[i];
            edgeSet = false;

            for (var j = 0; j < triangles.Length; j = j + 3)
            {
                var sideA = plane.GetSide(original.Vertices[triangles[j]]) == left;
                var sideB = plane.GetSide(original.Vertices[triangles[j + 1]]) == left;
                var sideC = plane.GetSide(original.Vertices[triangles[j + 2]]) == left;

                var sideCount = (sideA ? 1 : 0) +
                                (sideB ? 1 : 0) +
                                (sideC ? 1 : 0);
                //If none on side skip
                if (sideCount == 0)
                {
                    continue;
                }
                //If all then add a triangle
                if (sideCount == 3)
                {
                    partMesh.AddTriangle(i,
                                         original.Vertices[triangles[j]], original.Vertices[triangles[j + 1]], original.Vertices[triangles[j + 2]],
                                         original.Normals[triangles[j]], original.Normals[triangles[j + 1]], original.Normals[triangles[j + 2]],
                                         original.UV[triangles[j]], original.UV[triangles[j + 1]], original.UV[triangles[j + 2]]);
                    continue;
                }
                //else calculate a new triangle using the intersection point

                if (sideA)
                {
                    vertices.Add(original.Vertices[triangles[j]]);
                }
                if (sideB)
                {
                    vertices.Add(original.Vertices[triangles[j + 1]]);
                }
                if (sideC)
                {
                    vertices.Add(original.Vertices[triangles[j + 2]]);
                }

                //cut points
                var singleIndex = sideB == sideC ? 0 : sideA == sideC ? 1 : 2;

                ray1.origin = original.Vertices[triangles[j + singleIndex]];
                var dir1 = original.Vertices[triangles[j + ((singleIndex + 1) % 3)]] - original.Vertices[triangles[j + singleIndex]];
                ray1.direction = dir1;
                plane.Raycast(ray1, out var enter1);
                var lerp1 = enter1 / dir1.magnitude;

                ray2.origin = original.Vertices[triangles[j + singleIndex]];
                var dir2 = original.Vertices[triangles[j + ((singleIndex + 2) % 3)]] - original.Vertices[triangles[j + singleIndex]];
                ray2.direction = dir2;
                plane.Raycast(ray2, out var enter2);
                var lerp2 = enter2 / dir2.magnitude;

                if (fillHole)
                {
                    AddEdge(i,
                        partMesh,
                        left ? plane.normal * -1f : plane.normal,
                        ray1.origin + ray1.direction.normalized * enter1,
                        ray2.origin + ray2.direction.normalized * enter2,
                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 2) % 3)]], lerp2));
                }

                if (sideCount == 1)
                {
                    Vector3 vert = ray2.origin + ray2.direction.normalized * enter2;

                    //if (checkOverlap)
                    //{
                    //    if (vert != CheckTriValid(original,
                    //    original.Vertices[triangles[j + singleIndex]],
                    //    ray1.origin + ray1.direction.normalized * enter1,
                    //    vert))
                    //    {
                    //        continue;
                    //    }
                    //}


                    partMesh.AddTriangle(i,
                                        original.Vertices[triangles[j + singleIndex]],
                                        //Vector3.Lerp(originalMesh.vertices[triangles[j + singleIndex]], originalMesh.vertices[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        //Vector3.Lerp(originalMesh.vertices[triangles[j + singleIndex]], originalMesh.vertices[triangles[j + ((singleIndex + 2) % 3)]], lerp2),
                                        ray1.origin + ray1.direction.normalized * enter1,
                                        vert,
                                        original.Normals[triangles[j + singleIndex]],
                                        Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 2) % 3)]], lerp2),
                                        original.UV[triangles[j + singleIndex]],
                                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 2) % 3)]], lerp2));

                    continue;
                }

                if (sideCount == 2)
                {
                    Vector3 vertA = original.Vertices[triangles[j + ((singleIndex + 2) % 3)]];

                    //if (checkOverlap)
                    //{
                    //    if (vertA != CheckTriValid(original, ray1.origin + ray1.direction.normalized * enter1,
                    //        original.Vertices[triangles[j + ((singleIndex + 1) % 3)]],
                    //        vertA))
                    //    {
                    //        continue;
                    //    }
                    //}

                    partMesh.AddTriangle(i,
                                        ray1.origin + ray1.direction.normalized * enter1,
                                        original.Vertices[triangles[j + ((singleIndex + 1) % 3)]],
                                        vertA,
                                        Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        original.Normals[triangles[j + ((singleIndex + 1) % 3)]],
                                        original.Normals[triangles[j + ((singleIndex + 2) % 3)]],
                                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        original.UV[triangles[j + ((singleIndex + 1) % 3)]],
                                        original.UV[triangles[j + ((singleIndex + 2) % 3)]]);


                    Vector3 vertB = ray2.origin + ray2.direction.normalized * enter2;
                    //if (checkOverlap)
                    //{
                    //    if (vertB != CheckTriValid(original, ray1.origin + ray1.direction.normalized * enter1,
                    //        original.Vertices[triangles[j + ((singleIndex + 2) % 3)]],
                    //        vertB))
                    //    {
                    //        continue;
                    //    }
                    //}



                    partMesh.AddTriangle(i,
                                        ray1.origin + ray1.direction.normalized * enter1,
                                        original.Vertices[triangles[j + ((singleIndex + 2) % 3)]],
                                        vertB,
                                        Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        original.Normals[triangles[j + ((singleIndex + 2) % 3)]],
                                        Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 2) % 3)]], lerp2),
                                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                        original.UV[triangles[j + ((singleIndex + 2) % 3)]],
                                        Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 2) % 3)]], lerp2));
                    continue;
                }
            }
        }

        //Debug.LogWarning("LIST TO TRIANGULATE : " + vertices.Count);
        //foreach(Vector3 vert in vertices)
        //{
        //    Debug.Log(vert);
        //}
        //Debug.LogWarning("END LIST");

        partMesh.FillArrays();

        return partMesh;
    }

    //Use barycentric approach to determine if their is a vertex on the triangle
    private Vector3 CheckTriValid(PartMesh original, Vector3 vert1, Vector3 vert2, Vector3 vert3)
    {
        Vector3[] verts = original.Vertices;

        foreach(Vector3 vertex in verts)
        {
            if (OnEdge(vertex, vert1) || OnEdge(vertex, vert2) || OnEdge(vertex, vert3))
            {
                continue;
            }


            //pseudo
            // Compute vectors        
            //v0 = C - A
            //v1 = B - A
            //v2 = P - A
            //
            //// Compute dot products
            //            dot00 = dot(v0, v0)
            //dot01 = dot(v0, v1)
            //dot02 = dot(v0, v2)
            //dot11 = dot(v1, v1)
            //dot12 = dot(v1, v2)
            //
            //// Compute barycentric coordinates
            //            invDenom = 1 / (dot00 * dot11 - dot01 * dot01)
            //u = (dot11 * dot02 - dot01 * dot12) * invDenom
            //v = (dot00 * dot12 - dot01 * dot02) * invDenom
            //
            //// Check if point is in triangle
            //return (u >= 0) && (v >= 0) && (u + v < 1)

            Vector3 lineA = vert3 - vert1;
            Vector3 lineB = vert2 - vert1;
            Vector3 lineC = vertex - vert1;

            float dotAA = Vector3.Dot(lineA, lineA);
            float dotAB = Vector3.Dot(lineA, lineB);
            float dotAC = Vector3.Dot(lineA, lineC);
            float dotBB = Vector3.Dot(lineB, lineB);
            float dotBC = Vector3.Dot(lineB, lineC);

            float val = 1 / (dotAA * dotBB - dotAB * dotAB);
            float u = (dotBB * dotAC - dotAB * dotBC) * val;
            float v = (dotAA * dotBC - dotAB * dotAC) * val;

            if ((v > 0) && (u > 0) && (u + v < 1))
            {
                //Debug.LogWarning("OVERLAPPING VERTEX");
                //Debug.Log("P : " + vertex);
                //Debug.Log("T : " + vert1 + ", " + vert2 + ", " + vert3);
                //Debug.Log("V  : " + v);
                //Debug.Log("U : " + u);
                //Debug.Log("UV : " + u + v);

                return vertex;
            }
        }

        return vert3;
    }

    private bool OnEdge(Vector3 vertA, Vector3 vertB)
    {

        return vertA == vertB;
    }


    private void AddEdge(int subMesh, PartMesh partMesh, Vector3 normal, Vector3 vertex1, Vector3 vertex2, Vector2 uv1, Vector2 uv2)
    {
        if (!edgeSet)
        {
            edgeSet = true;
            edgeVertex = vertex1;
            edgeUV = uv1;
        }
        else
        {
            edgePlane.Set3Points(edgeVertex, vertex1, vertex2);

            partMesh.AddTriangle(subMesh,
                                edgeVertex,
                                edgePlane.GetSide(edgeVertex + normal) ? vertex1 : vertex2,
                                edgePlane.GetSide(edgeVertex + normal) ? vertex2 : vertex1,
                                normal,
                                normal,
                                normal,
                                edgeUV,
                                uv1,
                                uv2);
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

        for(int i=0; i < tris.Length; i+= 3)
        {
            Vector3 pointA = verts[tris[i]];
            Vector3 pointB = verts[tris[i+1]];
            Vector3 pointC = verts[tris[i+2]];

            vol += VolumeOfTri(pointA, pointB, pointC);
        }

        return Mathf.Abs(vol);
    }

    public class PartMesh
    {
        private List<Vector3> _Verticies = new List<Vector3>();
        private List<Vector3> _Normals = new List<Vector3>();
        private List<List<int>> _Triangles = new List<List<int>>();
        private List<Vector2> _UVs = new List<Vector2>();
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public int[][] Triangles;
        public Vector2[] UV;
        public GameObject GameObject;
        public Bounds Bounds = new Bounds();

        public PartMesh()
        {

        }

        public void AddTriangle(int submesh, Vector3 vert1, Vector3 vert2, Vector3 vert3, Vector3 normal1, Vector3 normal2, Vector3 normal3, Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            if (_Triangles.Count - 1 < submesh)
                _Triangles.Add(new List<int>());
            _Triangles[submesh].Add(_Verticies.Count);
            _Verticies.Add(vert1);
            _Triangles[submesh].Add(_Verticies.Count);
            _Verticies.Add(vert2);
            _Triangles[submesh].Add(_Verticies.Count);
            _Verticies.Add(vert3);
            _Normals.Add(normal1);
            _Normals.Add(normal2);
            _Normals.Add(normal3);
            _UVs.Add(uv1);
            _UVs.Add(uv2);
            _UVs.Add(uv3);

            Bounds.min = Vector3.Min(Bounds.min, vert1);
            Bounds.min = Vector3.Min(Bounds.min, vert2);
            Bounds.min = Vector3.Min(Bounds.min, vert3);
            Bounds.max = Vector3.Min(Bounds.max, vert1);
            Bounds.max = Vector3.Min(Bounds.max, vert2);
            Bounds.max = Vector3.Min(Bounds.max, vert3);
        }

        public void FillArrays()
        {
            Vertices = _Verticies.ToArray();
            Normals = _Normals.ToArray();
            UV = _UVs.ToArray();
            Triangles = new int[_Triangles.Count][];
            for (var i = 0; i < _Triangles.Count; i++)
                Triangles[i] = _Triangles[i].ToArray();
        }

        public void MakeGameobject(Slicer original)
        {
            GameObject = new GameObject(original.name);
            GameObject.layer = original.gameObject.layer;
            GameObject.transform.position = original.transform.position;
            GameObject.transform.rotation = original.transform.rotation;
            GameObject.transform.localScale = original.transform.localScale;
            if (original.carryTag)
            {
                GameObject.tag = original.gameObject.tag;
            }
            else
            {
                GameObject.tag = "Shard";
            }

            var mesh = new Mesh();
            mesh.name = original.GetComponent<MeshFilter>().mesh.name;

            mesh.vertices = Vertices;
            mesh.normals = Normals;
            mesh.uv = UV;
            for (var i = 0; i < Triangles.Length; i++)
                mesh.SetTriangles(Triangles[i], i, true);
            Bounds = mesh.bounds;

            var renderer = GameObject.AddComponent<MeshRenderer>();
            renderer.materials = original.GetComponent<MeshRenderer>().materials;

            var filter = GameObject.AddComponent<MeshFilter>();
            filter.mesh = mesh;

            var collider = GameObject.AddComponent<MeshCollider>();
            collider.convex = true;

            var rigidbody = GameObject.AddComponent<Rigidbody>();
            rigidbody.mass = original.mass;
            var meshDestroy = GameObject.AddComponent<Slicer>();
            meshDestroy.CutCascades = original.CutCascades;
            meshDestroy.ExplodeForce = original.ExplodeForce;
            meshDestroy.minSize = original.minSize;
            meshDestroy.destroyParticleMaterial = original.destroyParticleMaterial;
            meshDestroy.fillHole = original.fillHole;
            meshDestroy.checkSize = original.checkSize;
            meshDestroy.checkOverlap = original.checkOverlap;
        }
    }

}