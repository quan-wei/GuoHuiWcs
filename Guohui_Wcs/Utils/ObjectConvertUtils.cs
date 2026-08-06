using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Guohui_Wcs.Utils
{
    public class ObjectConvertUtils
    {

        public Dictionary<string, string> ObjectToMap<T>(T zone)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            PropertyInfo[] list = zone.GetType().GetProperties();
            foreach (PropertyInfo p in list)
            {
                //Console.WriteLine("键：" + p.Name + ",值：" + p.GetValue(zone, null));
                map.Add(p.Name, (p.GetValue(zone, null) ?? "").ToString());
            }
            return map;
        }
    }
}
