using Sandbox.ModAPI;
using VRage.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
using System;
using System.Text;
using VRage;
using VRage.Game.ModAPI;
using VRageMath;
using System.Linq;
using VRage.Utils;
using VRage.Game;
using Sandbox.Definitions;
using VRage.ObjectBuilders;
using System.Collections.Concurrent;
using Sandbox.Common.ObjectBuilders.Definitions;

namespace AILogisticsAutomation
{
    public abstract class BaseTerminalController<T, K> where T : BaseLogicComponent<K> where K : IMyCubeBlock
    {

        public class PhysicalItemInfo
        {

            public MyDefinitionId Id { get; set; }
            public string DisplayText { get; set; }
            public int Index { get; set; }
            public MyPhysicalItemDefinition ItemDefinition { get; set; }
            public MyTerminalControlComboBoxItem ComboBoxItem { get; set; }

        }

        public class PhysicalItemTypeInfo
        {

            public MyObjectBuilderType Type { get; set; }
            public string DisplayText { get; set; }
            public int Index { get; set; }
            public MyTerminalControlComboBoxItem ComboBoxItem { get; set; }
            public ConcurrentDictionary<MyDefinitionId, PhysicalItemInfo> Items { get; set; } = new ConcurrentDictionary<MyDefinitionId, PhysicalItemInfo>();

        }

        public ConcurrentDictionary<MyDefinitionId, PhysicalItemInfo> PhysicalItemIds { get; set; } = new ConcurrentDictionary<MyDefinitionId, PhysicalItemInfo>();
        public ConcurrentDictionary<MyObjectBuilderType, PhysicalItemTypeInfo> PhysicalItemTypes { get; set; } = new ConcurrentDictionary<MyObjectBuilderType, PhysicalItemTypeInfo>();

        protected virtual MyObjectBuilderType[] GetPhysicalItemFilter()
        {
            return new MyObjectBuilderType[] { typeof(MyObjectBuilder_TreeObject), typeof(MyObjectBuilder_Package) };
        }

        protected void DoLoadPhysicalItemIds()
        {
            var ignoredTypes = GetPhysicalItemFilter();
            PhysicalItemIds.Clear();
            PhysicalItemTypes.Clear();
            var list = MyDefinitionManager.Static.GetPhysicalItemDefinitions().Where(x => !ignoredTypes.Contains(x.Id.TypeId)).OrderBy(x => x.DisplayNameText).ToArray();
            for (int i = 0; i < list.Length; i++)
            {
                var item = list[i];
                if (!PhysicalItemIds.ContainsKey(item.Id))
                {
                    PhysicalItemIds[item.Id] = new PhysicalItemInfo()
                    {
                        Id = item.Id,
                        ItemDefinition = item,
                        DisplayText = item.DisplayNameText
                    };
                    if (!PhysicalItemTypes.ContainsKey(item.Id.TypeId))
                    {
                        PhysicalItemTypes[item.Id.TypeId] = new PhysicalItemTypeInfo()
                        {
                            Type = item.Id.TypeId,
                            DisplayText = item.Id.TypeId.ToString().Replace(MyObjectBuilderType.LEGACY_TYPE_PREFIX, "")
                        };
                    }
                }
            }
            // Sort Items
            var ordenedIdList = PhysicalItemIds.OrderBy(x => x.Value.DisplayText).Select(x => x.Key).ToArray();
            for (int i = 0; i < ordenedIdList.Length; i++)
            {
                PhysicalItemIds[ordenedIdList[i]].Index = i;
            }
            // Create Itens Combo Box
            foreach (var id in PhysicalItemIds.Keys)
            {
                PhysicalItemIds[id].ComboBoxItem = new MyTerminalControlComboBoxItem()
                {
                    Key = PhysicalItemIds[id].Index,
                    Value = MyStringId.GetOrCompute(PhysicalItemIds[id].DisplayText)
                };
            }
            // Sort Types
            var ordenedTypesList = PhysicalItemTypes.OrderBy(x => x.Value.DisplayText).Select(x => x.Key).ToArray();
            for (int i = 0; i < ordenedTypesList.Length; i++)
            {
                PhysicalItemTypes[ordenedTypesList[i]].Index = i;
            }
            // Create Types Combo Box
            foreach (var id in PhysicalItemTypes.Keys)
            {
                PhysicalItemTypes[id].ComboBoxItem = new MyTerminalControlComboBoxItem()
                {
                    Key = PhysicalItemTypes[id].Index,
                    Value = MyStringId.GetOrCompute(PhysicalItemTypes[id].DisplayText)
                };
                // Create type item list
                PhysicalItemTypes[id].Items.Clear();
                var listaItens = PhysicalItemIds.Values.Where(x => x.Id.TypeId == id).OrderBy(x => x.Index).ToArray();
                for (int i = 0; i < listaItens.Length; i++)
                {
                    var item = listaItens[i];
                    PhysicalItemTypes[id].Items[item.Id] = new PhysicalItemInfo()
                    {
                        Id = item.Id,
                        Index = i,
                        DisplayText = item.DisplayText,
                        ItemDefinition = item.ItemDefinition,
                        ComboBoxItem = new MyTerminalControlComboBoxItem()
                        {
                            Key = i,
                            Value = MyStringId.GetOrCompute(item.DisplayText)
                        }
                    };
                }
            }
        }

