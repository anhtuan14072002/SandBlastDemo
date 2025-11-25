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
        }
    }
}