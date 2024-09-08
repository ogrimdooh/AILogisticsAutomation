using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;

namespace AILogisticsAutomation
{
    public static class JsonUtils
    {

        public static string JsonToXml(string data)
        {
            var lines = data.Replace(Environment.NewLine, "\n").Split('\n');
            var nDict = new List<KeyValuePair<string, object>>();
            int nEnd = 0;
            JsonToDic(0, lines, nDict, out nEnd);
            StringBuilder sb = new StringBuilder();
            DicToXml(sb, nDict, 0);
            return sb.ToString();
        }

        public static string XmlToJson(string data)
        {
            var lines = data.Replace(Environment.NewLine, "\n").Split('\n');
            var nDict = new List<KeyValuePair<string, object>>();
            int nEnd = 0;
            JsonUtils.XmlToDic(0, lines, nDict, out nEnd);
            StringBuilder sb = new StringBuilder();
            JsonUtils.DicToJson(sb, nDict, 0);
            return sb.ToString();
        }

        public static void DicToXml(StringBuilder sb, List<KeyValuePair<string, object>> xDict, int level)
        {
            if (level == 0)
            {
                sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-16\"?>");
            }
            int i = 0;
            foreach (var item in xDict)
            {
                var sulfix = i == 0 && level == 0 ? " xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"" : "";
                var xDict2 = item.Value as List<KeyValuePair<string, object>>;
                if (xDict2 != null)
                {
                    var line = $"<{item.Key}{sulfix}>";
                    sb.AppendLine(line.PadLeft(line.Length + (level * 2), ' '));
                    DicToXml(sb, xDict2, level + 1);
                    line = $"</{item.Key}>";
                    sb.AppendLine(line.PadLeft(line.Length + (level * 2), ' '));
                }
                else
                {
                    if (item.Value == null)
                    {
                        var line = $"<{item.Key} />";
                        sb.AppendLine(line.PadLeft(line.Length + (level * 2), ' '));
                    }
                    else
                    {
                        var finalValue = $"<{item.Key}>{item.Value}</{item.Key}>";
                        sb.AppendLine(finalValue.PadLeft(finalValue.Length + (level * 2), ' '));
                    }
                }
            }
        }

        public static void DicToJson(StringBuilder sb, List<KeyValuePair<string, object>> xDict, int level)
        {
            if (level == 0)
            {
                sb.AppendLine("{");
                level++;
            }
            int i = 0;
            foreach (var item in xDict)
            {
                var sulfix = i == xDict.Count - 1 ? "" : ",";
                var xDict2 = item.Value as List<KeyValuePair<string, object>>;
                if (xDict2 != null)
                {
                    var line = $"{item.Key} : {{";
                    sb.AppendLine(line.PadLeft(line.Length + (level * 2), ' '));
                    DicToJson(sb, xDict2, level + 1);
                    line = $"}}{sulfix}";
                    sb.AppendLine(line.PadLeft(line.Length + (level * 2), ' '));
                }
                else
                {
                    string finalValue = "";
                    if (item.Value == null)
                    {
                        finalValue = "null";
                    }
                    else
                    {
                        finalValue = item.Value.ToString();
                        long v = 0;
                        ulong v4 = 0;
                        float v2 = 0;
                        bool v3 = false;
                        if (!long.TryParse(finalValue, out v) &&
                            !ulong.TryParse(finalValue, out v4) &&
                            !float.TryParse(finalValue, out v2) &&
                            !bool.TryParse(finalValue, out v3))
                        {
                            finalValue = "\"" + finalValue + "\"";
                        }
                        finalValue = $"{item.Key} : {finalValue}{sulfix}";
                        sb.AppendLine(finalValue.PadLeft(finalValue.Length + (level * 2), ' '));
                    }
                }
                i++;
            }
            if (level == 1)
                sb.AppendLine("}");
        }

        public static void JsonToDic(int start, string[] lines, List<KeyValuePair<string, object>> xDict, out int end)
        {
            end = 0;
            for (int i = start; i < lines.Length; i++)
            {
                var content = lines[i].Trim();
                if (!content.StartsWith("{"))
                {
                    if (content.StartsWith("}"))
                    {
                        end = i;
                        break;
                    }
                    else
                    {
                        var endName = content.IndexOf(':');
                        var currentName = content.Substring(0, endName - 1).Trim();
                        content = content.Substring(endName + 1).Trim();
                        if (content == "{")
                        {
                            var nDict = new List<KeyValuePair<string, object>>();
                            int nEnd = 0;
                            JsonToDic(i + 1, lines, nDict, out nEnd);
                            xDict.Add(new KeyValuePair<string, object>(currentName, nDict));
                            i = nEnd;
                        }
                        else
                        {
                            if (content.EndsWith(","))
                                content = content.Substring(0, content.Length - 1);
                            if (content.StartsWith("\"") && content.EndsWith("\""))
                                content = content.Substring(1, content.Length - 2);
                            if (content == "null")
                                content = "";
                            xDict.Add(new KeyValuePair<string, object>(currentName, content));
                        }
                    }
                }
            }
        }

        public static void XmlToDic(int start, string[] lines, List<KeyValuePair<string, object>> xDict, out int end)
        {
            end = 0;
            for (int i = start; i < lines.Length; i++)
            {
                var content = lines[i].Trim();
                if (!content.StartsWith("<?xml"))
                {
                    if (content.StartsWith("</"))
                    {
                        end = i;
                        break;
                    }
                    else
                    {
                        var endName = content.IndexOf('>');
                        var currentName = content.Substring(1, endName - 1).Split(' ').FirstOrDefault();
                        if (endName == content.Length - 1)
                        {
                            if (content.EndsWith("/>"))
                            {
                                xDict.Add(new KeyValuePair<string, object>(currentName, null));
                            }
                            else
                            {
                                var nDict = new List<KeyValuePair<string, object>>();
                                int nEnd = 0;
                                XmlToDic(i + 1, lines, nDict, out nEnd);
                                xDict.Add(new KeyValuePair<string, object>(currentName, nDict));
                                i = nEnd;
                            }
                        }
                        else
                        {
                            content = content.Substring(endName + 1);
                            var endContent = content.IndexOf('<');
                            content = content.Substring(0, endContent);
                            xDict.Add(new KeyValuePair<string, object>(currentName, content));
                        }
                    }
                }
            }
        }

    }

}
