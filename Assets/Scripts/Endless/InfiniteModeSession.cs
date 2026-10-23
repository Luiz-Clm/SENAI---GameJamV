using UnityEngine;

namespace WitchShmup.Endless
{
    /// <summary>Runtime flags and preferences shared by the menu, gameplay and retry flow.</summary>
    public static class InfiniteModeSession
    {
        private const string AutoWavesKey = "WitchShmup_Infinite_AutoWaves";
        private const string BestWaveKey = "WitchShmup_Infinite_BestWave";

        public static bool IsActive { get; private set; }

        public static void Begin() => IsActive = true;
        public static void End() => IsActive = false;

        public static bool AutoStartNextWave
        {
            get => PlayerPrefs.GetInt(AutoWavesKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(AutoWavesKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static int BestWave => PlayerPrefs.GetInt(BestWaveKey, 0);

        public static int RecordWave(int wave)
        {
            int best = Mathf.Max(BestWave, wave);
            if (best != BestWave)
            {
                PlayerPrefs.SetInt(BestWaveKey, best);
                PlayerPrefs.Save();
            }
            return best;
        }

        public static void ResetBestWave()
        {
            PlayerPrefs.DeleteKey(BestWaveKey);
            PlayerPrefs.Save();
        }
    }
}
