using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;

namespace AILogisticsAutomation
{
    public static class MyOreDetectorExtension
    {

        public static bool DoResetRange(this IMyOreDetector entity)
        {
            try
            {
                var range = (ITerminalProperty<float>)entity.GetProperty("Range");
                if (range != null)
                {
                    range.SetValue(entity, range.GetMinimum(entity));
                }
                if (AILogisticsAutomationSession.IsUsingOreDetectorReforge())
                {
                    var reforgedRange = (ITerminalProperty<float>)entity.GetProperty("Reforged: Range");
                    if (reforgedRange != null)
                    {
                        reforgedRange.SetValue(entity, reforgedRange.GetMinimum(entity));
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(typeof(MyOreDetectorExtension), ex);
            }
            return false;
        }

    }

}