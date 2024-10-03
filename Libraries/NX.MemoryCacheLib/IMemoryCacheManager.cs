namespace NX.MemoryCacheLib
{
    public interface IMemoryCacheManager<TValue>
    {
        void Add(string key, TValue value);
        void AddRange(string key, params TValue[] values);
        IEnumerable<TValue>? GetList(string key);
        void Update(string key, Func<TValue, bool> filter, Action<TValue> updateAction);
        void Remove(string key, Func<TValue, bool> filter);
        IEnumerable<TValue>? SafeDispose(string key);
        void Dispose(string key);
    }
}
