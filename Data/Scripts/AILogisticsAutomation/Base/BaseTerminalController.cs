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

namespace AILogisticsAutomation
{
    public abstract class BaseTerminalController<T, K> where T : BaseLogicComponent<K> where K : IMyCubeBlock
    {

        public const float SATURATION_DELTA = 0.8f;
        public const float VALUE_DELTA = 0.55f;
        public const float VALUE_COLORIZE_DELTA = 0.1f;

        public bool CustomControlsInit { get; private set; }

        protected List<IMyTerminalControl> CustomControls = new List<IMyTerminalControl>();

        protected abstract bool CanAddControls(IMyTerminalBlock block);
        protected abstract void DoInitializeControls();
        protected abstract string GetActionPrefix();

        public void InitializeControls()
        {
            lock (CustomControls)
            {
                if (CustomControlsInit) return;
                CustomControlsInit = true;
                try
                {

                    MyAPIGateway.TerminalControls.CustomControlGetter += CustomControlGetter;

                    DoInitializeControls();
                }
                catch (Exception ex)
                {
                    AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
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
                MyAPIGateway.TerminalControls.AddAction<K>(action);

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
            MyAPIGateway.TerminalControls.AddAction<K>(action);

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
            MyAPIGateway.TerminalControls.AddAction<K>(action);

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
            MyAPIGateway.TerminalControls.AddAction<K>(action);
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
            MyAPIGateway.TerminalControls.AddAction<K>(action);

            action = MyAPIGateway.TerminalControls.CreateAction<K>(string.Format("{0}_On", name));
            action.Name = new StringBuilder(string.Format("{0} {1}", name, onoffSwitch.OnText));
            action.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
            action.Enabled = onoffSwitch.Enabled;
            action.Action = (block) =>
            {
                onoffSwitch.Setter(block, true);
            };
            action.ValidForGroups = onoffSwitch.SupportsMultipleBlocks;
            MyAPIGateway.TerminalControls.AddAction<K>(action);

            action = MyAPIGateway.TerminalControls.CreateAction<K>(string.Format("{0}_Off", name));
            action.Name = new StringBuilder(string.Format("{0} {1}", name, onoffSwitch.OffText));
            action.Icon = @"Textures\GUI\Icons\Actions\SwitchOff.dds";
            action.Enabled = onoffSwitch.Enabled;
            action.Action = (block) =>
            {
                onoffSwitch.Setter(block, false);
            };
            action.ValidForGroups = onoffSwitch.SupportsMultipleBlocks;
            MyAPIGateway.TerminalControls.AddAction<K>(action);
        }

        protected void CreateProperty<T>(IMyTerminalValueControl<T> control, bool readOnly = false)
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
            MyAPIGateway.TerminalControls.AddAction<K>(action);

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
            MyAPIGateway.TerminalControls.AddAction<K>(action);
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

        public void CustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (CanAddControls(block))
            {
                foreach (var item in CustomControls)
                {
                    controls.Add(item);
                }
            }
        }

    }

}