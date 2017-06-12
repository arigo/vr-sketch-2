using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


namespace VRSketch2
{
    public class MoveEdgeAction : ModelAction
    {
        public static readonly Color COLOR = new Color(255 / 255f, 96 / 255f, 255 / 255f);

        Face face;
        int num;
        Vertex v1, v2;
        Vector3 origin, org_direction;
        float grab_fraction;
        List<FaceRenderer> face_rends;
        struct TempFace { public Face face; public Plane plane; }
        List<TempFace> faces;
        List<Vector3> extra_edges;
        List<EdgeSelection> extra_edges_sel;

        public MoveEdgeAction(Render render, Controller ctrl, EdgeSelection sel)
            : base(render, ctrl)
        {
            face = sel.face;
            num = sel.num;
            v1 = face.GetVertex(num);
            v2 = face.GetVertex(num + 1);

            origin = v1.position;
            org_direction = v2.position - origin;

            Vector3 pos = render.world.InverseTransformPoint(ctrl.position);
            grab_fraction = Vector3.Dot(pos - origin, org_direction) / org_direction.sqrMagnitude;

            face_rends = new List<FaceRenderer>();
            faces = new List<TempFace>();
            extra_edges = new List<Vector3>();
            extra_edges_sel = new List<EdgeSelection>();
            foreach (var face in render.model.faces)
            {
                int i1 = face.vertices.IndexOf(v1);
                int i2 = face.vertices.IndexOf(v2);
                if (i1 < 0 && i2 < 0)
                    continue;

                face_rends.Add(render.GetFaceRenderer(face));

                if (i1 >= 0 && i2 >= 0)
                    faces.Add(new TempFace { face = face, plane = face.plane });

                if (i1 >= 0)
                    AddExtraEdges(face, i1, i2);
                if (i2 >= 0)
                    AddExtraEdges(face, i2, i1);
            }
        }

        void AddExtraEdges(Face face, int num, int exclude_num)
        {
            int other = face.VertexNum(num - 1);
            if (other != exclude_num)
                AddExtraEdge(face, other, num, other);

            other = face.VertexNum(num + 1);
            if (other != exclude_num)
                AddExtraEdge(face, num, num, other);
        }

        void AddExtraEdge(Face face, int num, int near_num, int far_num)
         {
            extra_edges.Add(face.GetVertex(near_num).position);
            extra_edges.Add(face.GetVertex(far_num).position);
            extra_edges_sel.Add(new EdgeSelection { face = face, num = num });

            //Debug.Log("Face: " + face + " num " + num + "  edge:  NEAR " + extra_edges[extra_edges.Count-2] + "  FAR " + extra_edges[extra_edges.Count - 1]);
        }

        public override void Drag()
        {
            Vector3 pos = render.world.InverseTransformPoint(ctrl.position);
            pos -= org_direction * grab_fraction;
            if (Vector3.Distance(origin, pos) < Selection.DISTANCE_VERTEX_MIN)
            {
                AddSelection(new EdgeSelection { face = face, num = num }, Darker(COLOR));
                foreach (var temp in faces)
                    AddSelection(new FaceSelection { face = temp.face, color_in_face = true }, COLOR);
                pos = origin;   
            }
            else
            {
                AddSelection(new EdgeSelection { face = face, num = num }, COLOR);

                Subspace subspace = new Subspace3();

                foreach (var temp in faces)
                {
                    float distance = Mathf.Abs(Vector3.Dot(pos - origin, temp.plane.normal));
                    if (distance < Selection.DISTANCE_FACE_MIN)
                    {
                        AddSelection(new FaceSelection { face = temp.face, color_in_face = true }, COLOR);
                        subspace = subspace.IntersectedWithPlane(temp.plane.normal);
                    }
                }

                float min_distance = Selection.DISTANCE_EDGE_MIN;
                int closest_edge = -1;
                for (int i = 0; i < extra_edges.Count; i += 2)
                {
                    Vector3 v1 = extra_edges[i];
                    Vector3 v2 = extra_edges[i + 1];
                    Vector3 endpos = pos + (v1 - origin);
                    float distance = Vector3.ProjectOnPlane(endpos - v1, planeNormal: v2 - v1).magnitude;
                    if (distance < min_distance)
                    {
                        min_distance = distance * 0.99f;
                        closest_edge = i;
                    }
                }
                if (closest_edge >= 0)
                {
                    AddSelection(extra_edges_sel[closest_edge / 2], COLOR);
                    subspace = subspace.IntersectedWithSingleVector(
                        extra_edges[closest_edge + 1] - extra_edges[closest_edge]);
                }

                pos = subspace.Project(pos - origin) + origin;
            }

            v1.position = pos;
            v2.position = pos + org_direction;
            foreach (var face_rend in face_rends)
                face_rend.ComputeMesh();

            SelectionFinished();
        }
    }
}