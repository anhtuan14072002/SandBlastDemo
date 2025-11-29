using Zenject;

namespace Sand
{
    public class RewardSystem
    {
        UserData _userData;
        SaveService _saveService;

        [Inject]
        public RewardSystem(UserData userData, SaveService saveService)
        {
            _userData = userData;
            _saveService = saveService;
        }
        
        public void AddGems(int gem)
        {
            _userData.Gems.Value += gem;
            _saveService.Save();
        }

        public void DeductGems(int gem)
        {
            _userData.Gems.Value -= gem;
            _saveService.Save();
        }
        public void AddScore(int score)
        {
            if (score > _userData.HighScore.Value)
            {
                _userData.HighScore.Value = score;
                _saveService.Save();
            }
        }
        
        public void AddMagicBrush(int amount)
        {
            _userData.MagicBrush.Value += amount;
            _saveService.Save();
        }

        public void DeductMagicBrush(int amount)
        {
            _userData.MagicBrush.Value -= amount;
            _saveService.Save();
        }
        public void AddBoom(int amount)
        {
            _userData.Boom.Value += amount;
            _saveService.Save();
        }

        public void DeductBoom(int amount)
        {
            _userData.Boom.Value -= amount;
            _saveService.Save();
        }
    }
}