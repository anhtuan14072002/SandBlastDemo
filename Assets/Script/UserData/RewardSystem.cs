using Zenject;

namespace Sand
{
    public class RewardSystem
    {
        [Inject] private UserData _userData;
        [Inject] private SaveService _saveService;
        
        public void AddGems(double gem)
        {
            _userData.Gems.Value += gem;
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
    }
}