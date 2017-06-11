using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


namespace VRSketch2
{
    public class ModelAction
    {
        public virtual void Drag() { }
        public virtual void Stop() { }
    }


    public class MoveVertexAction : ModelAction
    {
        Render render;
        Controller ctrl;
        Vertex vertex;
        Transform move_point;
        Vector3 origin;
        List<Vector3> directions;
        
        public MoveVertexAction(Render render, Controller ctrl, VertexSelection sel)
        {
            this.render = render;
            this.ctrl = ctrl;
            vertex = sel.vertex;

            origin = vertex.position;
            directions = new List<Vector3>();
            foreach (var face in render.model.faces)
            {
                int i = face.vertices.IndexOf(vertex);
                if (i >= 0)
                {
                    directions.Add(origin - face.GetVertex(i - 1).position);
                    directions.Add(origin - face.GetVertex(i + 1).position);
                }
            }

            move_point = Object.Instantiate(render.moveVertexPrefab);
        }

        Vector3 SnapPosition(Vector3 pos)
        {
            Vector3 rpos = pos - origin;

            if (rpos.magnitude < Selection.DISTANCE_VERTEX_MIN)
            {
                /* snap to original position */
                return origin;
            }

            /* try to snap to a direction */
            float closest = Selection.DISTANCE_EDGE_MIN;
            Vector3 closest_rpos = Vector3.zero;

            foreach (var direction in directions)
            {
                Vector3 test_rpos = Vector3.Project(rpos, direction);
                float distance = Vector3.Distance(rpos, test_rpos);
                if (distance < closest)
                {
                    closest = distance * 0.99f;
                    closest_rpos = test_rpos;
                }
            }
            if (closest_rpos != Vector3.zero)
                return closest_rpos + origin;

            /* try to snap to a plane */
            closest = Selection.DISTANCE_FACE_MIN;
            closest_rpos = Vector3.zero;

            for (int i = 0; i < directions.Count; i += 2)
            {
                Vector3 plane_normal = Vector3.Cross(directions[i], directions[i + 1]);
                Vector3 test_rpos = Vector3.ProjectOnPlane(rpos, plane_normal);
                float distance = Vector3.Distance(rpos, test_rpos);
                if (distance < closest)
                {
                    closest = distance * 0.99f;
                    closest_rpos = test_rpos;
                }
            }
            if (closest_rpos != Vector3.zero)
                return closest_rpos + origin;

            return pos;   /* no snapping */
        }

        public override void Drag()
        {
            Vector3 pos = render.world.InverseTransformPoint(ctrl.position);
            pos = SnapPosition(pos);
            move_point.position = render.world.TransformPoint(pos);
            vertex.position = pos;
        }

        public override void Stop()
        {
            Object.Destroy(move_point);
        }
    }
}
