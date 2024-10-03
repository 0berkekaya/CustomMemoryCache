using System.Collections.Concurrent;

namespace NX.MemoryCacheLib
{
    public class TSMemoryCacheManager<TValue> : IMemoryCacheManager<TValue>
    {
        private ConcurrentDictionary<string, ConcurrentBag<TValue>> _concurrentDict { get; set; }
        private ConcurrentDictionary<string, Type> _typeMap { get; set; }

        public TSMemoryCacheManager()
        {
            _concurrentDict = new ConcurrentDictionary<string, ConcurrentBag<TValue>>();
            _typeMap = new ConcurrentDictionary<string, Type>();
        }

        public void Add(string key, TValue value)
        {
            if (value == null)
                throw new InvalidOperationException("Value Null Olamaz !");

            Type classType = value.GetType();
            TypeMapControl(key, classType);
            ConcurrentDictHandler(key, value);
        }

        public void AddRange(string key, params TValue[] values)
        {
            // Values dizisinin null veya boş olup olmadığını kontrol et
            if (values == null || values.Length == 0)
                throw new InvalidOperationException("Values Null veya Boş Olamaz !");

            // İlk öğenin tipini al, bu noktada values dizisi boş olamaz
            Type? classType = values.First()?.GetType();
            if (classType != null)
            {

                // Tüm öğelerin aynı türde olup olmadığını kontrol et
                if (!values.All(value => value != null && value.GetType() == classType))
                    throw new InvalidOperationException("Tüm öğeler aynı türde ve null olmamalıdır!");

                // TypeMapControl kontrolünü yap
                TypeMapControl(key, classType);

                // Thread-safe işlemler için lock kullanıyoruz
                lock (_concurrentDict)
                {
                    foreach (var value in values)
                    {
                        // Her bir öğenin null olup olmadığını kontrol et
                        if (value == null)
                            throw new InvalidOperationException("Value Null Olamaz !");

                        // ConcurrentBag'e ekleme işlemi
                        ConcurrentDictHandler(key, value);
                    }
                }
            }
        }

        public IEnumerable<TValue>? GetList(string key)
        {
            if (_concurrentDict.TryGetValue(key, out var list))
                return new List<TValue>(list);
            return null;
        }

        public void Update(string key, TValue newValue, Func<TValue, bool> filter)
        {
            if (_concurrentDict.TryGetValue(key, out var values) && !values.IsEmpty)
            {
                lock (values)
                {
                    IEnumerable<TValue> updatedValues = values.Select(t =>
                    {
                        if (filter(t))
                        {
                            return newValue;
                        }
                        return t;
                    });
                    _concurrentDict[key] = new ConcurrentBag<TValue>(updatedValues);
                }
            }
        }

        public void Update(string key, Func<TValue, bool> filter, Action<TValue> updateAction)
        {
            if (_concurrentDict.TryGetValue(key, out var values) && !values.IsEmpty)
            {
                lock (values)
                {
                    IEnumerable<TValue> itemsToUpdate = values.Where(t => filter(t));
                    foreach (TValue? item in itemsToUpdate)
                    {
                        updateAction(item);
                    }
                }
            }
        }

        public void Remove(string key, Func<TValue, bool> filter)
        {
            if (_concurrentDict.TryGetValue(key, out var values) && !values.IsEmpty)
            {
                lock (values)
                {
                    IEnumerable<TValue> newList = values.Where(t => !filter(t));

                    if (newList.Any())
                        _concurrentDict[key] = new ConcurrentBag<TValue>(newList);
                    else
                        Cleaner(key);
                }
            }
        }

        public IEnumerable<TValue>? SafeDispose(string key)
        {
            if (_concurrentDict.TryRemove(key, out ConcurrentBag<TValue>? values))
            {
                _typeMap.TryRemove(key, out _);
                return values;
            }
            return null;
        }

        public void Dispose(string key)
        {
            if (_concurrentDict.TryRemove(key, out _))
                _typeMap.TryRemove(key, out _);
        }

        private void ConcurrentDictHandler(string key, TValue value)
        {
            _concurrentDict.AddOrUpdate(key,
                new ConcurrentBag<TValue> { value },
                (existingKey, existingBag) =>
                {
                    existingBag.Add(value);
                    return existingBag;
                });
        }

        private void TypeMapControl(string key, Type value)
        {
            _typeMap.AddOrUpdate(key,
                value,
                (existingKey, existingValue) =>
                {
                    if (existingValue != value)
                    {
                        throw new InvalidOperationException($"Anahtar '{key}' için '{value.Name}' türünden nesneler eklenemez !");
                    }
                    return existingValue;
                });
        }

        private void Cleaner(string key)
        {
            if (_concurrentDict.TryRemove(key, out _))
            {
                _typeMap.TryRemove(key, out _);
            }
        }
    }
}
