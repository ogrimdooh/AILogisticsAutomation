using Sandbox.ModAPI;
using VRage.ModAPI;
using VRage.Game;
using System;
using Sandbox.Game.EntityComponents;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI;

namespace AILogisticsAutomation
{

    public interface IAIBlockSettings<T> : IBlockSettings<T>
    {

        bool GetEnabled();
        void SetEnabled(bool value);

    }

    public abstract class BaseAIBlock<T, S, D> : BaseWithSettingLogicComponent<T, S, D> where T : IMyTerminalBlock where S : IAIBlockSettings<D>
    {

        public bool IsValidToWork
        {
            get
            {
                return CurrentEntity.IsFunctional && IsPowered && IsEnabled && CountAIInventoryManager(Grid) == 1 && GetIsValidToWork();
            }
        }

        public bool IsWorking
        {
            get
            {
                return IsValidToWork && HadWorkToDo;
            }
        }

        public bool HadWorkToDo
        {
            get
            {
                return GetHadWorkToDo();
            }
        }

        public bool IsEnabled
        {
            get
            {
                return Settings?.GetEnabled() ?? false;
            }
        }

        public const float StandbyPowerConsumption = 0.001f;
        public const float OperationalPowerConsumption = 0.5f;

        protected abstract bool GetHadWorkToDo();
        protected abstract bool GetIsValidToWork();
        protected abstract void DoExecuteCycle();

        protected int CountAIInventoryManager(IMyCubeGrid grid)
        {
            return grid?.CountBlocks(new MyDefinitionId(typeof(MyObjectBuilder_OreDetector), "AIInventoryManager")) ?? 0;
        }

        protected override void CurrentEntity_OnClose(IMyEntity obj)
        {
            base.CurrentEntity_OnClose(obj);
            canRun = false;
        }

        protected float ComputeRequiredPower()
        {
            if (!CurrentEntity.IsFunctional || !Settings.GetEnabled())
                return 0.0f;
            return !HadWorkToDo ? StandbyPowerConsumption : Settings.GetPowerConsumption();
        }

        protected long GetGameTime()
        {
            return ExtendedSurvivalCoreAPI.Registered ? ExtendedSurvivalCoreAPI.GetGameTime() : AILogisticsAutomationTimeManager.Instance.GameTime;
        }

        protected long deltaTime = 0;
        protected long spendTime = 0;
        protected void DoRefreshDeltaTime()
        {
            deltaTime = GetGameTime();
        }

        protected readonly long cicleType = 3000; /* default cycle time */
        protected override void OnUpdateAfterSimulation100()
        {
            base.OnUpdateAfterSimulation100();
            if (IsServer)
            {
                try
                {
                    if (deltaTime == 0)
                        DoRefreshDeltaTime();
                    if (deltaTime != 0)
                    {
                        var updateTime = GetGameTime() - deltaTime;
                        DoRefreshDeltaTime();

                        if (!cycleIsRuning)
                        {
                            spendTime += updateTime;
                            if (spendTime >= cicleType)
                            {
                                spendTime = 0;
                                DoCallExecuteCycle();
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
                }
            }
            CurrentEntity.ResourceSink.SetRequiredInputByType(MyResourceDistributorComponent.ElectricityId, ComputeRequiredPower());
            CurrentEntity.RefreshCustomInfo();
            UpdateEmissiveState();
        }

        protected override void OnAppendingCustomInfo(StringBuilder sb)
        {
            base.OnAppendingCustomInfo(sb);
            sb.Append("Is Enabled: ").Append(Settings.GetEnabled() ? "Yes" : "No").Append('\n');
            if (Settings.GetEnabled())
            {
                sb.Append("Is Valid To Work: ").Append(IsValidToWork ? "Yes" : "No").Append('\n');
                sb.Append("Had Work To Do: ").Append(HadWorkToDo ? "Yes" : "No").Append('\n');
                sb.Append("Is Working: ").Append(IsWorking ? "Yes" : "No").Append('\n');
            }
            sb.Append('-', 30).Append('\n');
            sb.Append("Is Powered: ").Append(IsPowered ? "Yes" : "No").Append('\n');
            sb.Append("Required Power: ").Append(string.Format("{0}{1}", RequiredPower >= 1 ? RequiredPower : RequiredPower * 1000, RequiredPower >= 1 ? "MW" : "KW")).Append('\n');
            sb.Append("Current Power: ").Append(string.Format("{0}{1}", CurrentPower >= 1 ? CurrentPower : CurrentPower * 1000, CurrentPower >= 1 ? "MW" : "KW")).Append('\n');
        }

        protected bool cycleIsRuning = false;
        protected bool canRun = true;
        protected ParallelTasks.Task task;
        protected void DoCallExecuteCycle()
        {
            if (!cycleIsRuning)
            {
                cycleIsRuning = true;
                task = MyAPIGateway.Parallel.StartBackground(() =>
                {
                    try
                    {
                        try
                        {

                            DoExecuteCycle();
                        }
                        catch (Exception ex)
                        {
                            AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
                        }
                    }
                    finally
                    {
                        cycleIsRuning = false;
                    }
                });
            }
        }

        protected bool UpdateEmissiveState()
        {
            if (!IsEnabled)
                return SetEmissiveState(EmissiveState.Disabled);
            if (!IsWorking)
                return SetEmissiveState(EmissiveState.Warning);
            if (cycleIsRuning)
                return SetEmissiveState(EmissiveState.Working);
            return SetEmissiveState(EmissiveState.Alternative);
        }

    }

}