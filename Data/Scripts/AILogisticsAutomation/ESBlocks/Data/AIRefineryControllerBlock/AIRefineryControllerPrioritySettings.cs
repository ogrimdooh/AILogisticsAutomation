namespace AILogisticsAutomation
{

    public class AIRefineryControllerPrioritySettings : BasePrioritySettings<string>
    {

        protected override bool Compare(string item, string item2)
        {
            return item == item2;
        }

        protected override bool IsNull(string item)
        {
            return string.IsNullOrWhiteSpace(item);
        }

    }

}