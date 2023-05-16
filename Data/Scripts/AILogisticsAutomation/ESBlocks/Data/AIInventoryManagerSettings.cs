using System.Collections.Generic;
using System.Linq;

namespace AILogisticsAutomation
{
    public class AIInventoryManagerSettings
    {

        /* Interface Properties */

        public long SelectedEntityId { get; set; } = 0;
        public long SelectedIgnoreEntityId { get; set; } = 0;
        public long SelectedAddedIgnoreEntityId { get; set; } = 0;
        public string SelectedAddedFilterId { get; set; } = "";

        /* Data Properties */

        private readonly List<AIInventoryManagerCargoDefinition> definitions = new List<AIInventoryManagerCargoDefinition>();
        public List<AIInventoryManagerCargoDefinition> GetDefinitions()
        {
            return definitions;
        }

        private List<long> ignoreCargos = new List<long>();
        public List<long> GetIgnoreCargos()
        {
            return ignoreCargos;
        }

        private readonly List<long> ignoreFunctionalBlocks = new List<long>();
        public List<long> GetIgnoreFunctionalBlocks()
        {
            return ignoreFunctionalBlocks;
        }

        private readonly List<long> ignoreConnectors = new List<long>();
        public List<long> GetIgnoreConnectors()
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

        public AIInventoryManagerSettingsData GetData(bool checkFlag)
        {
            var data = new AIInventoryManagerSettingsData
            {
                definitions = definitions.Select(x => x.GetData(checkFlag)).ToArray(),
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
                pullRefrigerator = pullRefrigerator
            };
            return data;
        }

        public void UpdateData(AIInventoryManagerSettingsData data)
        {
            var dataToRemove = definitions.Where(x => !data.definitions.Any(y => y.entityId == x.EntityId)).ToArray();
            foreach (var item in dataToRemove)
            {
                definitions.Remove(item);
            }
            foreach (var item in data.definitions)
            {
                var query = definitions.Where(x => x.EntityId == item.entityId);
                if (query.Any())
                {
                    var savedItem = query.FirstOrDefault();
                    savedItem.UpdateData(item);
                }
                else
                {
                    var newItem = new AIInventoryManagerCargoDefinition()
                    {
                        EntityId = item.entityId
                    };
                    newItem.UpdateData(item);
                    definitions.Add(newItem);
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
        }

    }

}