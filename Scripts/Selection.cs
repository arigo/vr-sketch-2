using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRSketch2
{
    public abstract class Selection
    {
        public static Selection FindClosest(Vector3 point, Model model)
        {
            Selection result = VertexSelection.FindClosestVertex(point, model);
            if (result == null)
                result = EdgeSelection.FindClosestEdge(point, model);
            if (result == null)
                result = FaceSelection.FindClosestFace(point, model);
            return result;
        }

        public abstract float Distance(Vector3 point);
        public abstract void Enter(Render render);
        public abstract void Leave();
    }

    public class VertexSelection : Selection
    {
        public Vertex vertex;
        Transform selected;
        
        const float DISTANCE_VERTEX_MIN = 0.05f;

        static float DistanceToVertex(Vector3 v, Vector3 p)
        {
            return (v - p).magnitude;
        }

        public override float Distance(Vector3 point)
        {
            return DistanceToVertex(vertex.position, point);
        }

        public static VertexSelection FindClosestVertex(Vector3 point, Model model)
        {
            float distance_min = DISTANCE_VERTEX_MIN;
            Vertex closest = null;
            foreach (var vertex in model.GetVertices())
            {
                float distance = DistanceToVertex(vertex.position, point);
                if (distance < distance_min)
                {
                    closest = vertex;
                    distance_min = distance * 0.99f;
                }
            }

            if (closest == null)
                return null;
            return new VertexSelection { vertex = closest };
        }

        public override void Enter(Render render)
        {
            selected = Object.Instantiate(render.selectedPointPrefab);
            selected.position = vertex.position;
        }

        public override void Leave()
        {
            Object.Destroy(selected.gameObject);
        }
    }

    public class EdgeSelection : Selection
    {
        public Face face;
        public int num;
        Transform selected;

        const float DISTANCE_EDGE_MIN = 0.044f;

        static float DistanceToEdge(Vector3 v1, Vector3 v2, Vector3 p)
        {
            Vector3 p1 = v2 - v1;
            Vector3 p2 = p - v1;
            float dot = Vector3.Dot(p2, p1);
            if (dot > 0 && dot < p1.sqrMagnitude)
                return Vector3.ProjectOnPlane(p2, planeNormal: p1).magnitude;
            else
                return float.PositiveInfinity;
        }

        public override float Distance(Vector3 point)
        {
            Vertex v1, v2;
            GetVertices(out v1, out v2);
            return DistanceToEdge(v1.position, v2.position, point);
        }

        public static EdgeSelection FindClosestEdge(Vector3 point, Model model)
        {
            float distance_min = DISTANCE_EDGE_MIN;
            Face closest_face = null;
            int closest_num = 0;

            foreach (var face in model.faces)
            {
                Vector3 v2 = face.vertices[0].position;

                for (int i = face.vertices.Count - 1; i >= 0; i--)
                {
                    Vector3 v1 = face.vertices[i].position;
                    float distance = DistanceToEdge(v1, v2, point);
                    if (distance < distance_min)
                    {
                        closest_face = face;
                        closest_num = i;
                        distance_min = distance * 0.99f;
                    }
                    v2 = v1;
                }
            }

            if (closest_face == null)
                return null;
            return new EdgeSelection { face = closest_face, num = closest_num };
        }

        public void GetVertices(out Vertex v1, out Vertex v2)
        {
            v1 = face.vertices[num];
            int i = num + 1;
            if (i == face.vertices.Count)
                i = 0;
            v2 = face.vertices[i];
        }

        public override void Enter(Render render)
        {
            Vertex v1, v2;
            GetVertices(out v1, out v2);

            selected = Object.Instantiate(render.selectedEdgePrefab);

            Vector3 scale = selected.localScale;
            scale.y = Vector3.Distance(v1.position, v2.position) * 0.5f;
            selected.localScale = scale;
            selected.position = (v1.position + v2.position) * 0.5f;
            selected.rotation = Quaternion.LookRotation(v2.position - v1.position) * Quaternion.LookRotation(Vector3.up);
        }

        public override void Leave()
        {
            Object.Destroy(selected.gameObject);
        }
    }

    public class FaceSelection : Selection
    {
        public Face face;
        FaceRenderer face_rend;

        const float DISTANCE_TRIANGLE_MIN = 0.04f;

        public override float Distance(Vector3 point)
        {
            return face.plane.GetDistanceToPoint(point);
        }

        public static FaceSelection FindClosestFace(Vector3 point, Model model)
        {
            float distance_min = DISTANCE_TRIANGLE_MIN;
            Face closest = null;
            foreach (var face in model.faces)
            {
                float distance = Mathf.Abs(face.plane.GetDistanceToPoint(point));
                if (distance < distance_min && face.PointIsInside(point))
                {
                    closest = face;
                    distance_min = distance * 0.99f;
                }
            }

            if (closest == null)
                return null;
            return new FaceSelection { face = closest };
        }

        public override void Enter(Render render)
        {
            face_rend = render.GetFaceRenderer(face);
            face_rend.SetHighlight(render.selectedFaceMaterial);
        }

        public override void Leave()
        {
            face_rend.ClearHighlight();
        }
    }
}
