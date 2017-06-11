using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRSketch2
{
    public class Vertex
    {
        public Vector3 position;

        public Vertex(Vector3 position)
        {
            this.position = position;
        }
        public Vertex(float x, float y, float z)
        {
            position = new Vector3(x, y, z);
        }
    }


    public class Face
    {
        public Plane plane;
        public List<Vertex> vertices = new List<Vertex>();

        Vector3 plane1, plane2;

        public Vector2[] ProjectOnPlane()
        {
            Vector3 normal = plane.normal;
            if (normal.y < Mathf.Max(normal.x, normal.z))
                plane1 = new Vector3(0, 1, 0);
            else
                plane1 = new Vector3(1, 0, 0);

            plane1 = Vector3.ProjectOnPlane(plane1, normal).normalized;
            plane2 = Vector3.Cross(normal, plane1);

            var uvs = new Vector2[vertices.Count];
            for (int i = 0; i < uvs.Length; i++)
                uvs[i] = ProjectPointOnPlane(vertices[i].position);
            return uvs;
        }

        Vector2 ProjectPointOnPlane(Vector3 point)
        {
            return new Vector2(Vector3.Dot(point, plane1), Vector3.Dot(point, plane2));
        }

        public bool PointIsInside(Vector3 point)
        {
            int side = 0;
            Vector2[] uvs = ProjectOnPlane();
            Vector2 pt = ProjectPointOnPlane(point);

            Vector2 uv2 = uvs[0];
            for (int i = uvs.Length - 1; i >= 0; i--)
            {
                Vector2 uv1 = uvs[i];
                if (uv1.y < pt.y ^ uv2.y < pt.y)
                {
                    float x = (uv1.x * uv2.y - uv2.x * uv1.y) / (uv2.y - uv1.y);
                    if (x < pt.x)
                        side += (uv1.y < uv2.y) ? -1 : 1;
                }
                uv2 = uv1;
            }
            return side != 0;
        }
    }


    public class Model
    {
        public List<Face> faces = new List<Face>();

        public Vertex[] GetVertices()
        {
            var result = new List<Vertex>();
            var seen = new HashSet<Vertex>();

            foreach (var face in faces)
            {
                foreach (var vertex in face.vertices)
                {
                    if (seen.Add(vertex))
                        result.Add(vertex);
                }
            }
            return result.ToArray();
        }
    }
}
