using System;
using System.Linq;
using System.Collections.Generic;
using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Windows.Input;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using Newtonsoft.Json;
using FlaUI.Core.WindowsAPI;
using System.Runtime.InteropServices;
using System.Threading;

namespace Win_AutomationUI
{

    //THIS IS SOMETHING AS CORE: NOT COMPLETED + INTENTION WAS TO PUT HERE STUFF DIRECTLY RELATED TO AUTOMATION 
    //(to be considered to split it..) 

    public class AutomationCore : Control
    {
        public static FlaUI.Core.Application app = null;
        public static FlaUI.Core.AutomationElements.Window appWindow;
        private double WIDTH;
        private double HEIGHT;
        
        public static System.Diagnostics.Process appProcess = null;

        public static string FileContent = "";
        public static JContainer objectDetails = new JObject();
        private static readonly Dictionary<MouseButton, DateTime> LastClickTimes;
        private static readonly Dictionary<MouseButton, FlaUI.Core.Shapes.Point> LastClickPositions;

        static AutomationCore() {
            LastClickTimes = new Dictionary<MouseButton, DateTime>();
            LastClickPositions = new Dictionary<MouseButton, FlaUI.Core.Shapes.Point>();
        }
        public void StartThis(string appName, string appPath) {
            var aUI3 = new UIA3Automation();
            Process apps = Process.Start(appPath);

            System.Threading.Thread.Sleep(30000);
            Process[] SC = Process.GetProcesses();
            
            foreach (Process proc in SC)
            {
                System.Diagnostics.Debug.Write(" proc :: " + proc.ProcessName.ToLower() + " " + appName.ToLower() + "\n");
                if (proc.ProcessName.ToLower().Contains(appName.ToLower()))
                {
                    appProcess = proc;
                    
                    break;                    
                }
            }            
            app = FlaUI.Core.Application.Attach(appProcess);
            appWindow = app.GetMainWindow(aUI3, TimeSpan.FromMilliseconds(5000));

            System.Diagnostics.Debug.Write(" WIDTH " + appWindow.Properties.BoundingRectangle.Value.Width); 
            this.WIDTH = appWindow.Properties.BoundingRectangle.Value.Width;
            this.HEIGHT = appWindow.Properties.BoundingRectangle.Value.Height;
        }

        //temporary method for retrieving app. menu elements
        public void IdentifyMenuElements()
        {
            AutomationElement appMenu = appWindow.FindFirstByXPath($"*[@AutomationId='menu']");
            FlaUI.Core.Conditions.TrueCondition condition = new TrueCondition();
            FlaUI.Core.Definitions.TreeScope scope = FlaUI.Core.Definitions.TreeScope.Children;
            string menu = "Menu", menuItem = "", Item = "";
            int idx = 0;

            AutomationElement[] cts = appMenu.FindAll(scope, condition);
            for (int x = 0; x < cts.LongCount(); x++)
            {
                if (cts[x].Properties.ControlType.ToString().Equals("MenuItem"))
                {
                    menuItem = cts[x].Name;
                    idx++;
                    AutomationElement[] mnsi = cts[x].FindAllChildren();
                    for (int h = 0; h < mnsi.LongCount(); h++)
                    {
                        AutomationElement menuItemObject = mnsi[h];
                        mnsi[h].Click();                         

                        AutomationElement[] subComps = cts[x].FindAllChildren();
                        for (int d = 0; d < subComps.LongCount(); d++)
                        {
                            Item = subComps[d].Name;

                            if (Item == null || Item.Equals("") || menuItem.Equals(Item))
                            {
                                continue;
                            }
                            SaveElementAttributes("SceneComposer", menu, menuItem, menuItemObject, Item, idx++, subComps[d].FindFirstChild());
                        }
                    }
                }
            }                   
        FileContent = objectDetails.ToString();
        }
        

