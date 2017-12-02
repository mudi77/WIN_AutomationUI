using System;
using GMA = Gma.System.MouseKeyHook;
using FlaUI.UIA3;
using System.Windows.Threading;
using System.Windows.Forms;
using FlaUI.Core.AutomationElements.Infrastructure;
using win = System.Windows;
using Gma.System.MouseKeyHook;
using Newtonsoft.Json.Linq;
using System.Windows.Controls;

namespace Win_AutomationUI
{
    //RECORDING RELATED THINGS: NOT COMPLETED + HUGELY NEED FOR REDESIGN AND IMPROVEMENTS CONCEPTUAL AND TECHNICAL + PERFORMANCE
    public class Recording
    {
        private UIA3Automation automation = new UIA3Automation();
        private static Dispatcher dispatcher;
        private static GMA.IKeyboardMouseEvents events;

        private AutomationElement elCurrent = new UIA3Automation().FocusedElement();
        private AutomationElement elPrevious = new UIA3Automation().FocusedElement();
        private int skip4 = 4;
        private win.Media.Color hgltColorCurr = win.Media.Color.FromRgb(255, 255, 0);
        private int x = 0, y = 0;
        private bool focus = false;
        private bool mouseClick = true;
        private int timeBetweenClicks = 0;
        private JObject properties = new JObject();
        private static string propertiesText;

        private int GetSpeed(int _x, int _y)
        {
            int resX = Math.Abs(x - _x);
            int resY = Math.Abs(y - _y);
            x = _x;
            y = _y;
            int speed = resX > resY ? resX : resY;
            return speed;
        }
        public void Rec(bool recFlag)
        {
            if (recFlag)
            {
                Utils utils = new Utils();
                dispatcher = utils.BuildDispatcher("WORKER");
                dispatcher.BeginInvoke(DispatcherPriority.Background, new System.Action(() =>
                {
                    Record();
                }));
            }
            else
            {
                dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                events.KeyDown -= Events_KeyDown;
                events.KeyUp -= Events_KeyUp;
                dispatcher = null;
                events = null;
                LocatedObjects.objectsDB.Clear();
            }
        }
        private void Record()
        {
            events = GMA.Hook.GlobalEvents();            
            events.KeyDown += Events_KeyDown;
            events.KeyUp += Events_KeyUp;            
        }

        private void Events_KeyDown(object sender, win.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space && !focus)
            {
                focus = true;
                mouseClick = true;
                events.MouseMove += Events_MouseMove;
                events.MouseDownExt += Events_MouseDoubleClick; 
            }
        }
        private void Events_KeyUp(object sender, win.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space && focus)
            {
                focus = false;
                mouseClick = false;
                events.MouseMove -= Events_MouseMove;
                events.MouseUpExt -= Events_MouseDoubleClick;
    //          events.MouseUpExt += Events_MouseClickUP;
            }
        }
        private void Events_MouseMove(object sender, win.Forms.MouseEventArgs e)
        {
            if (GetSpeed(e.X, e.Y) < 3)
            {                         
                win.Point p = new win.Point(e.X, e.Y);
                try
                {
                    if (skip4 % 4 == 0)
                    {
                        elCurrent = automation.FromPoint(p);

                        if (!elCurrent.Equals(elPrevious))
                        {
                            elCurrent.DrawHighlight(false, hgltColorCurr, 50);
                            System.Diagnostics.Debug.Write(" ...render... " + elCurrent + "\n");
                            elPrevious = elCurrent;
                        }
                        else
                        {
                            elCurrent.DrawHighlight(false, hgltColorCurr, 100);
                        }
                    }
                    skip4++;
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Debug.Write("  EXCEPTION \n\n" + exception);
                }
            }
        }
        private void Events_MouseDoubleClick(object sender, MouseEventExtArgs e)
        {            
            if (e.Button.Equals(MouseButtons.Left))
            {
                if (e.IsMouseButtonDown) {
                    timeBetweenClicks = e.Timestamp - timeBetweenClicks;
                    if (0 < timeBetweenClicks && timeBetweenClicks < 500)
                    {
                        e.Handled = mouseClick;
                        AutomationElement element = automation.FromPoint(new win.Point(e.Location.X, e.Location.Y));

                        System.Diagnostics.Debug.Write(" AutomationElement :  " + element + "\n");
                        System.Diagnostics.Debug.Write(" left mouse click DOWN - DOUBLE - ON " + timeBetweenClicks + "\n");

                        DisplayRecordedObject(element);                                        
                    }
                    timeBetweenClicks = e.Timestamp;
                }
            }
        }
        private void Events_MouseClickUP(object sender, MouseEventExtArgs e)
        {
            e.Handled = mouseClick;
            if (e.Button.Equals(MouseButtons.Left))
            {
                if (e.IsMouseButtonDown)
                {                    
                    System.Diagnostics.Debug.Write(" left mouse click DOWN - OFF\n");
                }
            }
        }



        private void DisplayRecordedObject(AutomationElement obj) {
       
            double x = obj.Properties.ClickablePoint.Value.X;
            double y = obj.Properties.ClickablePoint.Value.Y;
            propertiesText = new Utils().WriteProperties(LocatedObjects.objectsDB.Count, x, y, obj.Name, obj.ControlType.ToString());

            Utils util = new Utils();
            Dispatcher disp = util.BuildDispatcher("UI");
            disp.Invoke(() => {
                ControlPanel.rightSide.Text = propertiesText;
            });
        }

        //edits just UI
        public void EditTreeUI(string what) {
            Utils util = new Utils();
            Dispatcher disp = util.BuildDispatcher("UI");
            disp.BeginInvoke(DispatcherPriority.Background, new System.Action(() => {

                TreeViewItem newItem = new TreeViewItem();
                if (what.Equals("AddItem")){
                    newItem.Header = ControlPanel.itemName.Text;
                    TreeViewItem selectedItem = (TreeViewItem)ControlPanel.jsonTree.SelectedItem;
                    selectedItem.Items.Add(newItem);
                }else {
                    newItem.Header = "_properties";
                    TreeViewItem props = new TreeViewItem();
                    props.Header = propertiesText;
                    newItem.Items.Add(props);
                    System.Diagnostics.Debug.WriteLine("props::: " + propertiesText);
                //    newItem.Items.Add(new TreeViewItem().SetValue() = propertiesText);
                    TreeViewItem selectedItem = (TreeViewItem)ControlPanel.jsonTree.SelectedItem;
                    selectedItem.Items.Add(newItem);
                }
            }));
        }


        public void SaveElementProperties() {
            string path = new Utils().GetSelectedTreeItem("path");
            System.Diagnostics.Debug.WriteLine("properties : " + path);

            EditTreeUI("AddProperties");
        }

    }
}