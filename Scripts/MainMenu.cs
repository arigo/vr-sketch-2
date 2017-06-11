using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


namespace VRSketch2
{
    public class MainMenu : MonoBehaviour
    {
        private void Start()
        {
            var gt = Controller.GlobalTracker(this);
            gt.onMenuClick += Click;
            gt.onTriggerDown += Gt_onTriggerDown;
        }

        private void Gt_onTriggerDown(Controller controller)
        {
            /* are we far enough from anything? */
            Vector3 p = controller.position;
            Vector3 vi = Vector3.up * Selection.DISTANCE_FACE_MIN;
            Vector3 vj = Vector3.forward * Selection.DISTANCE_FACE_MIN;
            Vector3 vk = Vector3.right * Selection.DISTANCE_FACE_MIN;
            bool too_close = false;

            foreach (var render in GameObject.FindObjectsOfType<Render>())
            {
                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                        for (int k = -1; k <= 1; k++)
                            too_close |= Selection.FindClosest(
                                render.world.InverseTransformPoint(p + i * vi + j * vj + k * vk),
                                render.model) != null;
            }
            if (!too_close)
                Click(controller);
        }

        public void Click(Controller controller)
        {
            var menu = new Menu
            {
                ModeChoice("Creation mode", controller, EMode.Create),
                ModeChoice("Movement mode", controller, EMode.Move),
            };
            menu.MakePopup(controller, gameObject);
        }

        private Menu.Item ModeChoice(string text, Controller controller, EMode mode)
        {
            var cm = ControllerMode.Get(controller);
            if (cm.mode == mode)
                text = "✔ " + text;
            return new Menu.Item
            {
                text = text,
                onClick = () => cm.ChangeMode(mode),
            };
        }
    }
}
