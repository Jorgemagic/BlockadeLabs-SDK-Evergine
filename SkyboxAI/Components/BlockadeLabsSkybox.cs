using BlockadeLabsSDK;
using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics.Effects;
using Evergine.Framework.Graphics.Materials;
using Evergine.Framework.Services;
using Evergine.Platform;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SkyboxAI.Components
{
    public class BlockadeLabsSkybox : Component
    {
        public event EventHandler<int> OnPercentageChanged;

        public string Prompt
        {
            get => this.prompt;
            set => this.prompt = this.ValidateFilename(value);
        }

        public int Percentage
        {
            get => this.percentageCompleted;
            set
            {
                this.percentageCompleted = value;
                this.OnPercentageChanged?.Invoke(this, this.percentageCompleted);
            }
        }

        public string apiKey;

        [BindComponent(true, true, BindComponentSource.Scene, "Skybox")]
        public MaterialComponent MaterialComponent;

        [BindService]
        private AssetsService assetsService;

        [BindService]
        private AssetsDirectory assetsDirectory;

        private GraphicsContext graphicsContext;
        private Effect skyboxEffect;
        private RenderLayerDescription skyboxLayer;
        private SamplerState skyboxSampler;
        private string prompt;
        private List<SkyboxStyleField> skyboxStyleFields;
        private List<SkyboxStyle> skyboxStyles;
        public string[] skyboxStyleOptions;
        public string skyboxStyleSelected;

        private string imagineObfuscatedId = string.Empty;
        public int percentageCompleted = -1;
        private bool isCancelled;
        private SkyboxMaterial skyboxMat;

        protected override bool OnAttached()
        {
            var result = base.OnAttached();

            this.graphicsContext = Application.Current.Container.Resolve<GraphicsContext>();
            this.skyboxEffect = this.assetsService.Load<Effect>(DefaultResourcesIDs.SkyboxEffectId);
            this.skyboxLayer = this.assetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.SkyboxRenderLayerID);
            this.skyboxSampler = this.assetsService.Load<SamplerState>(DefaultResourcesIDs.LinearClampSamplerID);

            return result;
        }

        public async Task GetSkyboxStyleOptions()
        {
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("api.blockadelabs.com"))
            {
                Debug.WriteLine("You need to provide an API Key in API options. Get one at api.blockadelabs.com");
                throw new Exception("You need to provide an API Key in API options. Get one at api.blockadelabs.com");
            }

            this.skyboxStyles = await ApiRequests.GetSkyboxStyles(apiKey);

            if (skyboxStyles == null)
            {
                Debug.WriteLine("Something went wrong. Please recheck you API key.");
                throw new Exception("Something went wrong. Please recheck you API key.");
            }

            this.skyboxStyleOptions = skyboxStyles.Select(s => s.name).ToArray();

            this.GetSkyboxStyleFields();
        }

        private void GetSkyboxStyleFields()
        {
            this.skyboxStyleFields = new List<SkyboxStyleField>();

            // add the default fields
            skyboxStyleFields.AddRange(new List<SkyboxStyleField>
            {
                new SkyboxStyleField(
                    new UserInput(
                        "prompt",
                        1,
                        "Prompt",
                        "",
                        "textarea"
                    )
                ),
                new SkyboxStyleField(
                    new UserInput(
                        "negative_text",
                        2,
                        "Negative text",
                        "",
                        "text"
                    )
                ),
                new SkyboxStyleField(
                    new UserInput(
                        "seed",
                        3,
                        "Seed",
                        "0",
                        "text"
                    )
                ),
                new SkyboxStyleField(
                    new UserInput(
                        "enhance_prompt",
                        4,
                        "Enhance prompt",
                        "false",
                        "boolean"
                    )
                ),
            });
        }

        public async void GenerateSkybox()
        {
            Debug.WriteLine("Generating a Skybox ...");

            if (this.percentageCompleted >= 0 && this.percentageCompleted < 100) return;

            // set prompt
            var prompt = this.skyboxStyleFields.First(
                skyboxStyleField => skyboxStyleField.key == "prompt"
            );

            prompt.value = this.prompt;

            SkyboxStyle style = this.skyboxStyles[0];
            if (!string.IsNullOrEmpty(this.skyboxStyleSelected))
            {
                style = this.skyboxStyles.First(
                    skyboxStyle => skyboxStyle.name == this.skyboxStyleSelected
                    );
            }

            await this.CreateSkybox(this.skyboxStyleFields, style.id);
        }

        public async Task CreateSkybox(List<SkyboxStyleField> skyboxStyleFields, int id)
        {
            this.isCancelled = false;
            this.Percentage = 1;

            var createSkyboxObfuscatedId = await ApiRequests.CreateSkybox(skyboxStyleFields, id, apiKey);
            await Task.Delay(0);
            ////var createSkyboxObfuscatedId = "caffa49480105a7a12616d3023bb31b1";
            ////var createSkyboxObfuscatedId = "1e4ce7151c692b9379c9e308fe592ddc";
            ////var createSkyboxObfuscatedId = "e4ca6941be9568ad852c28eaa40d868b";

            Debug.WriteLine($"Skybox id:{createSkyboxObfuscatedId}");

            await this.InitializeGetAssets(createSkyboxObfuscatedId);
        }

        private async Task InitializeGetAssets(string createImagineObfuscatedId)
        {
            if (createImagineObfuscatedId != string.Empty)
            {
                this.imagineObfuscatedId = createImagineObfuscatedId;
                this.Percentage = 33;

                string textureUrl = string.Empty;
                string depthMapUrl = string.Empty;
                string prompt = string.Empty;
                int count = 0;

                while (!isCancelled)
                {
                    await Task.Delay(1000);

                    if (isCancelled) break;

                    count++;

                    var getImagineResult = await ApiRequests.GetImagine(imagineObfuscatedId, apiKey);

                    if (getImagineResult.Count > 0)
                    {
                        this.Percentage = 66;
                        textureUrl = getImagineResult["textureUrl"];
                        depthMapUrl = getImagineResult["depthMapUrl"];
                        prompt = getImagineResult["prompt"];
                        break;
                    }
                }

                if (isCancelled)
                {
                    this.Percentage = -1;
                    this.imagineObfuscatedId = string.Empty;
                    return;
                }

                if (!string.IsNullOrWhiteSpace(textureUrl))
                {
                    var data = await ApiRequests.GetImagineImage(textureUrl);

                    this.CreateTexture(data, createImagineObfuscatedId);
                }

                this.Percentage = 100;

                Debug.WriteLine("Skybox generated. OK!");
            }
        }

        private void CreateTexture(byte[] data, string createImagineObfuscatedId)
        {
            // Create texture
            Debug.WriteLine("Creating texture...");
            Texture texture2D = null;
            using (var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data))
            {
                this.SaveSkybox(image, createImagineObfuscatedId);
                RawImageLoader.CopyImageToArrayPool(image, out _, out byte[] rgbaData);
                var description = new TextureDescription()
                {
                    Type = TextureType.Texture2D,
                    Width = (uint)image.Width,
                    Height = (uint)image.Height,
                    Depth = 1,
                    ArraySize = 1,
                    Faces = 1,
                    Usage = ResourceUsage.Default,
                    CpuAccess = ResourceCpuAccess.None,
                    Flags = TextureFlags.ShaderResource,
                    Format = PixelFormat.R8G8B8A8_UNorm,
                    MipLevels = 1,
                    SampleCount = TextureSampleCount.None,
                };

                texture2D = graphicsContext.Factory.CreateTexture(ref description);
                this.graphicsContext.UpdateTextureData(texture2D, rgbaData);
            }

            if (texture2D == null) return;

            // Create Material
            Debug.WriteLine("Creating Material...");
            if (this.skyboxMat == null)
            {
                this.skyboxMat = new SkyboxMaterial(skyboxEffect)
                {
                    TextureSampler = skyboxSampler,
                    LayerDescription = skyboxLayer,
                };
            }

            skyboxMat.Texture = texture2D;

            // Asigned to Enviroment
            Debug.WriteLine("Asigned Material to environment");
            this.MaterialComponent.Material = this.skyboxMat.Material;
        }

        private string ValidateFilename(string prompt)
        {
            if (string.IsNullOrEmpty(prompt)) return string.Empty;

            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                prompt = prompt.Replace(c, '_');
            }

            while (prompt.Contains("__"))
            {
                prompt = prompt.Replace("__", "_");
            }

            return prompt.TrimStart('_').TrimEnd('_');
        }

        private void SaveSkybox(Image image, string createImagineObfuscatedId)
        {
            var root = this.assetsDirectory.RootPath;
            var baseDir = this.TryGetSolutionDirectoryInfo(root);
            var enviromentDir = $"{baseDir}/Content/Enviroments/";

            if (!Directory.Exists(enviromentDir))
            {
                Directory.CreateDirectory(enviromentDir);
            }

            var path = $"{enviromentDir}{createImagineObfuscatedId}.jpg";
            image.SaveAsJpeg(path);
            Debug.WriteLine($"Saved image in {path}");
        }

        private DirectoryInfo TryGetSolutionDirectoryInfo(string currentPath = null)
        {
            var directory = new DirectoryInfo(
                currentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }
            return directory;
        }
    }
}
