namespace Assets.ParticleLeniaResarch01.Scripts.MathObjects
{
    /// <summary>
    /// 単一重み付き総和の形状を決定する構造体
    /// </summary>
    public readonly struct Weighted<T>
    {
        public readonly double w;
        public readonly T[] array;

        public Weighted(double w, T[] array)
        {
            this.w = w;
            this.array = array;
        }
    }
}