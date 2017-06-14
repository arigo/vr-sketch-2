using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


namespace VRSketch2
{
    public enum EMode { Create, Move, Guide };

    public class ControllerMode
    {
        static EMode mode = EMode.Move;

        public Controller controller;
        public Selection current_sel;
        public ModelAction current_action;

        static ControllerMode[] controller_mode;

        public static ControllerMode Get(Controller controller)
        {
            ControllerMode cm = controller.GetAdditionalData(ref controller_mode);
            cm.controller = controller;
            return cm;
        }

        public void Leave()
        {
            if (current_sel != null)
            {
                current_sel.Leave();
                current_sel = null;
            }
        }

        public void UpdatePointer(Render render, EMode mode)
        {
            GameObject prefab = null;

            switch (mode)
            {
                case EMode.Create: prefab = render.createPointerPrefab; break;
                case EMode.Move: prefab = render.movePointerPrefab; break;
                case EMode.Guide: prefab = render.guidePointerPrefab; break;
            }
            controller.SetPointer(prefab);
        }

        public void UpdatePointer(Render render)
        {
            UpdatePointer(render, mode);
        }

        static public EMode CurrentMode()
        {
            return mode;
        }

        static public void ChangeMode(EMode new_mode)
        {
            foreach (var cm in controller_mode)
            {
                if (cm != null)
                {
                    cm.StopAction();
                    cm.Leave();
                    /* will be re-entered automatically */
                }
            }
            mode = new_mode;
        }

        public void StartAction(Render render, Selection sel)
        {
            StopAction();

            switch (mode)
            {
                case EMode.Create:
                    if (sel is EdgeSelection)
                    {
                        current_action = new ExtrudeEdgeAction(render, controller, (EdgeSelection)sel);
                        return;
                    }
                    break;

                case EMode.Move:
                    if (sel is VertexSelection)
                    {
                        current_action = new MoveVertexAction(render, controller, (VertexSelection)sel);
                        return;
                    }
                    if (sel is EdgeSelection)
                    {
                        current_action = new MoveEdgeAction(render, controller, (EdgeSelection)sel);
                        return;
                    }
                    break;
            }
            throw new System.NotImplementedException();
        }

        public void DragAction()
        {
            if (current_action != null)
                current_action.Drag();
        }

        public void StopAction()
        {
            if (current_action != null)
            {
                current_action.Stop();
                current_action = null;
            }
        }
    }
}
