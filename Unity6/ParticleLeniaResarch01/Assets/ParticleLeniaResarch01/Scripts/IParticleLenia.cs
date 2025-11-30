using System;

namespace Assets.ParticleLeniaResarch01.Scripts.Data
{
    /// <summary>
    /// ParticleLeniaのシミュレーションを一般化するインターフェース
    /// </summary>
    public interface IParticleLenia
    {
        public delegate void PostAction(ParticleInfo newInfo, int index);

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
        void Init(PostAction postAction);

        /// <summary>
        /// ステップの更新
        /// </summary>
        /// <param name="step"></param>
        void Next(int step, PostAction intermediatePostAction, PostAction lastPostAction);

        /// <summary>
        /// 現在の粒子状態
        /// </summary>
        /// <returns></returns>
        ParticleInfoArray GetResult();
    }
}