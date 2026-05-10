using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Sistemskoprvideo
{
    internal class RequestQueue
    {
        private Queue<HttpListenerContext> queue = new Queue<HttpListenerContext>();
        private object locker = new object();
        private bool stopped = false;

        public void Enqueue(HttpListenerContext context)
        {
            lock (locker)
            {
                queue.Enqueue(context);

                // Budi jednu worker nit koja čeka na novi zahtev.
                Monitor.Pulse(locker);
            }
        }

        public HttpListenerContext? Dequeue()
        {
            lock (locker)
            {
                while (queue.Count == 0 && !stopped)
                {
                    // Ako nema zahteva, worker nit se blokira i čeka.
                    Monitor.Wait(locker);
                }

                if (stopped)
                {
                    return null;
                }

                return queue.Dequeue();
            }
        }

        public void Stop()
        {
            lock (locker)
            {
                stopped = true;

                // Budi sve worker niti da mogu pravilno da se završe.
                Monitor.PulseAll(locker);
            }
        }
    }
}