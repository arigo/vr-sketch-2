using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


namespace VRSketch2
{
    public class ModelAction
    {
        public Render render;
        public Controller ctrl;

        protected ModelAction(Render render, Controller ctrl)
        {
            this.render = render;
            this.ctrl = ctrl;
        }

        public virtual void Drag()
        {
            /* usually overridden */
        }

        public virtual void Stop()
        {
            SelectionFinished();
        }

        struct TempSel { public Selection sel; public Color col; }
        List<TempSel> temp_selection = new List<TempSel>();
        List<TempSel> temp_added = new List<TempSel>();

        protected void AddSelection(Selection sel, Color color)
        {
            temp_added.Add(new TempSel { sel = sel, col = color });
        }

        protected void SelectionFinished()
        {
            /* NB. all Leave() must be done before all Enter() 
             */
            int keep_common = 0;
            while (keep_common < temp_selection.Count && keep_common < temp_added.Count)
            {
                TempSel t1 = temp_selection[keep_common];
                TempSel t2 = temp_added[keep_common];
                if (t1.col != t2.col || !t1.sel.SameSelection(t2.sel))
                    break;
                keep_common++;
            }

            while (temp_selection.Count > keep_common)
            {
                TempSel tmp = temp_selection[temp_selection.Count - 1];
                temp_selection.RemoveAt(temp_selection.Count - 1);
                tmp.sel.Leave();
            }

            for (int i = keep_common; i < temp_added.Count; i++)
            {
                TempSel tmp = temp_added[i];
                tmp.sel.Enter(render, tmp.col);
                temp_selection.Add(tmp);
            }
            temp_added.Clear();

            foreach (var tmp in temp_selection)
                tmp.sel.Follow(render);
        }

        public static Color Darker(Color color)
        {
            return Color.Lerp(Color.black, color, 0.7f);
        }
    }
}
