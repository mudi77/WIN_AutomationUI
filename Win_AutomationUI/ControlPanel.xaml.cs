using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using System.Windows.Controls;

namespace Win_AutomationUI
{
    //CLASS NEAREST TO CONTROL PANEL UI BUTTONS/INPUTS .etc : NOT COMPLETED + SEEMS REDISGN IS NEEDED - MAIN UI IS LET RUNNING 
    //IN SEPARATE THREAD BUT - WAY HOW ANOTHER PROCESS ARE EXCUTED LOOKS UGLY (to be considered..)

    public partial class ControlPanel : Window
    {
        string appName, appPath = "";
        private static bool recFlag = false;
        private AutomationCore AutomationCore = new AutomationCore();
        public static TextBlock recText;
        public static TreeView jsonTree;
        public static TextBox itemName;
        public static TextBox rightSide;
        public static Thread UIThread;
        public string AppVer { get; set; }

        public ControlPanel()
        {
            double version = 0.1;
            AppVer = version.ToString("R");   
            InitializeComponent();
            rightSide = messageBoxText;
            recText = recordingBox;
            jsonTree = treeView_Box;
            itemName = txtbox_Add;
            UIThread = this.Dispatcher.Thread;
        }

        /* method as generic bridge for all UI controls */
        private void ControlEntryPoint(object sender, RoutedEventArgs e)
        {
            string actionName = sender.ToString().Split(':')[1];
            actionName = actionName.Replace(" ", "");
            System.Diagnostics.Debug.Write(" sender info :: " + actionName + "\n");

            if (actionName.Equals("Selectapp")) Runner(SelectApp);
            if (actionName.Equals("Launchapp")) Runner(RunApp); 
            if (actionName.Equals("Mapelements")) Runner(RunObjectsIdentification);
            if (actionName.Equals("Run")) Runner(RunTestCases);
            if (actionName.Equals("Record") || actionName.Equals("Stop")) Runner(Recording);
            if (actionName.Equals("Load")) Runner(Load);
            if (actionName.Equals("Save")) Runner(Save);
            if (actionName.Equals("Add")) Runner(Add);
            if (actionName.Equals("Loadtr")) Runner(Loadtr);
            if (actionName.Equals("SaveElementProperties")) Runner(SaveProperties);
        }


        /* function for running jobs in thread */
        public void Runner(System.Action MethodName)
        {
            if (MethodName != null)
            {
                Utils utils = new Utils();
                Dispatcher dispatcher = utils.BuildDispatcher("WORKER");
                dispatcher.BeginInvoke(DispatcherPriority.Background, new System.Action(() =>
                {
                    MethodName();
                }));
            }
        }


        /* couple methods mapped to buttons control panel */
        private void SelectApp()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                appName = Regex.Replace(openFileDialog.SafeFileName, @"(.exe|.bat|.cmd)", "");
                appPath = openFileDialog.FileName;
                UpdateUI("SelectApp", appName, null);
            };
        }
        private void RunApp()
        {
            Win_AutomationUI.AutomationCore at = new Win_AutomationUI.AutomationCore();
            System.Diagnostics.Debug.Write(" debug " + appName + " " + appPath + " \n");
            at.StartThis(appName, appPath);
            UpdateUI("RunApp", appName, null);
        }
        private void RunObjectsIdentification()
        {
            Win_AutomationUI.AutomationCore autoCore = new Win_AutomationUI.AutomationCore();
            autoCore.IdentifyMenuElements();
            UpdateUI("RunObjectsIdentification", appName, null);           
        }
        private void RunTestCases()
        {
            string msg = "";
            AutomationCore auto = new AutomationCore();            
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(() =>
            {
                msg = this.messageBoxText.Text;                
            }));
            auto.RunCurrent(msg);
        }
        private void Recording()
        {
            System.Diagnostics.Debug.Write("recording ignited .. \n");
            Recording Recording = new Recording();
            recFlag = recFlag ? false : true;
            Recording.Rec(recFlag);
            UpdateUI("Record", appName, null);
        }
        private void Load() {
            if (AutomationCore != null) AutomationCore.Load();
            UpdateUI("Load", appName, null);
        }
        private void Save()
        {
            Utils utils = new Utils();
            utils.SaveObjectsRef();
        }
        private void Add()
        {
            Recording Recording = new Recording();
            Recording.EditTreeUI("AddItem");   
        }
        private void Loadtr()
        {
            System.Diagnostics.Debug.WriteLine(" loadtr");
            Utils utils = new Utils();
            utils.LoadObjectsRef();
        }

        private void SaveProperties()
        {
            Recording rec = new Win_AutomationUI.Recording();
            rec.SaveElementProperties();
        }







        /* function for updating UI  */
        public void UpdateUI(string method, string data, JContainer obj)
        {
            Utils utils = new Utils();
            Dispatcher dispatcher = utils.BuildDispatcher("UI");
            dispatcher.BeginInvoke(DispatcherPriority.Render,
            new System.Action(() =>
            {
                switch (method)
                {
                    case "SelectApp":
                        {
                            this.txtEditor.Text = data;
                            runApp.IsEnabled = true;
                            runApp.Background = Brushes.LightGreen;
                            Border2.Background = Brushes.LightGreen;
                        }
                        break;
                    case "RunApp":
                        {
                            mapObjects.IsEnabled = true;
                            Border3.Background = Brushes.LightGreen;
                            mapObjects.Background = Brushes.LightGreen;
                            Record.IsEnabled = true;
                        }
                        break;
                    case "RunObjectsIdentification":
                        {
                            runSteps.IsEnabled = true;
                            messageBoxText.IsEnabled = true;
                            runSteps.Background = Brushes.LightGreen;
                            Border4.Background = Brushes.LightGreen;
                            utils.ReBuildTree();
                        }
                        break;
                    case "RunTestCases":
                        {

                        }
                        break;
                    case "Record":
                        {
                            Record.Background = recFlag ? Brushes.OrangeRed : Brushes.Red;
                            Record.Content = recFlag ? "Stop" : "Record";
                            messageBoxText.IsEnabled = recFlag ? false : true;
                        }
                        break;
                    case "structureUpdate":
                        {

                        }
                        break;
                    case "Load":
                        {
                            utils.ReBuildTree();                            
                        }
                        break;
                    case "Loadtr":
                        {
                            
                        }
                        break;
                }
            }));


        }











    }





}
