using Zenject;

namespace Sand
{
    public class LevelModClassicSystem
    {
        UserData _userData;
        SaveService _saveService;

        [Inject]
        public LevelModClassicSystem(UserData userData, SaveService saveService)
        {
            _userData = userData;
            _saveService = saveService;
        }
        
        public void IncreaseLevelModClassic()
        {
            _userData.LevelModClassic.Value++;
            _saveService.Save();
        }
        
        public void ResetLevelModClassic()
        {
            _userData.LevelModClassic.Value = 0;
            _saveService.Save();
        }
    }
}