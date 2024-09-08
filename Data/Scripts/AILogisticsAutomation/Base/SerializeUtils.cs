using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;

namespace AILogisticsAutomation
{
    public static class SerializeUtils
    {

        static public bool TrySerializeFromXML<T>(string data, out T o)
        {
            o = default(T);
            try
            {
                if (string.IsNullOrWhiteSpace(data))
                    return false;
                if (!data.Trim().StartsWith("<?xml"))
                {
                    // Maybe is a Json Value, so try to convert to Xml
                    data = JsonUtils.JsonToXml(data);
                }
                o = MyAPIGateway.Utilities.SerializeFromXML<T>(data);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

    }

}