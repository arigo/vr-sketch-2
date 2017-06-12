using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRSketch2
{
    public class FaceRenderer : MonoBehaviour
    {
        public Face face;
        public Render render;

        public void ComputeMesh()
        {
            face.RecomputePlane();

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
            var triangulator = new Triangulator(uvs);
            var triangulation = triangulator.Triangulate();

            /* 'triangles' are given two copies of triangulation going different way
                and having different vertexes */
            for (var i = 0; i < triangulation.Length / 3; ++i)
            {
                triangles.Add(face_vstart + triangulation[3 * i]);
                triangles.Add(face_vstart + triangulation[3 * i + 2]);
                triangles.Add(face_vstart + triangulation[3 * i + 1]);
                triangles.Add(face_vstart_back + triangulation[3 * i + 1]);
                triangles.Add(face_vstart_back + triangulation[3 * i + 2]);
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

        class TempHighlight
        {
            public Material mat;
            public Color col;
            public bool active;
            public TempHighlight prev;
        }
        TempHighlight temp_highlight;
        Material original_mat;

        public object SetHighlight(Material highlight_mat, Color color)
        {
            if (original_mat == null)
            {
                MeshRenderer mesh_rend = GetComponent<MeshRenderer>();
                original_mat = mesh_rend.sharedMaterial;
            }
            temp_highlight = new TempHighlight { mat = highlight_mat, col = color, active = true, prev = temp_highlight };
            RefreshHighlight();
            return temp_highlight;
        }

        void RefreshHighlight()
        {
            MeshRenderer mesh_rend = GetComponent<MeshRenderer>();
            if (temp_highlight.mat != null)
            {
                mesh_rend.sharedMaterials = new Material[] { original_mat, temp_highlight.mat };
                Material highlight_mat = mesh_rend.materials[1];    /* local copy */
                highlight_mat.SetColor("g_vOutlineColor", temp_highlight.col);
                highlight_mat.SetColor("g_vMaskedOutlineColor", Color.Lerp(Color.black, temp_highlight.col, 189 / 255f));
            }
            else
            {
                mesh_rend.materials[0].color = temp_highlight.col;
            }
        }

        public void ClearHighlight(object remove_highlight)
        {
            TempHighlight tmp = (TempHighlight)remove_highlight;
            tmp.active = false;
            if (tmp != temp_highlight)
                return;

            while (true)
            {
                temp_highlight = temp_highlight.prev;
                if (temp_highlight == null)
                {
                    MeshRenderer mesh_rend = GetComponent<MeshRenderer>();
                    mesh_rend.sharedMaterials = new Material[] { original_mat };
                    original_mat = null;
                    break;
                }
                if (temp_highlight.active)
                {
                    RefreshHighlight();
                    break;
                }
            }
        }
    }
}
