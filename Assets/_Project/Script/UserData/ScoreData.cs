using HadesSDK.Ads.Core;

namespace Sand
{
    public class ScoreData
    {
        UserData _userData;

        public ScoreData(UserData userData)
        {
            _userData = userData;
        }

        public void CheckHighScore(int score)
        {
            if (score <= _userData.HighScoreValue) return;
            _userData.HighScore.Value = score;
            FirebaseService.Instance.LogEvent("high_score", new EventParameter("high_score", "{" + score + "}"));
        }

        public void ResetCurrentScore()
        {
            _userData.CurrentScore.Value = 0;
        }
    }
}