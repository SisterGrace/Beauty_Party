using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
namespace Sky
{
    public class SkyConfigMgr : SkySingleton <SkyConfigMgr>
    {

        public SkyConfigData gameConfig;

        protected override void OnInstanceCreate()
        {
            gameConfig = Resources.Load<SkyConfigData>("SkyConfigData");
            if (gameConfig == null) return;
            if (gameConfig.maxLocalConfig == null)
                gameConfig.maxLocalConfig = new CoreMaxLocalConfig();
           
        }
        public string GetGA_gameKey()
        {
            return gameConfig?.GetGA_gameKey() ?? "";
        }
        public string GetGA_secretKey()
        {
            return gameConfig?.GetGA_secretKey() ?? "";
        }

    }
}