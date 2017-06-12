using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


namespace VRSketch2
{
    public class Render : MonoBehaviour
    {
        public Transform world;
        public Material defaultMaterial;
        public Transform selectedPointPrefab, selectedEdgePrefab;
        public Material selectedFaceMaterial;

        public GameObject createPointerPrefab, movePointerPrefab;

        public Model model;
        Dictionary<Face, FaceRenderer> face_renderers;

        void Start()
        {
            /* initial model, with three faces */
            model = new Model();

            var vertices = new Vertex[]
            {
                new Vertex(0, 0.5f, 0),
                new Vertex(1, 0.5f, 0),
                new Vertex(1, 1, 0),
                new Vertex(0, 1, 0),
                new Vertex(1, 1, 1),
                new Vertex(0, 1, 1),
                new Vertex(1, 0.5f, 1),
            };

            var face = new Face();
            face.vertices.Add(vertices[0]);
            face.vertices.Add(vertices[1]);
            face.vertices.Add(vertices[2]);
            face.vertices.Add(vertices[3]);
            model.faces.Add(face);

            face = new Face();
            face.vertices.Add(vertices[3]);
            face.vertices.Add(vertices[2]);
            face.vertices.Add(vertices[4]);
            face.vertices.Add(vertices[5]);
            model.faces.Add(face);

            face = new Face();
            face.vertices.Add(vertices[1]);
            face.vertices.Add(vertices[2]);
            face.vertices.Add(vertices[4]);
            face.vertices.Add(vertices[6]);
            model.faces.Add(face);


            ComputeAllMeshes();

            var ht = Controller.HoverTracker(this);
            ht.computePriority = ComputePriority;
            ht.onMoveOver += Ht_onMoveOver;
            ht.onLeave += Ht_onLeave;
            ht.onTriggerDown += Ht_onTriggerDown;
            ht.onTriggerDrag += Ht_onTriggerDrag;
            ht.onTriggerUp += Ht_onTriggerUp;
        }

        void ComputeAllMeshes()
        {
            if (face_renderers != null)
            {
                foreach (var face_rend in face_renderers.Values)
                    Destroy(face_rend.gameObject);
            }

            face_renderers = new Dictionary<Face, FaceRenderer>();
            foreach (var face in model.faces)
                ComputeMesh(face);
        }

        void ComputeMesh(Face face)
        {
            GetFaceRenderer(face).ComputeMesh();
        }

        public FaceRenderer GetFaceRenderer(Face face)
        {
            FaceRenderer face_rend;
            if (!face_renderers.TryGetValue(face, out face_rend))
            {
                var go = new GameObject("face");
                go.transform.SetParent(world, worldPositionStays: false);
                go.AddComponent<MeshRenderer>().sharedMaterial = defaultMaterial;
                go.AddComponent<MeshFilter>();
                face_rend = go.AddComponent<FaceRenderer>();
                face_rend.render = this;
                face_rend.face = face;
                face_renderers[face] = face_rend;
            }
            return face_rend;
        }


        /* ============  Controller interaction  ============ */

        private float ComputePriority(Controller controller)
        {
            Vector3 point = world.InverseTransformPoint(controller.position);
            Selection sel = Selection.FindClosest(point, model);
            if (sel == null)
                return float.NegativeInfinity;
            return -sel.Distance(point);
        }

        private void Ht_onMoveOver(Controller controller)
        {
            ControllerMode.Get(controller).UpdatePointer(this);

            Selection sel = Selection.FindClosest(world.InverseTransformPoint(controller.position), model);
            var cm = ControllerMode.Get(controller);
            if (sel == null ? cm.current_sel == null : sel.SameSelection(cm.current_sel))
                return;

            cm.Leave();

            if (sel != null)
            {
                cm.current_sel = sel;
                sel.Enter(this, Selection.SELECTION_COLOR);
                sel.Follow(this);
            }
        }

        private void Ht_onLeave(Controller controller)
        {
            var cm = ControllerMode.Get(controller);
            cm.Leave();
        }

        private void Ht_onTriggerDown(Controller controller)
        {
            var cm = ControllerMode.Get(controller);
            if (cm.current_sel != null)
            {
                Selection sel = cm.current_sel;
                cm.Leave();
                cm.StartAction(this, sel);
            }
        }

        private void Ht_onTriggerDrag(Controller controller)
        {
            var cm = ControllerMode.Get(controller);
            cm.DragAction();
        }

        private void Ht_onTriggerUp(Controller controller)
        {
            var cm = ControllerMode.Get(controller);
            cm.StopAction();
        }
    }
}
