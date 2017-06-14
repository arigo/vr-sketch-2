using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


namespace VRSketch2
{
    public class MoveVertexAction : ModelAction
    {
        public static readonly Color COLOR = new Color(255 / 255f, 96 / 255f, 255 / 255f);
        public static readonly Color GUIDE_COLOR = new Color(255 / 255f, 248 / 255f, 96 / 255f);

        Vertex vertex;
        Vector3 origin;
        struct TempEdge { public EdgeSelection sel; public Vertex far_vertex; public Vector3 direction; }
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
                        sel = new EdgeSelection { face = face, num = face.VertexNum(i - 1) },
                        far_vertex = face.GetVertex(i - 1),
                        direction = origin - face.GetVertex(i - 1).position
                    });
                    edges.Add(new TempEdge
                    {
                        sel = new EdgeSelection { face = face, num = i },
                        far_vertex = face.GetVertex(i + 1),
                        direction = origin - face.GetVertex(i + 1).position
                    });
                    face_rends.Add(render.GetFaceRenderer(face));
                }
            }
        }

        Subspace SnapPosition(Vector3 pos, out EdgeSelection closest_edge)
        {
            closest_edge = null;

            Vector3 rpos = pos - origin;

            if (rpos.magnitude < Selection.DISTANCE_VERTEX_MIN)
            {
                /* snap to original position */
                //foreach (var edge in edges)
                //    AddSelection(new VertexSelection { vertex = edge.far_vertex }, Darker(Selection.SELECTION_COLOR));
                return new Subspace0();
            }

            /* try to snap to a direction */
            float closest = Selection.DISTANCE_EDGE_MIN;
            Vector3 closest_rpos = Vector3.zero;

            foreach (var edge in edges)
            {
                Vector3 shift = edge.direction - (origin - edge.far_vertex.position);
                Vector3 test_rpos = Vector3.Project(rpos - shift, edge.direction) + shift;
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
                return new Subspace1(closest_rpos);
            }

            /* try to snap to a plane */
            closest = Selection.DISTANCE_FACE_MIN;
            Vector3 closest_normal = Vector3.zero;
            Face closest_face = null;

            for (int i = 0; i < edges.Count; i += 2)
            {
                Vector3 plane_normal = Vector3.Cross(edges[i].direction, edges[i + 1].direction);
                plane_normal.Normalize();
                float distance = Mathf.Abs(Vector3.Dot(rpos, plane_normal));
                if (distance < closest)
                {
                    closest = distance * 0.99f;
                    closest_face = edges[i].sel.face;
                    closest_normal = plane_normal;
                }
            }
            if (closest_face != null)
            {
                AddSelection(new FaceSelection { face = closest_face, color_in_face = true }, COLOR);
                return new Subspace2(closest_normal);
            }

            return new Subspace3();   /* no snapping */
        }

        public override void Drag()
        {
            Vector3 pos = render.world.InverseTransformPoint(ctrl.position);
            EdgeSelection closest_edge;
            var subspace = SnapPosition(pos, out closest_edge);
            var ptsubspace = new PointedSubspace(subspace, origin);

            Vector3 otherpos = render.world.InverseTransformPoint(other_ctrl.position);
            Selection othersel = Selection.FindClosest(otherpos, render.model, color_in_face: true);
            Vector3? dummyedge_from = null;
            string other_ctrl_hint = null;
            if (othersel != null)
            {
                AddSelection(othersel, GUIDE_COLOR);

                if (!othersel.ContainsVertex(vertex))
                {
                    PointedSubspace otherptsubspace = othersel.GetPointedSubspace();

                    if (othersel is VertexSelection)
                    {
                        Vertex v = ((VertexSelection)othersel).vertex;
                        var foot = v.position;
                        otherptsubspace = new PointedSubspace(subspace.NormalSubspace(), foot);

                        if (closest_edge != null && closest_edge.ContainsVertex(v))
                        {
                            float distance = Vector3.Distance(v.position, pos);
                            int cm_distance = Mathf.RoundToInt(distance * 100 / 5) * 5;
                            other_ctrl_hint = "clamped at " + cm_distance + " cm";

                            var point0 = v.position + cm_distance * 0.01f * (origin - v.position).normalized;
                            ptsubspace = new PointedSubspace(new Subspace0(), point0);
                            otherptsubspace = PointedSubspace.Void();
                        }
                        else if (subspace is Subspace0)
                        {
                            float distance = Vector3.Distance(v.position, origin);
                            int mm_distance = Mathf.RoundToInt(distance * 1000);
                            other_ctrl_hint = (mm_distance / 10.0).ToString() + " cm";
                        }
                    }
                    else if (othersel is EdgeSelection)
                    {
                        Vertex v1, v2;
                        ((EdgeSelection)othersel).GetVertices(out v1, out v2);
                        Vector3 foot1 = v1.position;
                        Vector3 foot2 = v2.position;

                        Subspace orthogonal_plane = new Subspace2(foot2 - foot1);
                        var ptsub1 = new PointedSubspace(orthogonal_plane, foot1);
                        var ptsub2 = new PointedSubspace(orthogonal_plane, foot2);

                        var dist0 = otherptsubspace.Distance(pos);
                        var dist1 = ptsub1.Distance(pos);
                        var dist2 = ptsub2.Distance(pos);

                        if (dist1 <= dist0 && dist1 <= dist2)
                        {
                            otherptsubspace = ptsub1;
                            othersel = new VertexSelection { vertex = v1 };
                        }
                        else if (dist2 <= dist0 && dist2 <= dist1)
                        {
                            otherptsubspace = ptsub2;
                            othersel = new VertexSelection { vertex = v2 };
                        }
                    }

                    if (otherptsubspace.Distance(pos) < Selection.DISTANCE_VERTEX_MIN)
                    {
                        ptsubspace = ptsubspace.IntersectedWith(otherptsubspace);
                        dummyedge_from = othersel.Center();
                    }
                }
            }

            pos = ptsubspace.Snap(pos);
            vertex.position = pos;
            foreach (var face_rend in face_rends)
                face_rend.ComputeMesh();

            if (dummyedge_from.HasValue)
                AddSelection(EdgeSelection.DummyEdgeSelection(dummyedge_from.Value, pos), GUIDE_COLOR);
            AddSelection(new VertexSelection { vertex = vertex }, subspace is Subspace0 ? Darker(COLOR) : COLOR);
            SelectionFinished();

            if (other_ctrl.CurrentHoverTracker() == null)
            {
                ControllerMode.Get(other_ctrl).UpdatePointer(render, EMode.Guide);
                other_ctrl.SetControllerHints(trigger: other_ctrl_hint);
            }
        }
    }
}