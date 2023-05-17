using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game;
using System.Collections.Generic;
using System;
using VRage.Utils;
using Sandbox.Game.Entities;
using System.Linq;
using Sandbox.Game.EntityComponents;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI;
using VRage;
using Sandbox.Game;
using Sandbox.Definitions;
using System.Collections.Concurrent;
using VRageMath;
using Sandbox.Game.Gui;

namespace AILogisticsAutomation
{

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OreDetector), false, "AIInventoryManager")]
    public class AIInventoryManagerBlock : BaseLogicComponent<IMyOreDetector>
    {

        private const float IDEAL_COMPOSTER_ORGANIC = 100;
        private const float IDEAL_FISHTRAP_BAIT = 5;
        private const float IDEAL_FISHTRAP_NOBLEBAIT = 2.5f;

        public AIInventoryManagerSettings Settings { get; set; } = new AIInventoryManagerSettings();

        public bool IsValidToWork
        {
            get
            {
                return CurrentEntity.IsFunctional && IsPowered && IsEnabled && CountAIInventoryManager(Grid) == 1;
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
                return Settings?.GetDefinitions().Any() ?? false;
            }
        }

        public bool IsEnabled
        {
            get
            {
                return Settings?.GetEnabled() ?? false;
            }
        }

        public const float StandbyPowerConsumption = 0.05f;
        public const float OperationalPowerConsumption = 0.5f;

        protected override void OnInit(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (IsServer)
            {
                LoadSettings();
                CurrentEntity.OnClose += CurrentEntity_OnClose;
            }
            else
            {
                RequestSettings();
            }
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        private void CurrentEntity_OnClose(IMyEntity obj)
        {
            AILogisticsAutomationStorage.Instance.RemoveEntity(CurrentEntity.EntityId);
            canRun = false;
        }

        protected void ReciveFromServer(string encodeData)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(encodeData))
                {
                    var decodeData = Base64Utils.DecodeFrom64(encodeData);
                    var data = MyAPIGateway.Utilities.SerializeFromXML<AIInventoryManagerSettingsData>(decodeData);
                    Settings.UpdateData(data);
                }
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        protected string GetEncodedData()
        {
            try
            {
                var data = Settings.GetData();
                var dataToSend = MyAPIGateway.Utilities.SerializeToXML<AIInventoryManagerSettingsData>(data);
                return Base64Utils.EncodeToBase64(dataToSend);
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
                return null;
            }
        }

        protected void SendToClient(ulong clientId)
        {
            try
            {
                if (AILogisticsAutomationSettings.Instance.Debug)
                    AILogisticsAutomationLogging.Instance.LogInfo(GetType(), $"SendToClient: clientId={clientId}");
                var encodeData = GetEncodedData();
                SendCallServer(new ulong[] { clientId }, "UpdateSettings", new Dictionary<string, string>() { { "DATA", encodeData } });
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        protected void SendPowerToClient()
        {
            try
            {
                if (AILogisticsAutomationSettings.Instance.Debug)
                    AILogisticsAutomationLogging.Instance.LogInfo(GetType(), $"SendPowerToClient: caller={Settings.GetPowerConsumption()}");
                SendCallServer(new ulong[] { }, "UpdatePower", new Dictionary<string, string>() { { "POWER", Settings.GetPowerConsumption().ToString() } });
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        protected void ReciveFromClient(ulong caller, string key, string action, string value, string owner)
        {
            try
            {
                if (AILogisticsAutomationSettings.Instance.Debug)
                    AILogisticsAutomationLogging.Instance.LogInfo(GetType(), $"ReciveFromClient: caller={caller} - key={key} - action={action} - value={value} - owner={owner}");
                if (Settings.UpdateData(key, action, value, owner))
                {
                    SaveSettings();
                    var players = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(players);
                    if (players.Any(x => x.SteamUserId != caller))
                    {
                        var ids = players.Where(x => x.SteamUserId != caller).Select(x => x.SteamUserId).ToArray();
                        if (ids.Any())
                        {
                            var changeData = new Dictionary<string, string>()
                            {
                                { "KEY", key },
                                { "ACTION", action },
                                { "VALUE", value },
                                { "OWNER", owner }
                            };
                            SendCallServer(ids, "SetSettings", changeData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        protected void LoadSettings()
        {
            try
            {
                var storedData = AILogisticsAutomationStorage.Instance.GetEntityValue(CurrentEntity.EntityId, "DATA");
                if (!string.IsNullOrWhiteSpace(storedData))
                {
                    var decodeData = Base64Utils.DecodeFrom64(storedData);
                    var data = MyAPIGateway.Utilities.SerializeFromXML<AIInventoryManagerSettingsData>(decodeData);
                    Settings.UpdateData(data);
                }
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        protected void RequestSettings()
        {
            try
            {
                SendCallClient(MyAPIGateway.Session.Player.SteamUserId, "RequestSettings", new Dictionary<string, string>() { });
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        protected void SaveSettings()
        {
            try
            {
                var encodeData = GetEncodedData();
                AILogisticsAutomationStorage.Instance.SetEntityValue(CurrentEntity.EntityId, "DATA", encodeData);
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        public void SendToServer(string key, string action, string value, string owner = null)
        {
            try
            {
                var changeData = new Dictionary<string, string>() 
                { 
                    { "KEY", key },
                    { "ACTION", action },
                    { "VALUE", value },
                    { "OWNER", owner }
                };
                if (!IsServer)
                {
                    var encodeData = GetEncodedData();
                    SendCallClient(MyAPIGateway.Session.Player.SteamUserId, "SetSettings", changeData);
                }
                else
                {
                    SaveSettings();
                    SendCallServer(new ulong[] { }, "SetSettings", changeData);
                }
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        private float ComputeRequiredPower()
        {            
            if (!CurrentEntity.IsFunctional || !Settings.GetEnabled())
                return 0.0f;
            return !HadWorkToDo ? StandbyPowerConsumption : Settings.GetPowerConsumption();
        }

        private long GetGameTime()
        {
            return ExtendedSurvivalCoreAPI.Registered ? ExtendedSurvivalCoreAPI.GetGameTime() : AILogisticsAutomationTimeManager.Instance.GameTime;
        }

        private long deltaTime = 0;
        private long spendTime = 0;
        public void DoRefreshDeltaTime()
        {
            deltaTime = GetGameTime();
        }

        private readonly long cicleType = 3000; /* default cycle time */
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

        private bool cycleIsRuning = false;
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

        private MyCubeGrid CubeGrid
        {
            get
            {
                return Grid as MyCubeGrid;
            }
        }

        private List<MyCubeGrid> GetSubGrids()
        {
            return CubeGrid.GetConnectedGrids(GridLinkTypeEnum.Mechanical).Where(x => x.EntityId != Grid.EntityId && CountAIInventoryManager(x) == 0).ToList();
        }

        public struct ShipConnected
        {

            public IMyShipConnector Connector;
            public MyCubeGrid Grid;

            public ShipConnected(IMyShipConnector connector)
            {
                Connector = connector;
                Grid = connector.CubeGrid as MyCubeGrid;
            }

        }

        private int CountAIInventoryManager(IMyCubeGrid grid)
        {
            if (grid != null)
            {
                if (ExtendedSurvivalCoreAPI.Registered && IsServer)
                {
                    var lista = ExtendedSurvivalCoreAPI.GetGridBlocks(grid.EntityId, typeof(MyObjectBuilder_OreDetector), "AIInventoryManager");
                    return lista?.Count ?? 0;
                }
                else
                {
                    var targetId = new MyDefinitionId(typeof(MyObjectBuilder_OreDetector), "AIInventoryManager");
                    List<IMySlimBlock> lista = new List<IMySlimBlock>();
                    grid.GetBlocks(lista, x => x.BlockDefinition.Id == targetId);
                    return lista.Count;
                }
            }
            return 0;
        }

        private Dictionary<IMyShipConnector, ShipConnected> GetConnectedGrids()
        {
            var data = new Dictionary<IMyShipConnector, ShipConnected>();
            List<IMySlimBlock> connectors = new List<IMySlimBlock>();
            if (ExtendedSurvivalCoreAPI.Registered)
            {
                connectors = ExtendedSurvivalCoreAPI.GetGridBlocks(Grid.EntityId, typeof(MyObjectBuilder_ShipConnector), null);
            }
            else
            {
                Grid.GetBlocks(connectors, x => x.BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_ShipConnector));
            }
            if (connectors != null && connectors.Any())
            {
                foreach (var connector in connectors)
                {
                    var c = (connector.FatBlock as IMyShipConnector);
                    if (c.IsConnected && c.OtherConnector.CubeGrid.EntityId != Grid.EntityId && CountAIInventoryManager(c.OtherConnector.CubeGrid) == 0)
                    {
                        data.Add(c, new ShipConnected(c.OtherConnector));
                    }
                }
            }
            return data;
        }

        public IEnumerable<MyCubeBlock> ValidInventories
        {
            get
            {
                return DoApplyBasicFilter(CubeGrid.Inventories);
            }
        }

        private IEnumerable<MyCubeBlock> DoApplyBasicFilter(HashSet<MyCubeBlock> inventories)
        {
            return inventories.Where(x =>
                x.IsFunctional &&
                ((x as IMyFunctionalBlock)?.Enabled ?? true) &&
                !Settings.GetIgnoreCargos().Contains(x.EntityId) &&
                !Settings.GetIgnoreFunctionalBlocks().Contains(x.EntityId) &&
                !Settings.GetIgnoreConnectors().Contains(x.EntityId) &&
                !x.BlockDefinition.Id.IsHydrogenEngine() &&
                !x.BlockDefinition.Id.IsParachute() &&
                (Settings.GetPullFromComposter() || !x.BlockDefinition.Id.IsComposter()) &&
                (Settings.GetPullFishTrap() || !x.BlockDefinition.Id.IsFishTrap()) &&
                (Settings.GetPullRefrigerator() || !x.BlockDefinition.Id.IsRefrigerator()) &&
                (Settings.GetPullFromRefinary() || !x.BlockDefinition.Id.IsRefinery()) &&
                (Settings.GetPullFromAssembler() || !x.BlockDefinition.Id.IsAssembler()) &&
                (Settings.GetPullFromReactor() || !x.BlockDefinition.Id.IsReactor()) &&
                (Settings.GetPullFromGasGenerator() || !x.BlockDefinition.Id.IsGasGenerator()) &&
                (Settings.GetPullFromGasTank() || !x.BlockDefinition.Id.IsGasTank())
            );
        }

        private float CalcPowerFromBlocks(float power, IEnumerable<MyCubeBlock> blocks)
        {
            var totalInventories = blocks.Count();

            power += 0.05f * totalInventories;

            if (Settings.GetFillReactor())
            {
                var totalReactors = blocks.Count(x => x.BlockDefinition.Id.IsReactor());
                power += 0.05f * totalReactors;
            }

            if (Settings.GetFillGasGenerator())
            {
                var totalGasGenerator = blocks.Count(x => x.BlockDefinition.Id.IsGasGenerator());
                power += 0.05f * totalGasGenerator;
            }

            if (Settings.GetFillRefrigerator())
            {
                var totalReactors = blocks.Count(x => x.BlockDefinition.Id.IsRefrigerator());
                power += 0.05f * totalReactors;
            }

            if (Settings.GetFillFishTrap())
            {
                var totalReactors = blocks.Count(x => x.BlockDefinition.Id.IsFishTrap());
                power += 0.05f * totalReactors;
            }

            if (Settings.GetFillComposter())
            {
                var totalReactors = blocks.Count(x => x.BlockDefinition.Id.IsComposter());
                power += 0.05f * totalReactors;
            }

            if (Settings.GetFillBottles())
            {
                var totalBottleTargets = blocks.Count(x => x.BlockDefinition.Id.IsBottleTaget());
                power += 0.05f * totalBottleTargets;
            }

            return power;
        }

        private float GetPowerConsumption(out List<MyCubeGrid> subgrids, out Dictionary<IMyShipConnector, ShipConnected> connectedGrids)
        {
            subgrids = null;
            connectedGrids = null;

            if (!IsValidToWork)
                return 0;

            // Get base power
            var power = CalcPowerFromBlocks(0, ValidInventories);

            // Get pull containers power
            power += (0.15f + (Settings.GetSortItensType() > 0 ? 0.075f : 0) ) * Settings.GetDefinitions().Count;
            
            // Get filter power
            var totalFilters = Settings.GetDefinitions().Sum(x => x.IgnoreIds.Count + x.IgnoreTypes.Count + x.ValidTypes.Count + x.ValidIds.Count);
            power += 0.025f * totalFilters;

            // Get subgrids
            if (Settings.GetPullSubGrids())
            {
                subgrids = GetSubGrids();
                foreach (var grid in subgrids)
                {
                    var query = DoApplyBasicFilter(grid.Inventories);
                    power = CalcPowerFromBlocks(power, query);
                }
            }

            // Get connected grids
            if (Settings.GetPullFromConnectedGrids())
            {
                connectedGrids = GetConnectedGrids();
                foreach (var connector in connectedGrids.Keys)
                {
                    if (!Settings.GetIgnoreConnectors().Contains(connector.EntityId))
                    {
                        var query = DoApplyBasicFilter(connectedGrids[connector].Grid.Inventories);
                        power = CalcPowerFromBlocks(power, query);
                    }
                }
            }

            return power;
        }

        private void DoCheckInventoryList(MyCubeBlock[] listaToCheck, ref List<IMyReactor> reactors, ref List<IMyGasGenerator> gasGenerators, 
            ref List<IMyGasTank> gasTanks, ref List<IMyGasGenerator> composters, ref List<IMyGasGenerator> fishTraps, ref List<IMyGasGenerator> refrigerators)
        {
            for (int i = 0; i < listaToCheck.Length; i++)
            {

                if (listaToCheck[i].BlockDefinition.Id.IsGasTank() && !Settings.GetPullFromGasTank())
                    continue;

                if (listaToCheck[i].BlockDefinition.Id.IsGasGenerator() && !Settings.GetPullFromGasGenerator())
                    continue;

                if (listaToCheck[i].BlockDefinition.Id.IsReactor() && !Settings.GetPullFromReactor())
                    continue;

                if (listaToCheck[i].BlockDefinition.Id.IsAssembler() && !Settings.GetPullFromAssembler())
                    continue;

                if (listaToCheck[i].BlockDefinition.Id.IsRefinery() && !Settings.GetPullFromRefinary())
                    continue;

                // Pula inventorios de despejo
                if (Settings.GetDefinitions().Any(x => x.EntityId == listaToCheck[i].EntityId))
                    continue;

                int targetInventory = 0;
                IMyReactor reactor = null;
                if (listaToCheck[i].BlockDefinition.Id.IsReactor())
                {
                    reactor = (listaToCheck[i] as IMyReactor);
                    if (!reactors.Contains(reactor))
                        reactors.Add(reactor);
                }
                IMyGasGenerator gasGenerator = null;
                if (listaToCheck[i].BlockDefinition.Id.IsGasGenerator())
                {
                    gasGenerator = (listaToCheck[i] as IMyGasGenerator);
                    if (!gasGenerators.Contains(gasGenerator))
                        gasGenerators.Add(gasGenerator);
                }
                if (listaToCheck[i].BlockDefinition.Id.IsFishTrap())
                {
                    gasGenerator = (listaToCheck[i] as IMyGasGenerator);
                    if (!fishTraps.Contains(gasGenerator))
                        fishTraps.Add(gasGenerator);
                }
                if (listaToCheck[i].BlockDefinition.Id.IsRefrigerator())
                {
                    gasGenerator = (listaToCheck[i] as IMyGasGenerator);
                    if (!refrigerators.Contains(gasGenerator))
                        refrigerators.Add(gasGenerator);
                }
                if (listaToCheck[i].BlockDefinition.Id.IsComposter())
                {
                    gasGenerator = (listaToCheck[i] as IMyGasGenerator);
                    if (!composters.Contains(gasGenerator))
                        composters.Add(gasGenerator);
                }
                IMyGasTank gasTank = null;
                if (listaToCheck[i].BlockDefinition.Id.IsGasTank())
                {
                    gasTank = (listaToCheck[i] as IMyGasTank);
                    if (!gasTanks.Contains(gasTank))
                        gasTanks.Add(gasTank);
                }
                if (listaToCheck[i].BlockDefinition.Id.IsAssembler() || listaToCheck[i].BlockDefinition.Id.IsRefinery())
                    targetInventory = 1;
                var inventoryBase = listaToCheck[i].GetInventory(targetInventory);
                if (inventoryBase != null)
                {                    
                    if (inventoryBase.GetItemsCount() > 0)
                    {
                        // Move itens para o iventário possivel
                        TryToFullFromInventory(inventoryBase, listaToCheck, null, reactor, gasGenerator);
                        // Verifica se tem algo nos inventários de produção, se for uma montadora
                        if (listaToCheck[i].BlockDefinition.Id.IsAssembler())
                        {
                            var assembler = (listaToCheck[i] as IMyAssembler);
                            var inventoryProd = listaToCheck[i].GetInventory(0);
                            TryToFullFromInventory(inventoryProd, listaToCheck, assembler, null, null);
                        }
                    }
                }

            }
        }

        private void DoFillBottles(List<IMyGasGenerator> gasGenerators, List<IMyGasTank> gasTanks)
        {
            if (Settings.GetFillBottles())
            {

            }
        }

        private void DoFillFishTrap(List<IMyGasGenerator> fishTraps)
        {
            if (Settings.GetFillFishTrap())
            {
                foreach (var fishTrap in fishTraps)
                {
                    var fishTrapDef = MyDefinitionManager.Static.GetCubeBlockDefinition(fishTrap.BlockDefinition) as MyOxygenGeneratorDefinition;
                    var fishTrapInventory = fishTrap.GetInventory(0) as MyInventory;
                    var size = fishTrapDef.Size.X * fishTrapDef.Size.Y * fishTrapDef.Size.Z;
                    var targetFuelsId = new Dictionary<MyDefinitionId, float> 
                    {
                        { ItensConstants.FISH_NOBLE_BAIT_ID.DefinitionId, IDEAL_FISHTRAP_NOBLEBAIT },
                        { ItensConstants.FISH_BAIT_SMALL_ID.DefinitionId, IDEAL_FISHTRAP_BAIT }
                    };
                    foreach (var targetFuelId in targetFuelsId.Keys)
                    {
                        var fuelInFishTrap = (float)fishTrapInventory.GetItemAmount(targetFuelId);
                        var value = targetFuelsId[targetFuelId];
                        var targetFuel = value * size;
                        if (targetFuel > 0)
                        {
                            if (fuelInFishTrap < targetFuel)
                            {
                                var fuelToAdd = targetFuel - fuelInFishTrap;
                                for (int i = 0; i < Settings.GetDefinitions().Count; i++)
                                {
                                    var def = Settings.GetDefinitions()[i];
                                    var targetBlock = ValidInventories.FirstOrDefault(x => x.EntityId == def.EntityId);
                                    var targetInventory = targetBlock.GetInventory(0);
                                    var fuelAmount = (float)targetInventory.GetItemAmount(targetFuelId);
                                    if (fuelAmount > 0)
                                    {
                                        if ((targetInventory as IMyInventory).CanTransferItemTo(fishTrapInventory, targetFuelId))
                                        {
                                            var builder = ItensConstants.GetPhysicalObjectBuilder(new UniqueEntityId(targetFuelId));
                                            var amountToTransfer = fuelAmount > fuelToAdd ? fuelToAdd : fuelAmount;
                                            InvokeOnGameThread(() =>
                                            {
                                                var amountTransfered = fishTrapInventory.AddMaxItems(amountToTransfer, builder);
                                                targetInventory.RemoveItemsOfType((MyFixedPoint)amountTransfered, builder);
                                            });
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }                    
                }
            }
        }

        private void DoFillComposter(List<IMyGasGenerator> composters)
        {
            if (Settings.GetFillComposter())
            {
                foreach (var composter in composters)
                {
                    var composterDef = MyDefinitionManager.Static.GetCubeBlockDefinition(composter.BlockDefinition) as MyOxygenGeneratorDefinition;
                    var targetFuelId = ItensConstants.ORGANIC_ID.DefinitionId;
                    var composterInventory = composter.GetInventory(0) as MyInventory;
                    var fuelInComposter = (float)composterInventory.GetItemAmount(targetFuelId);
                    var size = composterDef.Size.X * composterDef.Size.Y * composterDef.Size.Z;
                    var value = IDEAL_COMPOSTER_ORGANIC;
                    var targetFuel = value * size;
                    if (targetFuel > 0)
                    {
                        if (fuelInComposter < targetFuel)
                        {
                            var fuelToAdd = targetFuel - fuelInComposter;
                            for (int i = 0; i < Settings.GetDefinitions().Count; i++)
                            {
                                var def = Settings.GetDefinitions()[i];
                                var targetBlock = ValidInventories.FirstOrDefault(x => x.EntityId == def.EntityId);
                                var targetInventory = targetBlock.GetInventory(0);
                                var fuelAmount = (float)targetInventory.GetItemAmount(targetFuelId);
                                if (fuelAmount > 0)
                                {
                                    if ((targetInventory as IMyInventory).CanTransferItemTo(composterInventory, targetFuelId))
                                    {
                                        var builder = ItensConstants.GetPhysicalObjectBuilder(new UniqueEntityId(targetFuelId));
                                        var amountToTransfer = fuelAmount > fuelToAdd ? fuelToAdd : fuelAmount;
                                        InvokeOnGameThread(() =>
                                        {
                                            var amountTransfered = composterInventory.AddMaxItems(amountToTransfer, builder);
                                            targetInventory.RemoveItemsOfType((MyFixedPoint)amountTransfered, builder);
                                        });
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DoFillRefrigerator(List<IMyGasGenerator> refrigerators)
        {
            if (Settings.GetFillRefrigerator())
            {
                MyObjectBuilderType targetType = typeof(MyObjectBuilder_ConsumableItem);
                var query = Settings.GetDefinitions().Where(x => x.ValidTypes.Contains(targetType) || x.ValidIds.Any(y => y.TypeId == targetType));
                if (query.Any())
                {
                    foreach (var def in query)
                    {
                        var targetBlock = ValidInventories.FirstOrDefault(x => x.EntityId == def.EntityId);
                        var targetInventory = targetBlock.GetInventory(0);
                        if (targetInventory.ItemCount > 0)
                        {
                            /* start in the end */
                            for (int i = targetInventory.ItemCount - 1; i >= 0; i--)
                            {
                                var item = targetInventory.GetItemAt(i);
                                if (item.HasValue)
                                {
                                    MyObjectBuilderType itemType;
                                    if (MyObjectBuilderType.TryParse(item.Value.Type.TypeId, out itemType))
                                    {
                                        if (itemType == targetType)
                                        {
                                            foreach (var refrigerator in refrigerators)
                                            {
                                                var refrigeratorInventory = refrigerator.GetInventory(0) as MyInventory;
                                                if ((targetInventory as IMyInventory).CanTransferItemTo(refrigeratorInventory, item.Value.Type))
                                                {
                                                    var itemSlot = targetInventory.GetItemByID(item.Value.ItemId);
                                                    if (itemSlot.HasValue)
                                                    {
                                                        InvokeOnGameThread(() =>
                                                        {
                                                            var amountTransfered = refrigeratorInventory.AddMaxItems((float)item.Value.Amount, itemSlot.Value.Content);
                                                            targetInventory.RemoveItemsOfType((MyFixedPoint)amountTransfered, itemSlot.Value.Content);
                                                        });
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DoFillGasGenerator(List<IMyGasGenerator> gasGenerators)
        {
            if (Settings.GetFillGasGenerator())
            {
                foreach (var gasGenerator in gasGenerators)
                {
                    var gasGeneratorDef = MyDefinitionManager.Static.GetCubeBlockDefinition(gasGenerator.BlockDefinition) as MyOxygenGeneratorDefinition;
                    var targetFuelId = ItensConstants.ICE_ID.DefinitionId;
                    var gasGeneratorInventory = gasGenerator.GetInventory(0) as MyInventory;
                    var fuelInGasGenerator = (float)gasGeneratorInventory.GetItemAmount(targetFuelId);
                    var size = gasGeneratorDef.Size.X * gasGeneratorDef.Size.Y * gasGeneratorDef.Size.Z;
                    var value = gasGeneratorDef.CubeSize == MyCubeSize.Large ? Settings.GetLargeGasGeneratorAmount() : Settings.GetSmallGasGeneratorAmount();
                    var targetFuel = value * size;
                    if (targetFuel > 0)
                    {
                        if (fuelInGasGenerator < targetFuel)
                        {
                            var fuelToAdd = targetFuel - fuelInGasGenerator;
                            for (int i = 0; i < Settings.GetDefinitions().Count; i++)
                            {
                                var def = Settings.GetDefinitions()[i];
                                var targetBlock = ValidInventories.FirstOrDefault(x => x.EntityId == def.EntityId);
                                var targetInventory = targetBlock.GetInventory(0);
                                var fuelAmount = (float)targetInventory.GetItemAmount(targetFuelId);
                                if (fuelAmount > 0)
                                {
                                    if ((targetInventory as IMyInventory).CanTransferItemTo(gasGeneratorInventory, targetFuelId))
                                    {
                                        var builder = ItensConstants.GetPhysicalObjectBuilder(new UniqueEntityId(targetFuelId));
                                        var amountToTransfer = fuelAmount > fuelToAdd ? fuelToAdd : fuelAmount;
                                        InvokeOnGameThread(() =>
                                        {
                                            var amountTransfered = gasGeneratorInventory.AddMaxItems(amountToTransfer, builder);
                                            targetInventory.RemoveItemsOfType((MyFixedPoint)amountTransfered, builder);
                                        });
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DoFillReactors(List<IMyReactor> reactors)
        {
            if (Settings.GetFillReactor())
            {
                foreach (var reactor in reactors)
                {
                    var reactorDef = MyDefinitionManager.Static.GetCubeBlockDefinition(reactor.BlockDefinition) as MyReactorDefinition;
                    if (!reactorDef.FuelInfos.Any())
                        continue;
                    var targetFuelId = reactorDef.FuelInfos[0].FuelId;
                    var reactorInventory = reactor.GetInventory(0) as MyInventory;
                    var fuelInReactor = (float)reactorInventory.GetItemAmount(targetFuelId);
                    var size = reactorDef.Size.X * reactorDef.Size.Y * reactorDef.Size.Z;
                    var value = reactorDef.CubeSize == MyCubeSize.Large ? Settings.GetLargeReactorFuelAmount() : Settings.GetSmallReactorFuelAmount();
                    var targetFuel = value * size;
                    if (targetFuel > 0)
                    {
                        if (fuelInReactor < targetFuel)
                        {
                            var fuelToAdd = targetFuel - fuelInReactor;
                            for (int i = 0; i < Settings.GetDefinitions().Count; i++)
                            {
                                var def = Settings.GetDefinitions()[i];
                                var targetBlock = ValidInventories.FirstOrDefault(x => x.EntityId == def.EntityId);
                                var targetInventory = targetBlock.GetInventory(0);
                                var fuelAmount = (float)targetInventory.GetItemAmount(targetFuelId);
                                if (fuelAmount > 0)
                                {
                                    if ((targetInventory as IMyInventory).CanTransferItemTo(reactorInventory, targetFuelId))
                                    {
                                        var builder = ItensConstants.GetPhysicalObjectBuilder(new UniqueEntityId(targetFuelId));
                                        var amountToTransfer = fuelAmount > fuelToAdd ? fuelToAdd : fuelAmount;
                                        InvokeOnGameThread(() =>
                                        {
                                            var amountTransfered = reactorInventory.AddMaxItems(amountToTransfer, builder);
                                            targetInventory.RemoveItemsOfType((MyFixedPoint)amountTransfered, builder);
                                        });
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DoSortPullInventories()
        {
            if (Settings.GetSortItensType() != 0)
            {
                for (int i = 0; i < Settings.GetDefinitions().Count; i++)
                {
                    var def = Settings.GetDefinitions()[i];
                    var targetBlock = ValidInventories.FirstOrDefault(x => x.EntityId == def.EntityId);
                    var targetInventory = targetBlock.GetInventory(0);
                    if (targetInventory.ItemCount > 1)
                    {
                        int p = 1;
                        while (p < targetInventory.ItemCount)
                        {
                            var itemBefore = targetInventory.GetItemAt(p - 1);
                            var item = targetInventory.GetItemAt(p);
                            if (!item.HasValue || !itemBefore.HasValue)
                                break;
                            var itemBeforeDef = MyDefinitionManager.Static.GetPhysicalItemDefinition(itemBefore.Value.Type);
                            var itemDef = MyDefinitionManager.Static.GetPhysicalItemDefinition(item.Value.Type);
                            int dif = 0;
                            MyFixedPoint itemTotalMass = 0;
                            MyFixedPoint itemBeforeTotalMass = 0;
                            switch (Settings.GetSortItensType())
                            {
                                case 1: /* NAME */
                                    dif = itemDef.DisplayNameText.CompareTo(itemBeforeDef.DisplayNameText) * -1; /* A -> Z */
                                    break;
                                case 2: /* MASS */
                                    itemTotalMass = itemDef.Mass * item.Value.Amount;
                                    itemBeforeTotalMass = itemBeforeDef.Mass * itemBefore.Value.Amount;
                                    dif = itemTotalMass < itemBeforeTotalMass ? -1 : (itemTotalMass > itemBeforeTotalMass ? 1 : 0); /* + -> - */
                                    break;
                                case 3: /* TYPE NAME (ITEM NAME) */
                                    dif = itemDef.Id.TypeId.ToString().CompareTo(itemBeforeDef.Id.TypeId.ToString()) * -1; /* A -> Z */
                                    if (dif == 0)
                                        dif = itemDef.DisplayNameText.CompareTo(itemBeforeDef.DisplayNameText) * -1; /* A -> Z */
                                    break;
                                case 4: /* TYPE NAME (ITEM MASS) */
                                    dif = itemDef.Id.TypeId.ToString().CompareTo(itemBeforeDef.Id.TypeId.ToString()) * -1; /* A -> Z */
                                    if (dif == 0)
                                    {
                                        itemTotalMass = itemDef.Mass * item.Value.Amount;
                                        itemBeforeTotalMass = itemBeforeDef.Mass * itemBefore.Value.Amount;
                                        dif = itemTotalMass < itemBeforeTotalMass ? -1 : (itemTotalMass > itemBeforeTotalMass ? 1 : 0); /* + -> - */
                                    }
                                    break;
                            }
                            if (dif > 0)
                            {
                                InvokeOnGameThread(() =>
                                {
                                    MyInventory.Transfer(targetInventory, targetInventory, item.Value.ItemId, p - 1);
                                });
                            }
                            else
                                p++;
                        }
                    }
                }
            }
        }

        protected void DoExecuteCycle()
        {
            List<MyCubeGrid> subgrids;
            Dictionary<IMyShipConnector, ShipConnected > connectedGrids;
            var power = GetPowerConsumption(out subgrids, out connectedGrids);
            if (power != Settings.GetPowerConsumption())
            {
                Settings.SetPowerConsumption(power);
                SendPowerToClient();
                CurrentEntity.RefreshCustomInfo();
            }
            if (IsWorking)
            {
                List<IMyReactor> reactors = new List<IMyReactor>();
                List<IMyGasGenerator> gasGenerators = new List<IMyGasGenerator>();
                List<IMyGasTank> gasTanks = new List<IMyGasTank>();
                List<IMyGasGenerator> composters = new List<IMyGasGenerator>();
                List<IMyGasGenerator> fishTraps = new List<IMyGasGenerator>();
                List<IMyGasGenerator> refrigerators = new List<IMyGasGenerator>();
                DoCheckInventoryList(ValidInventories.ToArray(), ref reactors, ref gasGenerators, ref gasTanks, ref composters, ref fishTraps, ref refrigerators);
                if (Settings.GetPullSubGrids() && subgrids != null && subgrids.Any())
                {
                    foreach (var grid in subgrids)
                    {
                        DoCheckInventoryList(DoApplyBasicFilter(grid.Inventories).ToArray(), ref reactors, ref gasGenerators, ref gasTanks, ref composters, ref fishTraps, ref refrigerators);
                    }
                }
                if (Settings.GetPullFromConnectedGrids() && connectedGrids != null && connectedGrids.Any())
                {
                    foreach (var connector in connectedGrids.Keys)
                    {
                        if (!Settings.GetIgnoreConnectors().Contains(connector.EntityId))
                        {
                            DoCheckInventoryList(DoApplyBasicFilter(connectedGrids[connector].Grid.Inventories).ToArray(), ref reactors, ref gasGenerators, ref gasTanks, ref composters, ref fishTraps, ref refrigerators);
                        }
                    }
                }
                DoFillReactors(reactors);
                DoFillGasGenerator(gasGenerators);
                DoFillFishTrap(fishTraps);
                DoFillRefrigerator(refrigerators);
                DoFillComposter(composters);
                DoFillBottles(gasGenerators, gasTanks);
                DoSortPullInventories();
            }
        }

        private void TryToFullFromInventory(MyInventory inventoryBase, MyCubeBlock[] listaToCheck, IMyAssembler assembler, IMyReactor reactor, IMyGasGenerator gasGenerator)
        {
            var pullAll = true;
            var ignoreTypes = new List<MyObjectBuilderType>();
            var ignoreIds = new ConcurrentDictionary<MyDefinitionId, MyFixedPoint>();
            var maxForIds = new ConcurrentDictionary<MyDefinitionId, MyFixedPoint>();
            if (assembler != null)
            {
                pullAll = assembler.IsQueueEmpty;
                if (!pullAll)
                {
                    foreach (var queue in assembler.GetQueue())
                    {
                        var blueprint = queue.Blueprint as MyBlueprintDefinitionBase;
                        foreach (var item in blueprint.Prerequisites)
                        {
                            if (!ignoreIds.ContainsKey(item.Id))
                                ignoreIds[item.Id] = item.Amount * queue.Amount;
                            ignoreIds[item.Id] += item.Amount * queue.Amount;
                        }                        
                    }
                }
            }
            if (reactor != null)
            {
                pullAll = false;
                var reactorDef = MyDefinitionManager.Static.GetCubeBlockDefinition(reactor.BlockDefinition) as MyReactorDefinition;
                foreach (var item in reactorDef.FuelInfos)
                {
                    ignoreIds[item.FuelId] = (MyFixedPoint)int.MaxValue;
                }
            }
            if (gasGenerator != null)
            {
                pullAll = false;
                var blockId = ((MyDefinitionId)gasGenerator.BlockDefinition);
                if (blockId.IsGasGenerator())
                {
                    ignoreIds[ItensConstants.ICE_ID.DefinitionId] = (MyFixedPoint)int.MaxValue;
                }
                else if (blockId.IsFishTrap())
                {
                    ignoreIds[ItensConstants.FISH_BAIT_SMALL_ID.DefinitionId] = (MyFixedPoint)int.MaxValue;
                    ignoreIds[ItensConstants.FISH_NOBLE_BAIT_ID.DefinitionId] = (MyFixedPoint)int.MaxValue;
                }
                else if (blockId.IsComposter())
                {
                    ignoreIds[ItensConstants.ORGANIC_ID.DefinitionId] = (MyFixedPoint)IDEAL_COMPOSTER_ORGANIC;
                }
                else if (blockId.IsRefrigerator())
                {
                    ignoreTypes.Add(typeof(MyObjectBuilder_ConsumableItem));
                }
            }
            var itemsToCheck = inventoryBase.GetItems().ToArray();
            for (int j = 0; j < itemsToCheck.Length; j++)
            {
                var itemid = itemsToCheck[j].Content.GetId();

                if (!pullAll)
                {
                    if (ignoreIds.ContainsKey(itemid))
                    {
                        var invAmount = inventoryBase.GetItemAmount(itemid);
                        if (invAmount <= ignoreIds[itemid])
                        {
                            continue;
                        }
                        maxForIds[itemid] = invAmount - ignoreIds[itemid];
                    }
                    else if (ignoreTypes.Contains(itemid.TypeId))
                    {
                        continue;
                    }
                }

                var validTargets = Settings.GetDefinitions().Where(x =>
                    (x.ValidIds.Contains(itemid) || x.ValidTypes.Contains(itemid.TypeId)) &&
                    (!x.IgnoreIds.Contains(itemid) && !x.IgnoreTypes.Contains(itemid.TypeId))
                );
                if (validTargets.Any())
                {
                    foreach (var validTarget in validTargets)
                    {
                        var targetBlock = ValidInventories.FirstOrDefault(x => x.EntityId == validTarget.EntityId);
                        if (targetBlock != null)
                        {
                            var targetInventory = targetBlock.GetInventory(0);
                            if ((inventoryBase as IMyInventory).CanTransferItemTo(targetInventory, itemid))
                            {
                                InvokeOnGameThread(() =>
                                {
                                    var amountTransfered = targetInventory.AddMaxItems(maxForIds.ContainsKey(itemid) ? (float)maxForIds[itemid] : (float)itemsToCheck[j].Amount, itemsToCheck[j].Content);
                                    inventoryBase.RemoveItemsOfType((MyFixedPoint)amountTransfered, itemid);
                                });
                            }
                        }
                    }
                }
            }
        }

        private void InvokeOnGameThread(Action action, bool wait = true)
        {
            bool isExecuting = true;
            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                try
                {
                    action.Invoke();
                }
                finally
                {
                    isExecuting = false;
                }
            });
            while (wait && isExecuting)
            {
                if (MyAPIGateway.Parallel != null)
                    MyAPIGateway.Parallel.Sleep(25);
            }
        }

        public override void CallFromClient(ulong caller, string method, CommandExtraParams extraParams)
        {
            base.CallFromClient(caller, method, extraParams);
            try
            {
                if (AILogisticsAutomationSettings.Instance.Debug)
                    AILogisticsAutomationLogging.Instance.LogInfo(GetType(), $"CallFromClient: caller={caller} - method={method}");
                switch (method)
                {
                    case "RequestSettings":
                        SendToClient(caller);
                        break;
                    case "SetSettings":
                        ReciveFromClient(
                            caller, 
                            extraParams.extraParams.FirstOrDefault(x => x.id == "KEY")?.data,
                            extraParams.extraParams.FirstOrDefault(x => x.id == "ACTION")?.data,
                            extraParams.extraParams.FirstOrDefault(x => x.id == "VALUE")?.data,
                            extraParams.extraParams.FirstOrDefault(x => x.id == "OWNER")?.data
                        );
                        break;
                }
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        public override void CallFromServer(string method, CommandExtraParams extraParams)
        {
            base.CallFromServer(method, extraParams);
            try
            {
                switch (method)
                {
                    case "UpdateSettings":
                        ReciveFromServer(extraParams.extraParams.FirstOrDefault(x => x.id == "DATA")?.data);
                        break;
                    case "UpdatePower":
                        var power = float.Parse(extraParams.extraParams.FirstOrDefault(x => x.id == "POWER")?.data);
                        Settings.SetPowerConsumption(power);
                        break;
                    case "SetSettings":
                        Settings.UpdateData(
                            extraParams.extraParams.FirstOrDefault(x => x.id == "KEY")?.data,
                            extraParams.extraParams.FirstOrDefault(x => x.id == "ACTION")?.data,
                            extraParams.extraParams.FirstOrDefault(x => x.id == "VALUE")?.data,
                            extraParams.extraParams.FirstOrDefault(x => x.id == "OWNER")?.data
                        );
                        break;
                }
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        public enum EmissiveState
        {
            Working,
            Disabled,
            Warning,
            Damaged,
            Alternative,
            Locked,
            Autolock,
            Constraint
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

        protected bool SetEmissiveState(EmissiveState state)
        {
            if (CurrentEntity.Render.RenderObjectIDs[0] != uint.MaxValue)
            {
                switch (state)
                {
                    case EmissiveState.Working:
                        (CurrentEntity as MyCubeBlock).SetEmissiveState(MyCubeBlock.m_emissiveNames.Working, CurrentEntity.Render.RenderObjectIDs[0]);
                        return true;
                    case EmissiveState.Disabled:
                        (CurrentEntity as MyCubeBlock).SetEmissiveState(MyCubeBlock.m_emissiveNames.Disabled, CurrentEntity.Render.RenderObjectIDs[0]);
                        return true;
                    case EmissiveState.Warning:
                        (CurrentEntity as MyCubeBlock).SetEmissiveState(MyCubeBlock.m_emissiveNames.Warning, CurrentEntity.Render.RenderObjectIDs[0]);
                        return true;
                    case EmissiveState.Damaged:
                        (CurrentEntity as MyCubeBlock).SetEmissiveState(MyCubeBlock.m_emissiveNames.Damaged, CurrentEntity.Render.RenderObjectIDs[0]);
                        return true;
                    case EmissiveState.Alternative:
                        (CurrentEntity as MyCubeBlock).SetEmissiveState(MyCubeBlock.m_emissiveNames.Alternative, CurrentEntity.Render.RenderObjectIDs[0]);
                        return true;
                    case EmissiveState.Locked:
                        (CurrentEntity as MyCubeBlock).SetEmissiveState(MyCubeBlock.m_emissiveNames.Locked, CurrentEntity.Render.RenderObjectIDs[0]);
                        return true;
                    case EmissiveState.Autolock:
                        (CurrentEntity as MyCubeBlock).SetEmissiveState(MyCubeBlock.m_emissiveNames.Autolock, CurrentEntity.Render.RenderObjectIDs[0]);
                        return true;
                    case EmissiveState.Constraint:
                        (CurrentEntity as MyCubeBlock).SetEmissiveState(MyCubeBlock.m_emissiveNames.Constraint, CurrentEntity.Render.RenderObjectIDs[0]);
                        return true;
                }
            }
            return false;
        }

    }

}