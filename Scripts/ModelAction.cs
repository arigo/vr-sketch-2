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
        List<FaceRenderer> face_rends;
        List<Transform> highlight_edges;

        public MoveVertexAction(Render render, Controller ctrl, VertexSelection sel)
        {
            this.render = render;
            this.ctrl = ctrl;
            vertex = sel.vertex;

            origin = vertex.position;
            directions = new List<Vector3>();
            face_rends = new List<FaceRenderer>();
            foreach (var face in render.model.faces)
            {
                int i = face.vertices.IndexOf(vertex);
                if (i >= 0)
                {
                    directions.Add(origin - face.GetVertex(i - 1).position);
                    directions.Add(origin - face.GetVertex(i + 1).position);
                    face_rends.Add(render.GetFaceRenderer(face));
                }
            }

            move_point = Object.Instantiate(render.moveVertexPrefab);
            highlight_edges = new List<Transform>();
        }

        Vector3 SnapPosition(Vector3 pos, List<Vector3> highlight_edges)
        {
            Vector3 rpos = pos - origin;

            if (rpos.magnitude < Selection.DISTANCE_VERTEX_MIN)
            {
                /* snap to original position */
                foreach (var direction in directions)
                    highlight_edges.Add(origin - direction);
                return origin;
            }

            /* try to snap to a direction */
            float closest = Selection.DISTANCE_EDGE_MIN;
            Vector3 closest_rpos = Vector3.zero;
            Vector3 closest_direction = Vector3.zero;

            foreach (var direction in directions)
            {
                Vector3 test_rpos = Vector3.Project(rpos, direction);
                float distance = Vector3.Distance(rpos, test_rpos);
                if (distance < closest)
                {
                    closest = distance * 0.99f;
                    closest_rpos = test_rpos;
                    closest_direction = direction;
                }
            }
            if (closest_rpos != Vector3.zero)
            {
                highlight_edges.Add(origin - closest_direction);
                return closest_rpos + origin;
            }

            /* try to snap to a plane */
            closest = Selection.DISTANCE_FACE_MIN;
            closest_rpos = Vector3.zero;
            int closest_i = -1;

            for (int i = 0; i < directions.Count; i += 2)
            {
                Vector3 plane_normal = Vector3.Cross(directions[i], directions[i + 1]);
                Vector3 test_rpos = Vector3.ProjectOnPlane(rpos, plane_normal);
                float distance = Vector3.Distance(rpos, test_rpos);
                if (distance < closest)
                {
                    closest = distance * 0.99f;
                    closest_rpos = test_rpos;
                    closest_i = i;
                }
            }
            if (closest_rpos != Vector3.zero)
            {
                highlight_edges.Add(origin - directions[closest_i]);
                highlight_edges.Add(origin - directions[closest_i + 1]);
                return closest_rpos + origin;
            }

            return pos;   /* no snapping */
        }

        void SetHighlightCount(int count)
        {
            while (highlight_edges.Count > count)
            {
                Object.Destroy(highlight_edges[highlight_edges.Count - 1].gameObject);
                highlight_edges.RemoveAt(highlight_edges.Count - 1);
            }
            while (highlight_edges.Count < count)
            {
                highlight_edges.Add(Object.Instantiate(render.edgeAlignmentPrefab));
            }
        }

        public override void Drag()
        {
            Vector3 pos = render.world.InverseTransformPoint(ctrl.position);
            var hedges = new List<Vector3>();
            pos = SnapPosition(pos, hedges);
            move_point.position = render.world.TransformPoint(pos);
            vertex.position = pos;
            foreach (var face_rend in face_rends)
                face_rend.ComputeMesh();

            SetHighlightCount(hedges.Count);
            for (int i = 0; i < hedges.Count; i++)
                EdgeSelection.PositionEdge(highlight_edges[i], render.world.TransformPoint(hedges[i]),
                                           move_point.position);
        }

        public override void Stop()
        {
            Object.Destroy(move_point.gameObject);
            SetHighlightCount(0);
        }
    }
}
