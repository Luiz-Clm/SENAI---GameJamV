using UnityEngine;

namespace WitchShmup.Endless
{
    /// <summary>
    /// Data for one endless-mode scenario. Put assets under Resources/InfiniteMode;
    /// the wave manager selects the pack with the greatest startingWave not above the current wave.
    /// </summary>
    [CreateAssetMenu(menuName = "SpellGlide/Modo Infinito/Content Pack", fileName = "InfiniteContent")]
    public class InfiniteModeContentPack : ScriptableObject
    {
        [Tooltip("Wave where this set of enemies and scenery begins.")]
        public int startingWave = 1;

        [Tooltip("Enemy prefab choices for this section. Repeating a prefab gives it extra weight.")]
        public GameObject[] enemyPrefabs;

        [Tooltip("Optional boss prefab for this scenario. It must implement IInfiniteModeBoss.")]
        public GameObject bossPrefab;

        [Tooltip("Optional looping background prefab used when this scenario begins.")]
        public GameObject backgroundPrefab;
    }
}
