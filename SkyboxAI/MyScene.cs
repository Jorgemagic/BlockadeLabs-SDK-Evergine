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

        protected override void CreateScene()
        {
        }
    }
}


