using System;

namespace GPUSmoke
{
    public interface IParticle<W> where W : unmanaged
    {
        public abstract int WordCount { get; }
        public abstract void ToWords(Span<W> dst);
    }

}