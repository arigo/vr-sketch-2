using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


namespace VRSketch2
{
    public class ExtrudeEdgeAction : ModelAction
    {
        public static readonly Color COLOR = new Color(96 / 255f, 255 / 255f, 96 / 255f);

        Face face;
        int num;
        Vertex v1, v2;
        Vector3 origin, org_direction;
        float grab_fraction;
        List<FaceRenderer> face_rends;

        public ExtrudeEdgeAction(Render render, Controller ctrl, EdgeSelection sel)
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
            foreach (var face in render.model.faces)
            {
                if (face.vertices.IndexOf(v1) >= 0 || face.vertices.IndexOf(v2) >= 0)
                    face_rends.Add(render.GetFaceRenderer(face));
            }
        }

        public override void Drag()
        {
            Vector3 pos = render.world.InverseTransformPoint(ctrl.position);
            Vector3 rpos = pos - origin;
            Vector3 test_rpos = Vector3.Project(rpos, org_direction);
            float distance = Vector3.Distance(rpos, test_rpos);
            if (distance < Selection.DISTANCE_EDGE_MIN)
            {
                AddSelection(new EdgeSelection { face = face, num = num }, Darker(COLOR));
                pos = origin;
            }
            else
            {
                AddSelection(new EdgeSelection { face = face, num = num }, COLOR);
                pos -= org_direction * grab_fraction;
            }

            v1.position = pos;
            v2.position = pos + org_direction;
            foreach (var face_rend in face_rends)
                face_rend.ComputeMesh();

            SelectionFinished();
        }
    }
}