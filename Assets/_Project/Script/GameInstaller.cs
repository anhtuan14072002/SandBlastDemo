using HadesSDK.Ads.Core;
using Zenject;

namespace Sand
{
    public class GameInstaller : MonoInstaller
    {
        
        public override void InstallBindings()
        {
            Container.Bind<EffectBlock>()
                .FromComponentInHierarchy()
                .AsSingle();
            
            Container.Bind<ScoreView>()
                .FromComponentInHierarchy()
                .AsSingle();
            
            Container.Bind<GameResources>()
                .FromComponentInHierarchy()
                .AsSingle();
            
            Container.Bind<GameVisual>()
                .FromComponentInHierarchy()
                .AsSingle();
            
            Container.Bind<GameRevive>()
                .FromComponentInHierarchy()
                .AsSingle();
            
            Container.Bind<RenderMap>()
                .FromComponentInHierarchy()
                .AsSingle();
            
            Container.Bind<CheckLevelScore>()
                .FromComponentInHierarchy()
                .AsSingle();
            
            Container.Bind<BlockSpawn>()
                .FromComponentInHierarchy()
                .AsSingle();
            
            Container.Bind<BlockManager>()
                .FromComponentInHierarchy()
                .AsSingle();
            
            Container.Bind<PowerUpSystem>()
                .FromComponentInHierarchy()
                .AsSingle();
            
            Container.Bind<SoundManager>()
                .FromComponentInHierarchy()
                .AsSingle();
            
            Container.Bind<VibrationManager>()
                .FromComponentInHierarchy()
                .AsSingle();
            
            Container.Bind<RenderPicture>()
                .FromComponentInHierarchy()
                .AsSingle();
            
            Container.Bind<EffectGame>()
                .FromComponentInHierarchy()
                .AsSingle();
            
            Container.Bind<PopupAuction>()
                .FromComponentInHierarchy()
                .AsSingle();
        }
    }
}