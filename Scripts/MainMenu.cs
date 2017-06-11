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
            gt.onMenuClick += Gt_onMenuClick;
        }

        private void Gt_onMenuClick(Controller controller)
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