        public const float SATURATION_DELTA = 0.8f;
        public const float VALUE_DELTA = 0.55f;
        public const float VALUE_COLORIZE_DELTA = 0.1f;

        public bool CustomControlsInit { get; private set; }

        protected List<IMyTerminalControl> CustomControls = new List<IMyTerminalControl>();
        protected List<IMyTerminalAction> CustomActions = new List<IMyTerminalAction>();

        protected abstract bool CanAddControls(IMyTerminalBlock block);
        protected abstract void DoInitializeControls();
        protected abstract string GetActionPrefix();
        protected abstract string[] GetIdsToRemove();

        public void InitializeControls()
        {
            lock (CustomControls)
            {
                if (CustomControlsInit) return;
                CustomControlsInit = true;
                try
                {

                    MyAPIGateway.TerminalControls.CustomControlGetter += CustomControlGetter;
                    MyAPIGateway.TerminalControls.CustomActionGetter += CustomActionGetter;

                    DoInitializeControls();
                }
                catch (Exception ex)
                {
                    AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
                }
            }
        }

        private void CustomActionGetter(IMyTerminalBlock block, List<IMyTerminalAction> actions)
        {
            if (CanAddControls(block))
            {
                foreach (var item in CustomActions)
                {
                    actions.Add(item);
                }
            }
        }

        protected T GetSystem(IMyTerminalBlock block)
        {
            if (block != null && block.GameLogic != null) return block.GameLogic.GetAs<T>();
            return null;
        }

        protected void CreateComboBoxAction(string name, IMyTerminalControlCombobox combobox)
        {
            var list = new List<MyTerminalControlComboBoxItem>();
            combobox.ComboBoxContent(list);

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];

