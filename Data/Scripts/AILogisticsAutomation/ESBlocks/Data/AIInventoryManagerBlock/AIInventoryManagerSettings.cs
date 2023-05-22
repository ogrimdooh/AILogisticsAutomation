using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AILogisticsAutomation
{

    public class AIInventoryManagerSettings : IAIBlockSettings<AIInventoryManagerSettingsData>
    {

        /* Interface Properties */

        public long SelectedEntityId { get; set; } = 0;
        public long SelectedIgnoreEntityId { get; set; } = 0;
        public long SelectedAddedIgnoreEntityId { get; set; } = 0;
        public string SelectedAddedFilterId { get; set; } = "";

        /* Data Properties */

        private readonly ConcurrentDictionary<long, AIInventoryManagerCargoDefinition> definitions = new ConcurrentDictionary<long, AIInventoryManagerCargoDefinition>();
        public ConcurrentDictionary<long, AIInventoryManagerCargoDefinition> GetDefinitions()
        {
            return definitions;
        }

        private HashSet<long> ignoreCargos = new HashSet<long>();
        public HashSet<long> GetIgnoreCargos()
        {
            return ignoreCargos;
        }

        private readonly HashSet<long> ignoreFunctionalBlocks = new HashSet<long>();
        public HashSet<long> GetIgnoreFunctionalBlocks()
        {
            return ignoreFunctionalBlocks;
        }

        private readonly HashSet<long> ignoreConnectors = new HashSet<long>();
        public HashSet<long> GetIgnoreConnectors()
        {
            return ignoreConnectors;
        }

        private bool enabled = true;
        public bool GetEnabled()
        {
            return enabled;
        }
        public void SetEnabled(bool value)
        {
            enabled = value;
        }

        private bool pullFromConnectedGrids = true;
        public bool GetPullFromConnectedGrids()
        {
            return pullFromConnectedGrids;
        }
        public void SetPullFromConnectedGrids(bool value)
        {
            pullFromConnectedGrids = value;
        }

        private bool pullSubGrids = true;
        public bool GetPullSubGrids()
        {
            return pullSubGrids;
        }
        public void SetPullSubGrids(bool value)
        {
            pullSubGrids = value;
        }

        private long sortItensType = 0;
        public long GetSortItensType()
        {
            return sortItensType;
        }
        public void SetSortItensType(long value)
        {
            sortItensType = value;
        }

        private bool pullFromAssembler = false;
        public bool GetPullFromAssembler()
        {
            return pullFromAssembler;
        }
        public void SetPullFromAssembler(bool value)
        {
            pullFromAssembler = value;
        }

        private bool pullFromRefinary = false;
        public bool GetPullFromRefinary()
        {
            return pullFromRefinary;
        }
        public void SetPullFromRefinary(bool value)
        {
            pullFromRefinary = value;
        }

        private bool fillReactor = false;
        public bool GetFillReactor()
        {
            return fillReactor;
        }
        public void SetFillReactor(bool value)
        {
            fillReactor = value;
        }

        private float smallReactorFuelAmount = 5;
        public float GetSmallReactorFuelAmount()
        {
            return smallReactorFuelAmount;
        }
        public void SetSmallReactorFuelAmount(float value)
        {
            smallReactorFuelAmount = value;
        }

        private float largeReactorFuelAmount = 50;
        public float GetLargeReactorFuelAmount()
        {
            return largeReactorFuelAmount;
        }
        public void SetLargeReactorFuelAmount(float value)
        {
            largeReactorFuelAmount = value;
        }

        private bool pullFromReactor = false;
        public bool GetPullFromReactor()
        {
            return pullFromReactor;
        }
        public void SetPullFromReactor(bool value)
        {
            pullFromReactor = value;
        }

        private bool pullFromGasGenerator = false;
        public bool GetPullFromGasGenerator()
        {
            return pullFromGasGenerator;
        }
        public void SetPullFromGasGenerator(bool value)
        {
            pullFromGasGenerator = value;
        }

        private bool fillGasGenerator = false;
        public bool GetFillGasGenerator()
        {
            return fillGasGenerator;
        }
        public void SetFillGasGenerator(bool value)
        {
            fillGasGenerator = value;
        }

        private float smallGasGeneratorAmount = 35;
        public float GetSmallGasGeneratorAmount()
        {
            return smallGasGeneratorAmount;
        }
        public void SetSmallGasGeneratorAmount(float value)
        {
            smallGasGeneratorAmount = value;
        }

        private float largeGasGeneratorAmount = 1000;
        public float GetLargeGasGeneratorAmount()
        {
            return largeGasGeneratorAmount;
        }
        public void SetLargeGasGeneratorAmount(float value)
        {
            largeGasGeneratorAmount = value;
        }

        private bool fillBottles = false;
        public bool GetFillBottles()
        {
            return fillBottles;
        }
        public void SetFillBottles(bool value)
        {
            fillBottles = value;
        }

        private bool pullFromGasTank = false;
        public bool GetPullFromGasTank()
        {
            return pullFromGasTank;
        }
        public void SetPullFromGasTank(bool value)
        {
            pullFromGasTank = value;
        }

        private float powerConsumption = 0;
        public float GetPowerConsumption()
        {
            return powerConsumption;
        }
        public void SetPowerConsumption(float value)
        {
            powerConsumption = value;
        }

        private bool pullFromComposter;
        public bool GetPullFromComposter()
        {
            return pullFromComposter;
        }

        public void SetPullFromComposter(bool value)
        {
            pullFromComposter = value;
        }

        private bool fillComposter;
        public bool GetFillComposter()
        {
            return fillComposter;
        }

        public void SetFillComposter(bool value)
        {
            fillComposter = value;
        }

        private bool pullFishTrap;
        public bool GetPullFishTrap()
        {
            return pullFishTrap;
        }

        public void SetPullFishTrap(bool value)
        {
            pullFishTrap = value;
        }

        private bool fillFishTrap;
        public bool GetFillFishTrap()
        {
            return fillFishTrap;
        }

        public void SetFillFishTrap(bool value)
        {
            fillFishTrap = value;
        }

        private bool stackIfPossible;
        public bool GetStackIfPossible()
        {
            return stackIfPossible;
        }

        public void SetStackIfPossible(bool value)
        {
            stackIfPossible = value;
        }

        private bool pullRefrigerator;
        public bool GetPullRefrigerator()
        {
            return pullRefrigerator;
        }

        public void SetPullRefrigerator(bool value)
        {
            pullRefrigerator = value;
        }

        private bool fillRefrigerator;
        public bool GetFillRefrigerator()
        {
            return fillRefrigerator;
        }

        public void SetFillRefrigerator(bool value)
        {
            fillRefrigerator = value;
        }

        /* Contrutor */

        public AIInventoryManagerSettingsData GetData()
        {
            var data = new AIInventoryManagerSettingsData
            {
                definitions = definitions.Select(x => x.Value.GetData()).ToArray(),
                ignoreCargos = ignoreCargos.ToArray(),
                ignoreFunctionalBlocks = ignoreFunctionalBlocks.ToArray(),
                ignoreConnectors = ignoreConnectors.ToArray(),
                pullFromConnectedGrids = pullFromConnectedGrids,
                pullSubGrids = pullSubGrids,
                sortItensType = sortItensType,
                fillReactor = fillReactor,
                pullFromReactor = pullFromReactor,
                fillBottles = fillBottles,
                pullFromAssembler = pullFromAssembler,
                enabled = enabled,
                pullFromRefinary = pullFromRefinary,
                powerConsumption = powerConsumption,
                largeReactorFuelAmount = largeReactorFuelAmount,
                smallReactorFuelAmount = smallReactorFuelAmount,
                fillGasGenerator = fillGasGenerator,
                pullFromGasGenerator = pullFromGasGenerator,
                smallGasGeneratorAmount = smallGasGeneratorAmount,
                largeGasGeneratorAmount = largeGasGeneratorAmount,
                pullFromGasTank = pullFromGasTank,
                fillComposter = fillComposter,
                fillFishTrap = fillFishTrap,
                fillRefrigerator = fillRefrigerator,
                pullFishTrap = pullFishTrap,
                pullFromComposter = pullFromComposter,
                pullRefrigerator = pullRefrigerator,
                stackIfPossible = stackIfPossible
            };
            return data;
        }

        public bool UpdateData(string key, string action, string value, string owner)
        {
            long valueAsId = 0;
            bool valueAsFlag = false;
            int valueAsIndex = 0;
            float valueAsFloat = 0f;
            switch (key.ToUpper())
            {
                case "VALIDIDS":
                case "VALIDTYPES":
                case "IGNOREIDS":
                case "IGNORETYPES":
                    if (long.TryParse(owner, out valueAsId))
                    {
                        var def = definitions.ContainsKey(valueAsId) ? definitions[valueAsId] : null;
                        if (def != null)
                        {
                            return def.UpdateData(key, action, value);
                        }
                    }
                    break;
                case "DEFINITIONS":
                    if (long.TryParse(value, out valueAsId))
                    {
                        switch (action)
                        {
                            case "ADD":
                                definitions[valueAsId] = new AIInventoryManagerCargoDefinition() { EntityId = valueAsId };
                                return true;
                            case "DEL":
                                definitions.Remove(valueAsId);
                                return true;
                        }
                    }
                    break;
                case "IGNORECARGOS":
                    if (long.TryParse(value, out valueAsId))
                    {
                        switch (action)
                        {
                            case "ADD":
                                ignoreCargos.Add(valueAsId);
                                return true;
                            case "DEL":
                                ignoreCargos.Remove(valueAsId);
                                return true;
                        }
                    }
                    break;
                case "IGNOREFUNCTIONALBLOCKS":
                    if (long.TryParse(value, out valueAsId))
                    {
                        switch (action)
                        {
                            case "ADD":
                                ignoreFunctionalBlocks.Add(valueAsId);
                                return true;
                            case "DEL":
                                ignoreFunctionalBlocks.Remove(valueAsId);
                                return true;
                        }
                    }
                    break;
                case "IGNORECONNECTORS":
                    if (long.TryParse(value, out valueAsId))
                    {
                        switch (action)
                        {
                            case "ADD":
                                ignoreConnectors.Add(valueAsId);
                                return true;
                            case "DEL":
                                ignoreConnectors.Remove(valueAsId);
                                return true;
                        }
                    }
                    break;
                case "PULLFROMCONNECTEDGRIDS":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        pullFromConnectedGrids = valueAsFlag;
                        return true;
                    }
                    break;
                case "PULLSUBGRIDS":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        pullSubGrids = valueAsFlag;
                        return true;
                    }
                    break;
                case "SORTITENSTYPE":
                    if (int.TryParse(value, out valueAsIndex))
                    {
                        sortItensType = valueAsIndex;
                        return true;
                    }
                    break;
                case "FILLREACTOR":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        fillReactor = valueAsFlag;
                        return true;
                    }
                    break;
                case "PULLFROMREACTOR":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        pullFromReactor = valueAsFlag;
                        return true;
                    }
                    break;
                case "FILLBOTTLES":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        fillBottles = valueAsFlag;
                        return true;
                    }
                    break;
                case "PULLFROMASSEMBLER":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        pullFromAssembler = valueAsFlag;
                        return true;
                    }
                    break;
                case "ENABLED":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        enabled = valueAsFlag;
                        return true;
                    }
                    break;
                case "PULLFROMREFINARY":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        pullFromRefinary = valueAsFlag;
                        return true;
                    }
                    break;
                case "POWERCONSUMPTION":
                    if (float.TryParse(value, out valueAsFloat))
                    {
                        powerConsumption = valueAsFloat;
                        return true;
                    }
                    break;
                case "LARGEREACTORFUELAMOUNT":
                    if (float.TryParse(value, out valueAsFloat))
                    {
                        largeReactorFuelAmount = valueAsFloat;
                        return true;
                    }
                    break;
                case "SMALLREACTORFUELAMOUNT":
                    if (float.TryParse(value, out valueAsFloat))
                    {
                        smallReactorFuelAmount = valueAsFloat;
                        return true;
                    }
                    break;
                case "FILLGASGENERATOR":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        fillGasGenerator = valueAsFlag;
                        return true;
                    }
                    break;
                case "PULLFROMGASGENERATOR":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        pullFromGasGenerator = valueAsFlag;
                        return true;
                    }
                    break;
                case "SMALLGASGENERATORAMOUNT":
                    if (float.TryParse(value, out valueAsFloat))
                    {
                        smallGasGeneratorAmount = valueAsFloat;
                        return true;
                    }
                    break;
                case "LARGEGASGENERATORAMOUNT":
                    if (float.TryParse(value, out valueAsFloat))
                    {
                        largeGasGeneratorAmount = valueAsFloat;
                        return true;
                    }
                    break;
                case "PULLFROMGASTANK":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        pullFromGasTank = valueAsFlag;
                        return true;
                    }
                    break;
                case "FILLCOMPOSTER":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        fillComposter = valueAsFlag;
                        return true;
                    }
                    break;
                case "FILLFISHTRAP":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        fillFishTrap = valueAsFlag;
                        return true;
                    }
                    break;
                case "FILLREFRIGERATOR":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        fillRefrigerator = valueAsFlag;
                        return true;
                    }
                    break;
                case "PULLFISHTRAP":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        pullFishTrap = valueAsFlag;
                        return true;
                    }
                    break;
                case "PULLFROMCOMPOSTER":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        pullFromComposter = valueAsFlag;
                        return true;
                    }
                    break;
                case "PULLREFRIGERATOR":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        pullRefrigerator = valueAsFlag;
                        return true;
                    }
                    break;
                case "STACKIFPOSSIBLE":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        stackIfPossible = valueAsFlag;
                        return true;
                    }
                    break;
            }
            return false;
        }

        public void UpdateData(AIInventoryManagerSettingsData data)
        {
            var dataToRemove = definitions.Keys.Where(x => !data.definitions.Any(y => y.entityId == x)).ToArray();
            foreach (var item in dataToRemove)
            {
                definitions.Remove(item);
            }
            foreach (var item in data.definitions)
            {
                var def = definitions.ContainsKey(item.entityId) ? definitions[item.entityId] : null;
                if (def != null)
                {
                    def.UpdateData(item);
                }
                else
                {
                    var newItem = new AIInventoryManagerCargoDefinition()
                    {
                        EntityId = item.entityId
                    };
                    newItem.UpdateData(item);
                    definitions[item.entityId] = newItem;
                }
            }
            ignoreCargos.Clear();
            foreach (var item in data.ignoreCargos)
            {
                ignoreCargos.Add(item);
            }
            ignoreFunctionalBlocks.Clear();
            foreach (var item in data.ignoreFunctionalBlocks)
            {
                ignoreFunctionalBlocks.Add(item);
            }
            ignoreConnectors.Clear();
            foreach (var item in data.ignoreConnectors)
            {
                ignoreConnectors.Add(item);
            }
            pullFromConnectedGrids = data.pullFromConnectedGrids;
            pullSubGrids = data.pullSubGrids;
            sortItensType = data.sortItensType;
            fillReactor = data.fillReactor;
            pullFromReactor = data.pullFromReactor;
            fillBottles = data.fillBottles;
            pullFromAssembler = data.pullFromAssembler;
            pullFromRefinary = data.pullFromRefinary;
            smallReactorFuelAmount = data.smallReactorFuelAmount;
            largeReactorFuelAmount = data.largeReactorFuelAmount;
            fillGasGenerator = data.fillGasGenerator;
            pullFromGasGenerator = data.pullFromGasGenerator;
            smallGasGeneratorAmount = data.smallGasGeneratorAmount;
            largeGasGeneratorAmount = data.largeGasGeneratorAmount;
            pullFromGasTank = data.pullFromGasTank;
            stackIfPossible = data.stackIfPossible;
            powerConsumption = data.powerConsumption;
            pullFromComposter = data.pullFromComposter;
            fillComposter = data.fillComposter;
            pullFishTrap = data.pullFishTrap;
            fillFishTrap = data.fillFishTrap;
            pullRefrigerator = data.pullRefrigerator;
            fillRefrigerator = data.fillRefrigerator;
        }

    }

}