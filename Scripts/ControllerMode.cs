using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


namespace VRSketch2
{
    public enum EMode { Create, Move };

    public class ControllerMode
    {
        public EMode mode = EMode.Move;
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

        public void UpdatePointer(Render render)
        {
            GameObject prefab = null;

            switch (mode)
            {
                case EMode.Create: prefab = render.createPointerPrefab; break;
                case EMode.Move: prefab = render.movePointerPrefab; break;
            }
            controller.SetPointer(prefab);
        }

        public void ChangeMode(EMode new_mode)
        {
            StopAction();
            Leave();
            mode = new_mode;
            /* will be re-entered automatically */
        }

        public void StartAction(Render render, Selection sel)
        {
            StopAction();

            switch (mode)
            {
                case EMode.Create:
                    break;

                case EMode.Move:
                    if (sel is VertexSelection)
                    {
                        current_action = new MoveVertexAction(render, controller, (VertexSelection)sel);
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
