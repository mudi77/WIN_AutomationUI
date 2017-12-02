using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Windows.Forms;
using System.IO;
using FlaUI.Core.AutomationElements.Infrastructure;

namespace Win_AutomationUI
{
    class Utils
    {
        public Dispatcher BuildDispatcher(string type)
        {
            Dispatcher dispatcher = null;
            var manualResetEvent = new ManualResetEvent(false);
            var thread = new Thread(() =>
            {
                switch (type)
                {
                    case "UI": 
                        dispatcher = Dispatcher.FromThread(ControlPanel.UIThread);
                        break;
                    case "WORKER":
                        dispatcher = Dispatcher.CurrentDispatcher;
                        break;
                    default: 
                        break;
                }
                var synchronizationContext = new DispatcherSynchronizationContext(dispatcher);
                SynchronizationContext.SetSynchronizationContext(synchronizationContext);

                manualResetEvent.Set();
                Dispatcher.Run();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            manualResetEvent.WaitOne();
            manualResetEvent.Dispose();
            return dispatcher;
        }

        public void ReBuildTree()
        {
                    System.Windows.Controls.TreeView tree = ControlPanel.jsonTree;

                    JToken token = (JToken)Newtonsoft.Json.JsonConvert.DeserializeObject(Win_AutomationUI.AutomationCore.FileContent);
                    List<JObject> initial1 = new List<JObject>();
                    List<TreeViewItem> initial2 = new List<TreeViewItem>();

                    IterateThis(token, initial1, (TreeViewItem)tree.Items.GetItemAt(0), initial2);                       
        }

        private void IterateThis(JToken jsonCurrent, List<JObject> jsonParents, TreeViewItem viewItemCurrent, List<TreeViewItem> itemParents)
        {
            if (jsonCurrent.Type == JTokenType.Object)
            {
                JObject tempObj = jsonCurrent.ToObject<JObject>();
                foreach (JProperty prop in tempObj.Properties())
                {
                    TreeViewItem item = new TreeViewItem();
                    if (tempObj[prop.Name].Type == JTokenType.Object)
                    {
                        if (prop.Next != null)
                        {
                            jsonParents.Add((JObject)tempObj[prop.Next.ToObject<JProperty>().Name]);
                        }
                        item.Header = prop.Name;
                        viewItemCurrent.Items.Add(item);                       
                        
                        itemParents.Add(viewItemCurrent);
                        int pos = itemParents.Last().Items.Count - 1;
                        IterateThis((JToken)tempObj[prop.Name], jsonParents, (TreeViewItem)viewItemCurrent.Items.GetItemAt(pos), itemParents);
                    }
                    else
                    {
                        TreeViewItem itemKey = new TreeViewItem();
                        TreeViewItem itemValue = new TreeViewItem();
                        itemKey.Header = prop.Name;
                        itemValue.Header = prop.Value;
                        itemKey.Items.Add(itemValue);
                        viewItemCurrent.Items.Add(itemKey);
                        int pos = itemParents.Last().Items.Count - 1;
                        if (tempObj.Properties().Last().Equals(prop) && jsonParents.Count > 0)
                        {
                            JObject o = jsonParents.Last();
                            jsonParents.Remove(o);
                            IterateThis(o.Parent.First.Parent, jsonParents, (TreeViewItem)itemParents.Last().Items.GetItemAt(pos), itemParents);
                        }
                    }
                }
            }
        }

        private void RebuildTestBase(JToken jsonCurrent, List<JObject> jsonParents, TreeViewItem viewItemCurrent, List<TreeViewItem> itemParents) {
           
            Dispatcher disp = BuildDispatcher("UI");
            disp.Invoke(DispatcherPriority.Background, new System.Action(() => {

            string itemType = viewItemCurrent.Parent.GetType().ToString().Split('.').Last();
            bool hasNoChilds = false;
            System.Diagnostics.Debug.WriteLine("\n * HEADER : " + viewItemCurrent.Header);
            System.Diagnostics.Debug.WriteLine(" * JSON  : " + jsonCurrent.Path);

            TreeViewItem tempItem = (TreeViewItem)(itemType.Equals("TreeView") ? viewItemCurrent.Items.GetItemAt(0) : viewItemCurrent);
            JToken tempJson = jsonCurrent;

            if (((TreeViewItem)viewItemCurrent.Items.GetItemAt(0)).HasItems) {

                viewItemCurrent = (TreeViewItem)viewItemCurrent.Items.GetItemAt(0);
                System.Diagnostics.Debug.WriteLine("\n HAS CHILD    viewItemCurrent : " + viewItemCurrent);

                if (!tempItem.Header.Equals("APP :")) {
                    jsonCurrent = jsonCurrent.GetType() == typeof(JValue) ? jsonCurrent.First() : jsonCurrent.First().First;
                }
                System.Diagnostics.Debug.WriteLine(" HAS CHILD    jsonCurrent : " + jsonCurrent.Path);
            } else if(((TreeViewItem)tempItem.Parent).Items.Count == 1){
                System.Diagnostics.Debug.WriteLine(" NO CHILD");
                hasNoChilds = true;
                viewItemCurrent = itemParents.Last();
                jsonCurrent = jsonParents.Last();
                if (viewItemCurrent == null || jsonCurrent == null) {
                    System.Diagnostics.Debug.WriteLine(" WAS NULL");
                    return;
                } else {
                        System.Diagnostics.Debug.WriteLine("COUNT items : " + itemParents.Count + " jsons : " + jsonParents.Count);
                    itemParents.Remove(viewItemCurrent);
                    jsonParents.Remove((JObject)jsonCurrent);
                }
            }

            if (((TreeViewItem)tempItem.Parent).Items.Count > 1) {
                int index = ((TreeViewItem)tempItem.Parent).Items.IndexOf(tempItem);
                    if (hasNoChilds)
                    {
                        viewItemCurrent = (TreeViewItem)((TreeViewItem)tempItem.Parent).Items.GetItemAt(index + 1);
                        jsonCurrent = (JObject)tempJson.Parent.Next.First;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("COUNT items : " + itemParents.Count + " jsons : " + jsonParents.Count);
                        itemParents.Add((TreeViewItem)((TreeViewItem)tempItem.Parent).Items.GetItemAt(index + 1));
                        System.Diagnostics.Debug.WriteLine("\n HAS SIBLING " + itemParents.Last().Header);

                        JObject obj = (JObject)tempJson.Parent.Next.First;
                        jsonParents.Add((JObject)obj);
                        System.Diagnostics.Debug.WriteLine(" HAS SIBLING " + jsonParents.Last().Path);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("XX");
                }                            
            RebuildTestBase(jsonCurrent, jsonParents, viewItemCurrent, itemParents);                   
            }));
        }

        public void SaveObjectsRef()
        {                                 
            var settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple           
            };
            JsonSerializer serializer = JsonSerializer.Create(settings);
            JToken token = (JToken)Newtonsoft.Json.JsonConvert.DeserializeObject(Win_AutomationUI.AutomationCore.FileContent);
            
            RebuildTestBase(token, new List<JObject>(), (TreeViewItem)ControlPanel.jsonTree.Items.GetItemAt(0), new List<TreeViewItem>());
            try
            {
                using (StreamWriter sw = new StreamWriter(@"D:\TestStructure.txt"))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, AutomationCore.objectDetails);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(" e : " + e.ToString());
            }
            
        }

        public void LoadObjectsRef()
        {            
            IDictionary<int, AutomationElement> output = null;    
            OpenFileDialog ofDialog = new OpenFileDialog();
            ofDialog.ShowDialog();

            string fileName = ofDialog.FileName;
            var stream = File.OpenRead(fileName);
                try
                {
                    StreamReader reader = new StreamReader(stream);
                    string text = reader.ReadToEnd();

                //    output = JsonConvert.DeserializeObject<IDictionary<int, AutomationElement>>(text, new DictionaryConverter());
                // LocatedObjects.objectDetails = (JContainer)JsonConvert.DeserializeObject(text);
                AutomationCore.objectDetails = (JContainer)JsonConvert.DeserializeObject(text);

            } catch (Exception e) {
                    System.Diagnostics.Debug.WriteLine(" error ?? :: " + e);
                }         

            AutomationElement test = (AutomationElement) output.ElementAt(0).Value;
            System.Diagnostics.Debug.WriteLine(" first 2 ?? :: " + test);                     
        }


        public string WriteProperties(int id, double x, double y, string name, string type)
        {
            JObject o = new JObject();
            o.Add("id", id);
            o.Add("coordinates", "{ x: " + x + ", y: " + y + "}");
            o.Add("name", name);
            o.Add("type", type);
            return o.ToString();
        }

        public string GetSelectedTreeItem(string ItemOrPath) {
            TreeViewItem selectedItem = new TreeViewItem();
            Utils util = new Utils();
            string path = "";
            Dispatcher disp = util.BuildDispatcher("UI");
            disp.Invoke(DispatcherPriority.Background, new System.Action(() => {
                 selectedItem = (TreeViewItem)ControlPanel.jsonTree.SelectedItem;            
                if (ItemOrPath.Equals("item")){
                    path = selectedItem.Header.ToString();
                }else{
                    path = selectedItem.Header.ToString();                
                    while (selectedItem.Parent!=null && !((TreeViewItem)selectedItem.Parent).Header.ToString().Equals("APP :")) {
                        path = string.Concat(((TreeViewItem)selectedItem.Parent).Header.ToString(), (string.Concat(".", path)));
                        selectedItem = (TreeViewItem) selectedItem.Parent;
                        System.Diagnostics.Debug.WriteLine(" header: " + selectedItem.Header);
                    }                
                }
            }));
            return path;
        }

    }









}
