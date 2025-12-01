namespace Sand
{
    public class ScoreData
    {
        UserData _userData;
        SaveService _saveService;

        public ScoreData(UserData userData, SaveService saveService)
        {
            _userData = userData;
            _saveService = saveService;
        }

        public void CheckHighScore(int score)
        {
            if (score <= _userData.HighScoreValue) return;
            _userData.HighScore.Value = score;
            // _saveService.Save();
        }

        public void ResetCurrentScore()
        {
            _userData.CurrentScore.Value = 0;
            // _saveService.Save();
        }
    }
}