using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;
using System;

namespace VRSketch2
{
    public class FaceRenderer : MonoBehaviour
    {
        public Face face;
        public Render render;

        public void ComputeMesh()
        {
            var vpositions = new List<Vector3>();
            var vnormals = new List<Vector3>();
            var triangles = new List<int>();

            /* add vertices and normals to the lists, twice, for the opposite normals */
            int face_vstart = vpositions.Count;
            foreach (var vertex in face.vertices)
            {
                vpositions.Add(vertex.position);
                vnormals.Add(face.plane.normal);
            }

            int face_vstart_back = vpositions.Count;
            foreach (var vertex in face.vertices)
            {
                vpositions.Add(vertex.position);
                vnormals.Add(-face.plane.normal);
            }

            /* cast the vertexes on the 2D plane, so we can compute triangulation. */
            var uvs = face.ProjectOnPlane();
            var triangulation = new Triangulator(uvs).Triangulate();

            /* 'triangles' are given two copies of triangulation going different way
                and having different vertexes */
            for (var i = 0; i < triangulation.Length / 3; ++i)
            {
                triangles.Add(face_vstart + triangulation[3 * i]);
                triangles.Add(face_vstart + triangulation[3 * i + 1]);
                triangles.Add(face_vstart + triangulation[3 * i + 2]);
                triangles.Add(face_vstart_back + triangulation[3 * i + 2]);
                triangles.Add(face_vstart_back + triangulation[3 * i + 1]);
                triangles.Add(face_vstart_back + triangulation[3 * i]);
            }

            /* build the mesh */
            var mesh = new Mesh();
            mesh.vertices = vpositions.ToArray();
            mesh.normals = vnormals.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateBounds();

            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        public void SetHighlight(Material highlight_mat)
        {
            MeshRenderer mesh_rend = GetComponent<MeshRenderer>();
            mesh_rend.sharedMaterials = new Material[] { mesh_rend.sharedMaterial, highlight_mat };
        }

        public void ClearHighlight()
        {
            MeshRenderer mesh_rend = GetComponent<MeshRenderer>();
            mesh_rend.sharedMaterials = new Material[] { mesh_rend.sharedMaterial };
        }
    }


    public class Render : MonoBehaviour
    {
        public Material defaultMaterial;
        public Transform selectedPointPrefab, selectedEdgePrefab;
        public Material selectedFaceMaterial;

        public Model model;
        Dictionary<Face, FaceRenderer> face_renderers;

        void Start()
        {
            /* initial model, with just one face */
            model = new Model();
            var face = new Face();
            face.vertices.Add(new Vertex(0, 0.5f, 0));
            face.vertices.Add(new Vertex(1, 0.5f, 0));
            face.vertices.Add(new Vertex(1, 1, 0));
            face.vertices.Add(new Vertex(0, 1, 0));
            face.plane = new Plane(new Vector3(0, 0, 1), new Vector3(0, 1, 0));
            model.faces.Add(face);

            ComputeAllMeshes();

            var ht = Controller.HoverTracker(this);
            ht.computePriority = ComputePriority;
            ht.onMoveOver += Ht_onMoveOver;
            ht.onLeave += Ht_onLeave;
        }

        void ComputeAllMeshes()
        {
            face_renderers = new Dictionary<Face, FaceRenderer>();
            foreach (var face in model.faces)
                ComputeMesh(face);
        }

        void ComputeMesh(Face face)
        {
            FaceRenderer face_rend;
            if (!face_renderers.TryGetValue(face, out face_rend))
            {
                var go = new GameObject("face");
                go.transform.SetParent(transform);
                go.AddComponent<MeshRenderer>().sharedMaterial = defaultMaterial;
                go.AddComponent<MeshFilter>();
                face_rend = go.AddComponent<FaceRenderer>();
                face_rend.render = this;
                face_rend.face = face;
                face_renderers[face] = face_rend;
            }
            face_rend.ComputeMesh();
        }

        public FaceRenderer GetFaceRenderer(Face face)
        {
            return face_renderers[face];
        }


        /* ============  Controller interaction  ============ */

        private float ComputePriority(Controller controller)
        {
            Selection sel = Selection.FindClosest(controller.position, model);
            if (sel == null)
                return float.NegativeInfinity;
            return -sel.Distance(controller.position);
        }

        Selection current_sel;

        private void Ht_onMoveOver(Controller controller)
        {
            Selection sel = Selection.FindClosest(controller.position, model);
            if (sel != current_sel)
            {
                Ht_onLeave(controller);
                current_sel = sel;
                if (current_sel != null)
                    current_sel.Enter(this);
            }
        }

        private void Ht_onLeave(Controller controller)
        {
            if (current_sel != null)
            {
                current_sel.Leave();
                current_sel = null;
            }
        }
    }
}
