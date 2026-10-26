using UnityEngine;

namespace WitchShmup.Enemies
{
    /// <summary>Livro alado: patrulha a faixa seca e causa dano apenas por contato.</summary>
    public class WingedBookEnemy : EnemyBase
    {
        [Header("Voo do Livro")]
        [SerializeField] private float horizontalSpeed = 1.8f;
        [SerializeField] private float bobAmplitude = 0.3f;
        [SerializeField] private float bobFrequency = 2f;
        private float baseY;

        [Header("Animação do Livro")]
        [SerializeField] private Sprite[] flapFrames;
        [SerializeField] private float animationFps = 8f;
        [SerializeField] private WitchShmup.Utils.SimpleSpriteAnimator bookAnimator;

        protected override void Awake()
        {
            base.Awake();
            waterZone = EnemyWaterZone.SurfaceOnly;
            waterZoneMargin = Mathf.Max(waterZoneMargin, 0.25f);

            if (bookAnimator == null)
            {
                bookAnimator = GetComponent<WitchShmup.Utils.SimpleSpriteAnimator>();
                if (bookAnimator == null)
                {
                    bookAnimator = gameObject.AddComponent<WitchShmup.Utils.SimpleSpriteAnimator>();
                }
            }

            if (bookAnimator != null && flapFrames != null && flapFrames.Length > 0)
            {
                bookAnimator.SetFrames(flapFrames, animationFps);
            }
        }

        private void Start() => baseY = transform.position.y;

        protected override void Update()
        {
            base.Update();
            transform.position += Vector3.left * (horizontalSpeed * speedMultiplier * Time.deltaTime);
            float waterY = WitchShmup.Environment.PhaseTwoWaterSystem.Instance != null
                ? WitchShmup.Environment.PhaseTwoWaterSystem.Instance.SurfaceY + waterZoneMargin
                : baseY;
            float y = Mathf.Max(waterY, baseY + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude);
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
        }
    }
}
