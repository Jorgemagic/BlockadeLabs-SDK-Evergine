using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using System.Diagnostics;
using System;
using BlockadeLabsSDK;
using System.Collections.Generic;
using System.Linq;
using SkyboxAI.Components;

namespace SkyboxAI
{
    public class MyScene : Scene
    {
        public override void RegisterManagers()
        {
            base.RegisterManagers();
            
            this.Managers.AddManager(new global::Evergine.Bullet.BulletPhysicManager3D());            
        }

        protected override async void CreateScene()
        {
            Entity BlockadeLabsAI = new Entity()
                .AddComponent(new BlockadeLabsSkybox()
                {
                    apiKey = "7SFtmkXQbBVN5CWm4Ib2y0cD32W5uE310QaUqRMsvsdCx38VmTPAxJO2pEqz",
                    Prompt = "White house",
                });

            this.Managers.EntityManager.Add(BlockadeLabsAI);

            var c = BlockadeLabsAI.FindComponent<BlockadeLabsSkybox>();
            await c.GetSkyboxStyleOptions();
            c.skyboxStyleSelected = "Realism";
            c.GenerateSkybox();
        }
    }
}


