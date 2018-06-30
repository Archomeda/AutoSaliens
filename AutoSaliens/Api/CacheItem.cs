using System;

namespace AutoSaliens.Api
{
    internal class CacheItem<T>
    {
        public CacheItem() { }

        public CacheItem(T item, TimeSpan expiryTime)
        {
            this.Item = item;
            this.Expires = DateTime.Now + expiryTime;
        }

        public DateTime Expires { get; }

        public T Item { get; set; }
    }
}