        public void Load() {
            OpenFileDialog ofDialog = new OpenFileDialog();
            ofDialog.ShowDialog();          
            try
            {
                using (StreamReader fileReader = new StreamReader(ofDialog.FileName))
                       FileContent = fileReader.ReadToEnd();                
            }
            catch (IOException ioEx)
            {
                MessageBox.Show("Unable to open the file. " + ioEx.Message, "Error");
            }
        }

        
        public void SaveElementAttributes(string appName, string menu, string menuItem, AutomationElement menuItemObj, string Item, int id, AutomationElement itemObj)
        {
            appName = appName.Replace(" ", "_").Replace(".", "");
            menu = menu.Replace(" ", "_").Replace(".", "");
            menuItem = menuItem.ToString().Replace(" ", "").Replace(".", "").Trim();
            Item = Item.ToString().Replace(" ", "").Replace(".", "").Trim();
            string controlType = "";

            double X = 0, Y = 0;
            if (menuItemObj != null)
            {
                FlaUI.Core.Shapes.Point menuP = menuItemObj.Properties.ClickablePoint;  
                X = menuP.X;
                Y = menuP.Y;
                controlType = menuItemObj.ControlType.ToString();
            }

            double Xi = 0, Yi = 0;
            if (itemObj != null)
            {
                FlaUI.Core.Shapes.Point itemP = itemObj.Properties.ClickablePoint;  
                Xi = itemP.X;
                Yi = itemP.Y;
                controlType = itemObj.ControlType.ToString();
            }

            JObject temp = new JObject();
            Utils utils = new Utils();

            if (!objectDetails.Any())
            {
                objectDetails[appName] = temp;
                objectDetails[appName][menu] = temp;
                objectDetails[appName][menu][menuItem] = new JObject();
                objectDetails[appName][menu][menuItem]["_properties"] = utils.WriteProperties(--id, X, Y, menuItem, controlType);
                LocatedObjects.objectsDB.Add(id, menuItemObj);

                objectDetails[appName][menu][menuItem][Item] = new JObject();
                objectDetails[appName][menu][menuItem][Item]["_properties"] = utils.WriteProperties(++id, Xi, Yi, Item, controlType);
                LocatedObjects.objectsDB.Add(id, itemObj);
            }
            else if (objectDetails[appName][menu][menuItem] == null)
            {
                objectDetails[appName][menu][menuItem] = temp;
                objectDetails[appName][menu][menuItem]["_properties"] = utils.WriteProperties(--id, X, Y, menuItem, controlType);
                LocatedObjects.objectsDB.Add(id, menuItemObj);

                objectDetails[appName][menu][menuItem][Item] = temp;
                objectDetails[appName][menu][menuItem][Item]["_properties"] = utils.WriteProperties(++id, Xi, Yi, Item, controlType);
                LocatedObjects.objectsDB.Add(id, itemObj);
            }
            else if (objectDetails[appName][menu][menuItem][Item] == null)
            {
                objectDetails[appName][menu][menuItem][Item] = temp;
                objectDetails[appName][menu][menuItem][Item]["_properties"] = utils.WriteProperties(id, Xi, Yi, Item, controlType);
                LocatedObjects.objectsDB.Add(id, itemObj);
            }
        }


        public void RunCurrent(string path)
        {
            string[] steps = path.Contains('\n') ? path.Split('\n') : new[] { path };
            JToken obj = null;

            foreach (string step in steps)
            {
                string[] parseStep = step.Trim().Split(".".ToCharArray());
                obj = objectDetails;
                foreach (string level in parseStep)
                {
                    obj = obj[level];
                }

                IEnumerable<JProperty> props = obj.ToObject<JObject>().Properties();
                JProperty properties = props.Where(prop => prop.Name.ToString() == "_properties").First();            
                JObject propObj = (JObject) JsonConvert.DeserializeObject(properties.Value.ToString());
                JObject coordObj = (JObject)JsonConvert.DeserializeObject(propObj["coordinates"].ToString());

                int x = (int)coordObj["x"];
                int y = (int)coordObj["y"];

                FlaUI.Core.Shapes.Point p = new FlaUI.Core.Shapes.Point((double) x, (double) y);
                int id = (int) propObj["id"];
                TestClick(p);             
            }
        }


        

