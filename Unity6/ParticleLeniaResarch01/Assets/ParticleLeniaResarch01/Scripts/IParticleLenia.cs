using UnityEngine.Rendering;

namespace Assets.ParticleLeniaResarch01.Scripts.Data
{
    public class ParticleLeniaRecorder
    {
    }

    /// <summary>
    /// ParticleLeniaのシミュレーションを一般化するインターフェース
    /// </summary>
    public interface IParticleLenia
    {
        /// <summary>
        /// シミュレーション詳細の説明
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// 現在の時間
        /// </summary>
        public double CurrentTime { get; }

        /// <summary>
        /// 現在のステップ
        /// </summary>
        public int CurrentStep { get; }

        /// <summary>
        /// 粒子の初期化方法
        /// </summary>
        public IParticleLeniaInitializer Initializer { get; set; }

        /// <summary>
        /// ステップの更新
        /// </summary>
        /// <param name="step"></param>
        public void Next(int step);

        /// <summary>
        /// 現在の粒子状態
        /// </summary>
        /// <returns></returns>
        public ParticleInfoArray GetResult();
    }
}