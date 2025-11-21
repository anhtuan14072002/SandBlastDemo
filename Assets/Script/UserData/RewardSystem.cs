using Zenject;

namespace Sand
{
    public class RewardSystem
    {
        [Inject] UserData _userData;
        [Inject] SaveService _saveService;
        
        public void AddGems(int gem)
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