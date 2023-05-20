using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using VRage.Utils;
using VRageMath;

namespace AILogisticsAutomation
{

    [ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
    public class AILogisticsAutomationSettings : BaseSettings
    {

        private const int CURRENT_VERSION = 1;
        private const string FILE_NAME = "AILogisticsAutomation.Settings.xml";

        private static AILogisticsAutomationSettings _instance;
        public static AILogisticsAutomationSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Load();
                return _instance;
            }
        }

        private static bool Validate(AILogisticsAutomationSettings settings)
        {
            var res = true;
            return res;
        }

        public static AILogisticsAutomationSettings Load()
        {
            _instance = Load(FILE_NAME, CURRENT_VERSION, Validate, () => { return new AILogisticsAutomationSettings(); });
            return _instance;
        }

        public static void ClientLoad(string data)
        {
            _instance = GetData<AILogisticsAutomationSettings>(data);
        }

        public string GetDataToClient()
        {
            return GetData(this);
        }

        public static void Save()
        {
            try
            {
                Save(Instance, FILE_NAME, true);
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(typeof(AILogisticsAutomationSettings), ex);
            }
        }

        [XmlElement]
        public bool Debug { get; set; } = false;

        [XmlElement]
        public EnergyCostSettings EnergyCost { get; set; } = new EnergyCostSettings();

        public AILogisticsAutomationSettings()
        {

        }

        protected override void OnAfterLoad()
        {
            base.OnAfterLoad();

        }

        public bool SetConfigValue(string name, string value)
        {
            switch (name)
            {
                case "debug":
                    bool debug;
                    if (bool.TryParse(value, out debug))
                    {
                        Debug = debug;
                        return true;
                    }
                    break;
                case "energycost.defaultblockcost":
                    float energycostdefaultblockcost;
                    if (float.TryParse(value, out energycostdefaultblockcost))
                    {
                        EnergyCost.DefaultBlockCost = energycostdefaultblockcost;
                        return true;
                    }
                    break;
                case "energycost.defaultpullcost":
                    float energycostdefaultpullcost;
                    if (float.TryParse(value, out energycostdefaultpullcost))
                    {
                        EnergyCost.DefaultPullCost = energycostdefaultpullcost;
                        return true;
                    }
                    break;
                case "energycost.filtercost":
                    float energycostfiltercost;
                    if (float.TryParse(value, out energycostfiltercost))
                    {
                        EnergyCost.FilterCost = energycostfiltercost;
                        return true;
                    }
                    break;
                case "energycost.sortcost":
                    float energycostsortcost;
                    if (float.TryParse(value, out energycostsortcost))
                    {
                        EnergyCost.SortCost = energycostsortcost;
                        return true;
                    }
                    break;
                case "energycost.stackcost":
                    float energycoststackcost;
                    if (float.TryParse(value, out energycoststackcost))
                    {
                        EnergyCost.StackCost = energycoststackcost;
                        return true;
                    }
                    break;
                case "energycost.fillreactorcost":
                    float energycostfillreactorcost;
                    if (float.TryParse(value, out energycostfillreactorcost))
                    {
                        EnergyCost.FillReactorCost = energycostfillreactorcost;
                        return true;
                    }
                    break;
                case "energycost.fillgasgeneratorcost":
                    float energycostfillgasgeneratorcost;
                    if (float.TryParse(value, out energycostfillgasgeneratorcost))
                    {
                        EnergyCost.FillGasGeneratorCost = energycostfillgasgeneratorcost;
                        return true;
                    }
                    break;
                case "energycost.fillbottlescost":
                    float energycostfillbottlescost;
                    if (float.TryParse(value, out energycostfillbottlescost))
                    {
                        EnergyCost.FillBottlesCost = energycostfillbottlescost;
                        return true;
                    }
                    break;
                case "energycost.extendedsurvival.fillrefrigeratorcost":
                    float energycostfillrefrigeratorcost;
                    if (float.TryParse(value, out energycostfillrefrigeratorcost))
                    {
                        EnergyCost.ExtendedSurvival.FillRefrigeratorCost = energycostfillrefrigeratorcost;
                        return true;
                    }
                    break;
                case "energycost.extendedsurvival.fillfishtrapcost":
                    float energycostfillfishtrapcost;
                    if (float.TryParse(value, out energycostfillfishtrapcost))
                    {
                        EnergyCost.ExtendedSurvival.FillFishTrapCost = energycostfillfishtrapcost;
                        return true;
                    }
                    break;
                case "energycost.extendedsurvival.fillcompostercost":
                    float energycostfillcompostercost;
                    if (float.TryParse(value, out energycostfillcompostercost))
                    {
                        EnergyCost.ExtendedSurvival.FillComposterCost = energycostfillcompostercost;
                        return true;
                    }
                    break;
            }
            return false;
        }

    }

}
