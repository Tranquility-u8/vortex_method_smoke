using System;

public interface IParticleData<W> where W : unmanaged
{
    public abstract int WordCount { get; }
    public abstract void ToWords(Span<W> dst);
}
