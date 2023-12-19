using Evergine.Editor.Extension;
using Evergine.Editor.Extension.Attributes;
using SkyboxAI.Components;

namespace SkyboxAI
{
    [CustomPanelEditor(typeof(BlockadeLabsSkybox))]
    public class BlockadeLabsSkyboxEditor : PanelEditor
    {
        private static string ApiKey;
        private static string Prompt;
        private static string Styles;
        private static string GenerateAISkybox;
        private static string Percentage;

        private BlockadeLabsSkybox component;

        protected override async void Loaded()
        {
            base.Loaded();
            this.component = this.Instance as BlockadeLabsSkybox;
            
            await this.component.GetSkyboxStyleOptions();

            this.propertyPanelContainer.InvalidateLayout();
        }

        public override void GenerateUI()
        {            
            this.propertyPanelContainer.AddText(
                nameof(ApiKey),
                nameof(ApiKey),
                getValue: () => this.component.apiKey,
                setValue: x => this.component.apiKey = x);

            this.propertyPanelContainer.AddText(
                nameof(Prompt),
                nameof(Prompt),
                getValue: () => this.component.Prompt,
                setValue: x => this.component.Prompt = x);

            this.propertyPanelContainer.AddSelector(
                nameof(Styles),
                nameof(Styles),
                this.component.skyboxStyleOptions,
                getValue: () => this.component.skyboxStyleSelected,
                setValue: (x) => this.component.skyboxStyleSelected = x);

            ////this.propertyPanelContainer.AddLabel(
            ////    nameof(Percentage),
            ////    $"{this.component.Percentage}%");

            this.propertyPanelContainer.AddButton(
                nameof(GenerateAISkybox),
                "Generate",
                this.component.GenerateSkybox);


        }
    }
}

