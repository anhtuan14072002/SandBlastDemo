using System;
using UnityEditor.Overlays;
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
                .AsSingle();
            Container.Bind<RewardSystem>()
                .AsSingle()
                .NonLazy();
            Container.Bind<ScoreData>()
                .AsSingle()
                .NonLazy();
            Container.Bind<LevelModClassicData>()
                .AsSingle()
                .NonLazy();
            Container.Bind<SaveMapData>()
                .AsSingle()
                .NonLazy();
        }

       private void LoadFromES3(UserData userData)
        {
            LoadIntValue(SaveKeys.HighScore, value => userData.HighScore.Value = value);
            LoadIntValue(SaveKeys.Gems, value => userData.Gems.Value = value);
            LoadIntValue(SaveKeys.MagicBrush, value => userData.MagicBrush.Value = value);
            LoadIntValue(SaveKeys.Boom, value => userData.Boom.Value = value);
            LoadIntValue(SaveKeys.LevelModClassic, value => userData.LevelModClassic.Value = value);
            LoadIntValue(SaveKeys.CurrentScore, value => userData.CurrentScore.Value = value);
        }

        private void LoadIntValue(string key, Action<int> setValue)
        {
            if (!ES3.KeyExists(key)) return;
            try
            {
                setValue(ES3.Load<int>(key));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Error loading {key}: {ex.Message}. Using default value 0.");
                // setValue(0);
            }
        }
    }
}