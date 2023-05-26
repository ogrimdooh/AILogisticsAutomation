using VRage.Game;

namespace AILogisticsAutomation
{
    public class AIAssemblerControllerPrioritySettings : BasePrioritySettings<MyDefinitionId>
    {

        protected override bool Compare(MyDefinitionId item, MyDefinitionId item2)
        {
            return item == item2;
        }

        protected override bool IsNull(MyDefinitionId item)
        {
            return item == default(MyDefinitionId);
        }

    }

}