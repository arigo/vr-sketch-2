﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRSketch2
{
    public abstract class Selection
    {
        public const float DISTANCE_VERTEX_MIN = 0.05f;
        public const float DISTANCE_EDGE_MIN = 0.044f;
        public const float DISTANCE_FACE_MIN = 0.04f;

        public static Selection FindClosest(Vector3 point, Model model, bool color_in_face = false)
        {
            Selection result = VertexSelection.FindClosestVertex(point, model);
            if (result == null)
                result = EdgeSelection.FindClosestEdge(point, model);
            if (result == null)
                result = FaceSelection.FindClosestFace(point, model, color_in_face);
            return result;
        }

        public static readonly Color SELECTION_COLOR = new Color(82 / 255f, 198 / 255f, 255 / 255f);

        public abstract bool SameSelection(Selection other);
        public abstract float Distance(Vector3 point);
        public abstract void Enter(Render render, Color color);
        public abstract void Follow(Render render);
        public abstract void Leave();
        public abstract Selection Clone();
        public abstract bool ContainsVertex(Vertex v);
        public abstract PointedSubspace GetPointedSubspace();
        public abstract Vector3 Center();
    }

    public class VertexSelection : Selection
    {
        public Vertex vertex;
        Transform selected;

        public override Selection Clone()
        {
            return new VertexSelection { vertex = vertex };
        }

        public override bool SameSelection(Selection other)
        {
            if (!(other is VertexSelection))
                return false;
            return vertex == ((VertexSelection)other).vertex;
        }

        static float DistanceToVertex(Vector3 v, Vector3 p)
        {
            return (v - p).magnitude;
        }

        public override float Distance(Vector3 point)
        {
            return DistanceToVertex(vertex.position, point);
        }

        public override bool ContainsVertex(Vertex v)
        {
            return v == vertex;
        }

        public override Vector3 Center()
        {
            return vertex.position;
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

        public override void Enter(Render render, Color color)
        {
            selected = Object.Instantiate(render.selectedPointPrefab);
            selected.GetComponent<Renderer>().material.color = color;
        }

        public override void Follow(Render render)
        {
            selected.position = render.world.TransformPoint(vertex.position);
        }

        public override void Leave()
        {
            Object.Destroy(selected.gameObject);
        }

        public override PointedSubspace GetPointedSubspace()
        {
            return new PointedSubspace(new Subspace0(), vertex.position);
        }
    }

    public class EdgeSelection : Selection
    {
        public Face face;
        public int num;
        Transform selected;

        static public EdgeSelection DummyEdgeSelection(Vector3 v1, Vector3 v2)
        {
            var dummy_face = new Face();
            dummy_face.vertices.Add(new Vertex(v1));
            dummy_face.vertices.Add(new Vertex(v2));
            return new EdgeSelection { face = dummy_face, num = 0 };
        }

        public override Selection Clone()
        {
            return new EdgeSelection { face = face, num = num };
        }

        public override bool SameSelection(Selection other)
        {
            if (!(other is EdgeSelection))
                return false;
            return face == ((EdgeSelection)other).face &&
                   num == ((EdgeSelection)other).num;
        }

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

        public override bool ContainsVertex(Vertex v)
        {
            Vertex v1, v2;
            GetVertices(out v1, out v2);
            return v == v1 || v == v2;
        }

        public override Vector3 Center()
        {
            Vertex v1, v2;
            GetVertices(out v1, out v2);
            return (v1.position + v2.position) * 0.5f;
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

        public static void PositionEdge(Transform tr, Vector3 p1, Vector3 p2)
        {
            Vector3 scale = tr.localScale;
            scale.y = Vector3.Distance(p1, p2) * 0.5f + 0.003f;
            tr.localScale = scale;
            tr.position = (p1 + p2) * 0.5f;
            if (p1 != p2)
                tr.rotation = Quaternion.LookRotation(p2 - p1) * Quaternion.LookRotation(Vector3.up);
        }

        public override void Enter(Render render, Color color)
        {
            selected = Object.Instantiate(render.selectedEdgePrefab);
            selected.GetComponent<Renderer>().material.color = color;
        }

        public override void Follow(Render render)
        {
            Vertex v1, v2;
            GetVertices(out v1, out v2);

            Vector3 p1 = render.world.TransformPoint(v1.position);
            Vector3 p2 = render.world.TransformPoint(v2.position);
            PositionEdge(selected, p1, p2);
        }

        public override void Leave()
        {
            Object.Destroy(selected.gameObject);
            selected = null;
        }

        public override PointedSubspace GetPointedSubspace()
        {
            Vertex v1, v2;
            GetVertices(out v1, out v2);
            return new PointedSubspace(new Subspace1(v2.position - v1.position), v1.position);
        }
    }

    public class FaceSelection : Selection
    {
        public Face face;
        public bool color_in_face = false;
        FaceRenderer face_rend;
        object highlight_token;

        public override Selection Clone()
        {
            return new FaceSelection { face = face, color_in_face = color_in_face };
        }

        public override bool SameSelection(Selection other)
        {
            if (!(other is FaceSelection))
                return false;
            return face == ((FaceSelection)other).face;
        }

        public override float Distance(Vector3 point)
        {
            return face.plane.GetDistanceToPoint(point);
        }

        public override bool ContainsVertex(Vertex v)
        {
            foreach (Vertex vertex in face.vertices)
                if (v == vertex)
                    return true;
            return false;
        }

        public override Vector3 Center()
        {
            Vector3 sum = Vector3.zero;
            foreach (Vertex vertex in face.vertices)
                sum += vertex.position;
            return sum / face.vertices.Count;
        }

        public static FaceSelection FindClosestFace(Vector3 point, Model model, bool color_in_face = false)
        {
            float distance_min = DISTANCE_FACE_MIN;
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
            return new FaceSelection { face = closest, color_in_face = color_in_face };
        }

        public override void Enter(Render render, Color color)
        {
            face_rend = render.GetFaceRenderer(face);
            highlight_token = face_rend.SetHighlight(color_in_face ? null : render.selectedFaceMaterial, color);
        }

        public override void Follow(Render render)
        {
        }

        public override void Leave()
        {
            face_rend.ClearHighlight(highlight_token);
        }

        public override PointedSubspace GetPointedSubspace()
        {
            Vector3 foot = face.plane.distance * face.plane.normal;
            return new PointedSubspace(new Subspace2(face.plane.normal), foot);
        }
    }
}
