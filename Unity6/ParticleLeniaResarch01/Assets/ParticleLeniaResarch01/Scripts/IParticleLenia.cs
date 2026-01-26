using Assets.ParticleLeniaResarch01.Analyzers;
using Assets.ParticleLeniaResarch01.Scripts.Data;
using System;

namespace Assets.ParticleLeniaResarch01.Scripts
{
    /// <summary>
    /// ParticleLeniaのシミュレーションを一般化するインターフェース
    /// </summary>
    public interface IParticleLenia
    {
        /// <summary>
        /// シミュレーション詳細の説明
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 現在の時間
        /// </summary>
        double CurrentTime { get; }

        /// <summary>
        /// 現在のステップ
        /// </summary>
        int CurrentStep { get; }

        /// <summary>
        /// 粒子の初期化方法
        /// </summary>
        IParticleLeniaInitializer Initializer { get; set; }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="postAction"></param>
        ParticleInfoArray Init(ParticleLeniaAnalyzerForeachEvent forEachCreatedParticle);

        /// <summary>
        /// ステップの更新
        /// </summary>
        ParticleInfoArray Next(ParticleLeniaAnalyzerForeachEvent forEachUpdatedParticle);

        /// <summary>
        /// 粒子の削除
        /// </summary>
        /// <param name="pointIndexs"></param>
        void RemovePoints(ReadOnlySpan<int> pointIndexs);

        /// <summary>
        /// 現在の粒子状態
        /// </summary>
        /// <returns></returns>
        ParticleInfoArray GetLastResult();
    }
}