                var action = MyAPIGateway.TerminalControls.CreateAction<K>(string.Format("{0}_{1}", name, i));
                action.Name = new StringBuilder(string.Format("{0} {1}", name, item.Value.String));
                action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
                action.Enabled = combobox.Enabled;
                action.Action = (block) =>
                {
                    combobox.Setter(block, item.Key);
                };
                action.Writer = (block, result) =>
                {
                    var value = combobox.Getter(block);
                    var query = list.Where(x => x.Key == value);
                    result.Append(query.Any() ? query.FirstOrDefault().Value.String : "-");
                };
                action.ValidForGroups = combobox.SupportsMultipleBlocks;
                CustomActions.Add(action);

            }
        }

        protected void CreateCheckBoxAction(string name, IMyTerminalControlCheckbox checkbox)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<K>(string.Format("{0}_OnOff", name));
            action.Name = new StringBuilder(string.Format("{0} On/Off", name));
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Enabled = checkbox.Enabled;
            action.Action = (block) =>
            {
                checkbox.Setter(block, !checkbox.Getter(block));
            };
            action.Writer = (block, result) =>
            {
                result.Append(checkbox.Getter(block) ? MyTexts.Get(checkbox.OnText) : MyTexts.Get(checkbox.OffText));
            };
            action.ValidForGroups = checkbox.SupportsMultipleBlocks;
            CustomActions.Add(action);

            action = MyAPIGateway.TerminalControls.CreateAction<K>(string.Format("{0}_On", name));
            action.Name = new StringBuilder(string.Format("{0} On", name));
            action.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
            action.Enabled = checkbox.Enabled;
            action.Action = (block) =>
            {
                checkbox.Setter(block, true);
            };
            action.Writer = (block, result) =>
            {
                result.Append(checkbox.Getter(block) ? MyTexts.Get(checkbox.OnText) : MyTexts.Get(checkbox.OffText));
            };
            action.ValidForGroups = checkbox.SupportsMultipleBlocks;
            CustomActions.Add(action);

            action = MyAPIGateway.TerminalControls.CreateAction<K>(string.Format("{0}_Off", name));
            action.Name = new StringBuilder(string.Format("{0} Off", name));
            action.Icon = @"Textures\GUI\Icons\Actions\SwitchOff.dds";
            action.Enabled = checkbox.Enabled;
            action.Action = (block) =>
            {
                checkbox.Setter(block, false);
            };
            action.Writer = (block, result) =>
            {
                result.Append(checkbox.Getter(block) ? MyTexts.Get(checkbox.OnText) : MyTexts.Get(checkbox.OffText));
            };
            action.ValidForGroups = checkbox.SupportsMultipleBlocks;
            CustomActions.Add(action);
        }

        protected void CreateOnOffSwitchAction(string name, IMyTerminalControlOnOffSwitch onoffSwitch)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<K>(string.Format("{0}_OnOff", name));
            action.Name = new StringBuilder(string.Format("{0} {1}/{2}", name, onoffSwitch.OnText, onoffSwitch.OffText));
            action.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            action.Enabled = onoffSwitch.Enabled;
            action.Action = (block) =>
            {
                onoffSwitch.Setter(block, !onoffSwitch.Getter(block));
            };
            action.ValidForGroups = onoffSwitch.SupportsMultipleBlocks;
            CustomActions.Add(action);

            action = MyAPIGateway.TerminalControls.CreateAction<K>(string.Format("{0}_On", name));
            action.Name = new StringBuilder(string.Format("{0} {1}", name, onoffSwitch.OnText));
            action.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
            action.Enabled = onoffSwitch.Enabled;
            action.Action = (block) =>
            {
                onoffSwitch.Setter(block, true);
            };
            action.ValidForGroups = onoffSwitch.SupportsMultipleBlocks;
            CustomActions.Add(action);

            action = MyAPIGateway.TerminalControls.CreateAction<K>(string.Format("{0}_Off", name));
            action.Name = new StringBuilder(string.Format("{0} {1}", name, onoffSwitch.OffText));
            action.Icon = @"Textures\GUI\Icons\Actions\SwitchOff.dds";
            action.Enabled = onoffSwitch.Enabled;
            action.Action = (block) =>
            {
                onoffSwitch.Setter(block, false);
            };
            action.ValidForGroups = onoffSwitch.SupportsMultipleBlocks;
            CustomActions.Add(action);
        }

        protected void CreateProperty(IMyTerminalValueControl<T> control, bool readOnly = false)
        {
            var property = MyAPIGateway.TerminalControls.CreateProperty<T, K>(GetActionPrefix() + "." + control.Id);
            property.SupportsMultipleBlocks = false;
            property.Getter = control.Getter;
            if (!readOnly) property.Setter = control.Setter;
            MyAPIGateway.TerminalControls.AddControl<K>(property);
        }

        protected void CreateSliderActions(string sliderName, IMyTerminalControlSlider slider)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<K>(string.Format("{0}_Increase", sliderName));
            action.Name = new StringBuilder(string.Format("{0} Increase", sliderName));
            action.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            action.Enabled = slider.Enabled;
            action.Action = (block) =>
            {
                var val = slider.Getter(block);
                slider.Setter(block, val + 1);
            };
            action.ValidForGroups = slider.SupportsMultipleBlocks;
            CustomActions.Add(action);

            action = MyAPIGateway.TerminalControls.CreateAction<K>(string.Format("{0}_Decrease", sliderName));
            action.Name = new StringBuilder(string.Format("{0} Decrease", sliderName));
            action.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            action.Enabled = slider.Enabled;
            action.Action = (block) =>
            {
                var val = slider.Getter(block);
                slider.Setter(block, val - 1);
            };
            action.ValidForGroups = slider.SupportsMultipleBlocks;
            CustomActions.Add(action);
        }

        protected Vector3 CheckConvertToHSVColor(Vector3 value)
        {
            if (value.X < 0f) value.X = 0f;
            if (value.X > 360f) value.X = 360f;
            if (value.Y < 0f) value.Y = 0f;
            if (value.Y > 100f) value.Y = 100f;
            if (value.Z < 0f) value.Z = 0f;
            if (value.Z > 100f) value.Z = 100f;

            return new Vector3(value.X / 360f,
                              (value.Y / 100f) - SATURATION_DELTA,
                              (value.Z / 100f) - VALUE_DELTA + VALUE_COLORIZE_DELTA);
        }

        protected Vector3 ConvertFromHSVColor(Vector3 value)
        {
            return new Vector3(value.X * 360f,
                              (value.Y + SATURATION_DELTA) * 100f,
                              (value.Z + VALUE_DELTA - VALUE_COLORIZE_DELTA) * 100f);
        }

        protected IMyTerminalControlLabel CreateTerminalLabel(string name, string text)
        {
            var label = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, K>(name);
            label.Label = MyStringId.GetOrCompute(text);
            CustomControls.Add(label);
            return label;
        }

        protected IMyTerminalControlSeparator CreateTerminalSeparator(string name)
        {
            var separator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, K>(name);
            CustomControls.Add(separator);
            return separator;
        }

        protected IMyTerminalControlCheckbox CreateCheckbox(string name, string title, Func<IMyTerminalBlock, bool> enabled,
            Func<IMyTerminalBlock, bool> getter, Action<IMyTerminalBlock, bool> setter, bool supMultiple = false, string tooltip = null,
            string onText = "Yes", string offText = "No", Func<IMyTerminalBlock, bool> visible = null)
        {
            var onOffSwitch = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, K>(name);
            onOffSwitch.Title = MyStringId.GetOrCompute(title);
            onOffSwitch.Tooltip = MyStringId.GetOrCompute(tooltip);
            onOffSwitch.Enabled = enabled;
            if (visible != null)
                onOffSwitch.Visible = visible;
            onOffSwitch.Getter = getter;
            onOffSwitch.Setter = setter;
            onOffSwitch.OnText = MyStringId.GetOrCompute(onText);
            onOffSwitch.OffText = MyStringId.GetOrCompute(offText);
            onOffSwitch.SupportsMultipleBlocks = supMultiple;
            CustomControls.Add(onOffSwitch);
            return onOffSwitch;
        }

        protected IMyTerminalControlTextbox CreateTextbox(string name, string title, Func<IMyTerminalBlock, bool> enabled,
            Func<IMyTerminalBlock, StringBuilder> getter, Action<IMyTerminalBlock, StringBuilder> setter, bool supMultiple = false,
            string tooltip = null, Func<IMyTerminalBlock, bool> visible = null)
        {
            var textbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, K>(name);
            textbox.Title = MyStringId.GetOrCompute(title);
            textbox.Tooltip = MyStringId.GetOrCompute(tooltip);
            textbox.Enabled = enabled;
            if (visible != null)
                textbox.Visible = visible;
            textbox.Getter = getter;
            textbox.Setter = setter;
            textbox.SupportsMultipleBlocks = supMultiple;
            CustomControls.Add(textbox);
            return textbox;
        }

        protected IMyTerminalControlOnOffSwitch CreateOnOffSwitch(string name, string title, Func<IMyTerminalBlock, bool> enabled,
            Func<IMyTerminalBlock, bool> getter, Action<IMyTerminalBlock, bool> setter, bool supMultiple = false, string tooltip = null,
            string onText = "On", string offText = "Off", Func<IMyTerminalBlock, bool> visible = null)
        {
            var onOffSwitch = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, K>(name);
            onOffSwitch.Title = MyStringId.GetOrCompute(title);
            onOffSwitch.Tooltip = MyStringId.GetOrCompute(tooltip);
            onOffSwitch.Enabled = enabled;
            if (visible != null)
                onOffSwitch.Visible = visible;
            onOffSwitch.Getter = getter;
            onOffSwitch.Setter = setter;
            onOffSwitch.OnText = MyStringId.GetOrCompute(onText);
            onOffSwitch.OffText = MyStringId.GetOrCompute(offText);
            onOffSwitch.SupportsMultipleBlocks = supMultiple;
            CustomControls.Add(onOffSwitch);
            return onOffSwitch;
        }

        protected IMyTerminalControlCombobox CreateCombobox(string name, string title, Func<IMyTerminalBlock, bool> enabled,
            Func<IMyTerminalBlock, long> getter, Action<IMyTerminalBlock, long> setter, Action<List<MyTerminalControlComboBoxItem>> comboBoxContent, 
            bool supMultiple = false, string tooltip = null, Func<IMyTerminalBlock, bool> visible = null)
        {
            var combobox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, K>(name);
            combobox.Title = MyStringId.GetOrCompute(title);
            combobox.Tooltip = MyStringId.GetOrCompute(tooltip);
            combobox.Enabled = enabled;
            if (visible != null)
                combobox.Visible = visible;
            combobox.ComboBoxContent = comboBoxContent;
            combobox.Getter = getter;
            combobox.Setter = setter;
            combobox.SupportsMultipleBlocks = supMultiple;
            CustomControls.Add(combobox);
            return combobox;
        }

        protected IMyTerminalControlSlider CreateSlider(string name, string title, Func<IMyTerminalBlock, bool> enabled,
            Func<IMyTerminalBlock, float> getter, Action<IMyTerminalBlock, float> setter, Action<IMyTerminalBlock, StringBuilder> writer,
            Vector2 limits, bool supMultiple = false, string tooltip = null, Func<IMyTerminalBlock, bool> visible = null)
        {
            var slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, K>(name);
            slider.Title = MyStringId.GetOrCompute(title);
            slider.Tooltip = MyStringId.GetOrCompute(tooltip);
            slider.SetLimits(limits.X, limits.Y);
            slider.Enabled = enabled;
            if (visible != null)
                slider.Visible = visible;
            slider.Writer = writer;
            slider.Getter = getter;
            slider.Setter = setter;
            slider.SupportsMultipleBlocks = supMultiple;
            CustomControls.Add(slider);
            return slider;
        }

        protected IMyTerminalControlListbox CreateListbox(string name, string title, Func<IMyTerminalBlock, bool> enabled,
            Action<IMyTerminalBlock, List<MyTerminalControlListBoxItem>, List<MyTerminalControlListBoxItem>> listContent,
            Action<IMyTerminalBlock, List<MyTerminalControlListBoxItem>> itemSelected, int visibleRowsCount = 5,
            bool supMultiple = false, string tooltip = null, Func<IMyTerminalBlock, bool> visible = null)
        {
            var listbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, K>(name);
            listbox.Title = MyStringId.GetOrCompute(title);
            listbox.Tooltip = MyStringId.GetOrCompute(tooltip);
            listbox.Enabled = enabled;
            if (visible != null)
                listbox.Visible = visible;
            listbox.ListContent = listContent;
            listbox.ItemSelected = itemSelected;
            listbox.SupportsMultipleBlocks = supMultiple;
            listbox.VisibleRowsCount = visibleRowsCount;
            CustomControls.Add(listbox);
            return listbox;
        }

        protected IMyTerminalControlButton CreateTerminalButton(string name, string title, Func<IMyTerminalBlock, bool> enabled, 
            Action<IMyTerminalBlock> action, bool supMultiple = false, string tooltip = null, 
            Func<IMyTerminalBlock, bool> visible = null) 
        {
            var button = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, K>(name);
            button.Title = MyStringId.GetOrCompute(title);
            button.Tooltip = MyStringId.GetOrCompute(tooltip);            
            button.Enabled = enabled;
            if (visible != null)
                button.Visible = visible;
            button.Action = action;
            button.SupportsMultipleBlocks = supMultiple;
            CustomControls.Add(button);
            return button;
        }

        protected void UpdateVisual(IMyTerminalControl control)
        {
            if (control != null) control.UpdateVisual();
        }

        protected void UpdateVisual(IMyTerminalBlock block)
        {
            foreach (var item in CustomControls)
            {
                UpdateVisual(item);
            }
            block.RefreshCustomInfo();
        }

        protected void Redraw(IMyTerminalControl control)
        {
            if (control != null) control.RedrawControl();
        }

        protected void Redraw()
        {
            foreach (var item in CustomControls)
            {
                Redraw(item);
            }
        }

        private static string[] OreDetectorReforgeIds = new string[] 
        {
            "Reforged: Separator",
            "Reforged: GPSColor",
            "Reforged: Range",
            "Reforged: Count",
            "Reforged: Whitelist",
            "Ores"
        };
        public void CustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (CanAddControls(block))
            {
                if (GetIdsToRemove().Any())
                {
                    controls.RemoveAll(x => GetIdsToRemove().Contains(x.Id));
                }
                if (AILogisticsAutomationSession.IsUsingOreDetectorReforge())
                {
                    controls.RemoveAll(x => OreDetectorReforgeIds.Contains(x.Id));
                }
                foreach (var item in CustomControls)
                {
                    controls.Add(item);
                }
            }
        }

    }

}