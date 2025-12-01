using System;

namespace HadesSDK.Ads.Core
{
    public interface IAOAProvider
    {
        public void LoadAOA();
        public void ShowAOA();

        public bool IsAOAReady();

        public Action onAOADisplay { get; }
        
    }
}