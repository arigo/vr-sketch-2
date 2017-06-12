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

        public Material SetHighlight(Material highlight_mat, Color color)
        {
            MeshRenderer mesh_rend = GetComponent<MeshRenderer>();
            Material original_mat = mesh_rend.sharedMaterial;
            if (highlight_mat != null)
            {
                mesh_rend.sharedMaterials = new Material[] { original_mat, highlight_mat };
                highlight_mat = mesh_rend.materials[1];    /* local copy */
                highlight_mat.SetColor("g_vOutlineColor", color);
                highlight_mat.SetColor("g_vMaskedOutlineColor", Color.Lerp(Color.black, color, 189 / 255f));
            }
            else
            {
                mesh_rend.materials[0].color = color;
            }
            return original_mat;
        }

        public void ClearHighlight(Material original_mat)
        {
            MeshRenderer mesh_rend = GetComponent<MeshRenderer>();
            mesh_rend.sharedMaterials = new Material[] { original_mat };
        }
    }
}
