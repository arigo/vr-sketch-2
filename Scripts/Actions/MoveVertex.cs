using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


namespace VRSketch2
{
    public class MoveVertexAction : ModelAction
    {
        public static readonly Color COLOR = new Color(255 / 255f, 96 / 255f, 255 / 255f);

        Vertex vertex;
        Vector3 origin;
        struct TempEdge { public EdgeSelection sel; public Vector3 direction; }
        List<TempEdge> edges;
        List<FaceRenderer> face_rends;

        public MoveVertexAction(Render render, Controller ctrl, VertexSelection sel)
            : base(render, ctrl)
        {
            vertex = sel.vertex;

            origin = vertex.position;
            edges = new List<TempEdge>();
            face_rends = new List<FaceRenderer>();
            foreach (var face in render.model.faces)
            {
                int i = face.vertices.IndexOf(vertex);
                if (i >= 0)
                {
                    edges.Add(new TempEdge
                    {
                        sel = new EdgeSelection { face = face, num = i > 0 ? i - 1 : face.vertices.Count - 1 },
                        direction = origin - face.GetVertex(i - 1).position
                    });
                    edges.Add(new TempEdge
                    {
                        sel = new EdgeSelection { face = face, num = i },
                        direction = origin - face.GetVertex(i + 1).position
                    });
                    face_rends.Add(render.GetFaceRenderer(face));
                }
            }
        }

        Vector3 SnapPosition(Vector3 pos)
        {
            Vector3 rpos = pos - origin;

            if (rpos.magnitude < Selection.DISTANCE_VERTEX_MIN)
            {
                /* snap to original position */
                foreach (var edge in edges)
                    AddSelection(edge.sel, Darker(COLOR));
                return origin;
            }

            /* try to snap to a direction */
            float closest = Selection.DISTANCE_EDGE_MIN;
            Vector3 closest_rpos = Vector3.zero;
            Selection closest_edge = null;

            foreach (var edge in edges)
            {
                Vector3 test_rpos = Vector3.Project(rpos, edge.direction);
                float distance = Vector3.Distance(rpos, test_rpos);
                if (distance < closest)
                {
                    closest = distance * 0.99f;
                    closest_rpos = test_rpos;
                    closest_edge = edge.sel;
                }
            }
            if (closest_edge != null)
            {
                AddSelection(closest_edge, COLOR);
                return closest_rpos + origin;
            }

            /* try to snap to a plane */
            closest = Selection.DISTANCE_FACE_MIN;
            closest_rpos = Vector3.zero;
            Face closest_face = null;

            for (int i = 0; i < edges.Count; i += 2)
            {
                Vector3 plane_normal = Vector3.Cross(edges[i].direction, edges[i + 1].direction);
                Vector3 test_rpos = Vector3.ProjectOnPlane(rpos, plane_normal);
                float distance = Vector3.Distance(rpos, test_rpos);
                if (distance < closest)
                {
                    closest = distance * 0.99f;
                    closest_rpos = test_rpos;
                    closest_face = edges[i].sel.face;
                }
            }
            if (closest_rpos != Vector3.zero)
            {
                AddSelection(new FaceSelection { face = closest_face, color_in_face = true }, COLOR);
                return closest_rpos + origin;
            }

            return pos;   /* no snapping */
        }

        public override void Drag()
        {
            Vector3 pos = render.world.InverseTransformPoint(ctrl.position);
            pos = SnapPosition(pos);
            vertex.position = pos;
            foreach (var face_rend in face_rends)
                face_rend.ComputeMesh();

            AddSelection(new VertexSelection { vertex = vertex }, COLOR);
            SelectionFinished();
        }
    }
}