        public static FlaUI.Core.Shapes.Point Position
        {
            get
            {
                POINT point;
                User32.GetCursorPos(out point);
                return point;
            }
            set
            {
                POINT point = value;
                User32.SetCursorPos(point.X, point.Y);
            }
        }
        public void TestClick(FlaUI.Core.Shapes.Point p)
        {
            var currClickPosition = Position;            

            AutomationElementPropertyValues properties = appWindow.Properties;
            double leftPoisition = properties.BoundingRectangle.Value.Left;
            double topPoisition = properties.BoundingRectangle.Value.Top;

            p.X += leftPoisition < 0 ? 0 : leftPoisition;
            p.Y += topPoisition < 0 ? 0 : topPoisition;

            p.X = p.X * 1.25;
            p.Y = p.Y * 1.25;

            Position = p;

            if (LastClickPositions.Count > 0 && LastClickPositions[MouseButton.Left].Equals(currClickPosition))
            {
                // Get the timeout needed to not fire a double click
                var timeout = (int)User32.GetDoubleClickTime() - DateTime.Now.Subtract(LastClickTimes[MouseButton.Left]).Milliseconds;
                // Wait the needed time to prevent the double click
                if (timeout > 0) Thread.Sleep(timeout);
            }

            MouseButton mouseButton = MouseButton.Left;
            Down(mouseButton, p);
            Up(mouseButton, p);

            LastClickTimes[mouseButton] = DateTime.Now;
            LastClickPositions[mouseButton] = Position;       
        }

        public static void Down(MouseButton mouseButton, FlaUI.Core.Shapes.Point p)
        {
            uint data;
            var flags = GetFlagsAndDataForButton(mouseButton, true, out data);
            SendInput((int)p.X, (int)p.Y, data, flags);
        }
        public static void Up(MouseButton mouseButton, FlaUI.Core.Shapes.Point p)
        {
            uint data;
            var flags = GetFlagsAndDataForButton(mouseButton, false, out data);
            SendInput((int)p.X, (int)p.Y, data, flags);
        }
        private static MouseEventFlags GetFlagsAndDataForButton(MouseButton mouseButton, bool isDown, out uint data)
        {
            MouseEventFlags mouseEventFlags;
            var mouseData = MouseEventDataXButtons.NOTHING;
            switch (mouseButton)
            {
                case MouseButton.Left:
                    mouseEventFlags = isDown ? MouseEventFlags.MOUSEEVENTF_LEFTDOWN : MouseEventFlags.MOUSEEVENTF_LEFTUP;
                    break;
                case MouseButton.Right:
                    mouseEventFlags = isDown ? MouseEventFlags.MOUSEEVENTF_RIGHTDOWN : MouseEventFlags.MOUSEEVENTF_RIGHTUP;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("mouseButton");
            }
            data = (uint)mouseData;
            return mouseEventFlags;
        }
        private static void SendInput(int x, int y, uint data, MouseEventFlags flags)
        {
            System.Diagnostics.Debug.WriteLine(" In Mouse SendInput ");
            // correct permissions ?
            //   var permissions = new PermissionSet(PermissionState.Unrestricted);
            //   permissions.Demand();
            //    System.Diagnostics.Debug.WriteLine(" USED COORDS 1 :: " + x + " " + y);
            // Check if we are trying to do an absolute move
            if (flags.HasFlag(MouseEventFlags.MOUSEEVENTF_ABSOLUTE))
            {
                // Absolute position requires normalized coordinates
                NormalizeCoordinates(ref x, ref y);
                flags |= MouseEventFlags.MOUSEEVENTF_VIRTUALDESK;
            }

            // Build the mouse input object
            var mouseInput = new MOUSEINPUT
            {
                dx = x,
                dy = y,
                mouseData = data,
                dwExtraInfo = User32.GetMessageExtraInfo(),
                time = 0,
                dwFlags = flags
            };

            // Build the input object
            var input = INPUT.MouseInput(mouseInput);
            // Send the command

            if (User32.SendInput(1, new[] { input }, INPUT.Size) == 0)
            {
                System.Diagnostics.Debug.WriteLine("error");
                var errorCode = Marshal.GetLastWin32Error();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("click " + mouseInput.dx);
            } 
        }
        private static void NormalizeCoordinates(ref int x, ref int y)
        {
            var vScreenWidth = User32.GetSystemMetrics(SystemMetric.SM_CXVIRTUALSCREEN);
            var vScreenHeight = User32.GetSystemMetrics(SystemMetric.SM_CYVIRTUALSCREEN);
            var vScreenLeft = User32.GetSystemMetrics(SystemMetric.SM_XVIRTUALSCREEN);
            var vScreenTop = User32.GetSystemMetrics(SystemMetric.SM_YVIRTUALSCREEN);

            x = (x - vScreenLeft) * 65536 / vScreenWidth + 65536 / (vScreenWidth * 2);
            y = (y - vScreenTop) * 65536 / vScreenHeight + 65536 / (vScreenHeight * 2);
        }




    }
}
