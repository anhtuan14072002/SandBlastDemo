using System;
using System.Collections.Generic;
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
            Container.Bind<PictureDrawData>()
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

            LoadListIntValue(SaveKeys.CompletedPictureIndices,
                value => userData.CompletedPictureIndices = value);

            LoadDictionaryIntIntValue(SaveKeys.PictureFillProgress,
                value => userData.PictureFillProgress = value);
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
            }
        }

        private void LoadListIntValue(string key, Action<List<int>> setValue)
        {
            if (!ES3.KeyExists(key)) return;
            try
            {
                setValue(ES3.Load<List<int>>(key));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Error loading {key}: {ex.Message}.");
            }
        }

        private void LoadDictionaryIntIntValue(string key, Action<Dictionary<int,int>> setValue)
        {
            if (!ES3.KeyExists(key)) return;
            try
            {
                setValue(ES3.Load<Dictionary<int,int>>(key));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Error loading {key}: {ex.Message}.");
            }
        }
    }
}
