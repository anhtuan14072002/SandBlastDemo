using Zenject;

namespace Sand
{
    public class UserDataInstall : MonoInstaller
    {
        public override void InstallBindings()
        {
            var userData = new UserData();
            LoadFromES3(userData);
            
            Container.Bind<UserData>()
                .FromInstance(userData)
                .AsSingle()
                .NonLazy();
            Container.Bind<SaveService>()
                .AsSingle()
                .NonLazy();
            Container.Bind<RewardSystem>()
                .AsSingle()
                .NonLazy();

        }

        private void LoadFromES3(UserData userData)
        {
            if (ES3.KeyExists(SaveKeys.HighScore))
                userData.HighScore.Value = ES3.Load<int>(SaveKeys.HighScore);
            if (ES3.KeyExists(SaveKeys.Gems))
                userData.Gems.Value = ES3.Load<double>(SaveKeys.Gems);
            
        }
    }
}