using System;
using System.Collections.Generic;
using System.Threading;

namespace Sistemskoprvideo
{
    internal class ImageCache
    {
        private class CacheItem
        {
            public byte[] Data { get; set; } = Array.Empty<byte>();
            public DateTime ExpirationTime { get; set; }
        }

        private Dictionary<string, CacheItem> cache = new Dictionary<string, CacheItem>();

        // Ovde pamtimo koje slike se trenutno obrađuju.
        // Ovo rešava cache stampede problem.
        private HashSet<string> processingImages = new HashSet<string>();

        private object locker = new object();
        private int expirationSeconds;
        private Logger logger;

        public ImageCache(int expirationSeconds, Logger logger)
        {
            this.expirationSeconds = expirationSeconds;
            this.logger = logger;
        }

        public byte[] GetOrCreate(string imageName, Func<byte[]> createFunction)
        {
            lock (locker)
            {
                // Provera da li slika već postoji u cache-u.
                if (cache.ContainsKey(imageName))
                {
                    CacheItem item = cache[imageName];

                    if (DateTime.Now < item.ExpirationTime)
                    {
                        logger.Log("Cache hit - rezultat preuzet iz cache-a: " + imageName);
                        return item.Data;
                    }

                    logger.Log("Cache expired - rezultat je istekao: " + imageName);
                    cache.Remove(imageName);
                }

                // Ako neka druga nit već obrađuje istu sliku,
                // ova nit čeka da se obrada završi.
                while (processingImages.Contains(imageName))
                {
                    logger.Log("Cache stampede zaštita - nit čeka rezultat za: " + imageName);

                    Monitor.Wait(locker);

                    // Kada se nit probudi, ponovo proverava cache.
                    if (cache.ContainsKey(imageName))
                    {
                        CacheItem item = cache[imageName];

                        if (DateTime.Now < item.ExpirationTime)
                        {
                            logger.Log("Cache hit nakon čekanja - rezultat preuzet iz cache-a: " + imageName);
                            return item.Data;
                        }

                        logger.Log("Cache expired nakon čekanja: " + imageName);
                        cache.Remove(imageName);
                    }
                }

                // Ova nit sada postaje odgovorna za obradu slike.
                processingImages.Add(imageName);
            }

            byte[] result;

            try
            {
                // Obrada slike se radi VAN lock-a,
                // da druge niti ne bi bile nepotrebno blokirane.
                result = createFunction();
            }
            catch
            {
                lock (locker)
                {
                    processingImages.Remove(imageName);
                    Monitor.PulseAll(locker);
                }

                throw;
            }

            lock (locker)
            {
                cache[imageName] = new CacheItem
                {
                    Data = result,
                    ExpirationTime = DateTime.Now.AddSeconds(expirationSeconds)
                };

                logger.Log("Rezultat dodat u cache: " + imageName);

                processingImages.Remove(imageName);

                // Budimo sve niti koje čekaju rezultat ove slike.
                Monitor.PulseAll(locker);

                return result;
            }
        }
    }
}