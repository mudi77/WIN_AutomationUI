using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using FlaUI.Core.AutomationElements.Infrastructure;


namespace Win_AutomationUI
{
    //THIS CLASS SHOULD BE REMOVED AS MOST OF ITs CONTENT HAS BEEN MOVED INTO AutomationCore or replaced by 
    //other objects from AutomationCore
   
    public class LocatedObjects 
    {        
        public static Dictionary<int, AutomationElement> objectsDB = new Dictionary<int, AutomationElement>();
        public static JContainer objectDetails = new JObject();
        public static Dictionary<int, FlaUI.Core.Shapes.Point> objectPoints = new Dictionary<int, FlaUI.Core.Shapes.Point>(); 
    }

    public class UIElementsMap
    {
        private static UIElementsMap instance;
        private JContainer structure;
        private UIElementsMap() { }

        public static UIElementsMap Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new UIElementsMap();
                }
                return instance;
            }
        }
    }

}
