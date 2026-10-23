using System;

namespace WitchShmup.Endless
{
    /// <summary>Small adapter contract for bosses added to future endless-mode scenarios.</summary>
    public interface IInfiniteModeBoss
    {
        event Action InfiniteBossDefeated;
        void StartInfiniteFight();
    }
}
