using System;
using System.IO;
using System.Net;
using System.Threading;

namespace Sistemskoprvideo
{
    internal class ImageServer
    {
        private HttpListener listener;
        private RequestQueue requestQueue;
        private ImageCache imageCache;
        private ImageProcessor imageProcessor;
        private Logger logger;

        private Thread? listenerThread;
        private Thread[] workers;

        private bool running = false;

        private int port;
        private int workerCount;

        private string rootFolder = "root";

        public ImageServer(int port, int workerCount, int cacheExpirationSeconds)
        {
            this.port = port;
            this.workerCount = workerCount;

            listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");

            requestQueue = new RequestQueue();
            logger = new Logger();
            imageCache = new ImageCache(cacheExpirationSeconds, logger);
            imageProcessor = new ImageProcessor();

            workers = new Thread[workerCount];
        }

        public void Start()
        {
            if (!Directory.Exists(rootFolder))
            {
                Directory.CreateDirectory(rootFolder);
            }

            running = true;

            listener.Start();

            logger.Log($"Server je pokrenut na adresi: http://localhost:{port}/");
            logger.Log($"Broj worker niti: {workerCount}");
            logger.Log($"Cache ističe nakon 60 sekundi.");

            listenerThread = new Thread(ListenLoop);
            listenerThread.Name = "Listener nit";
            listenerThread.Start();

            for (int i = 0; i < workerCount; i++)
            {
                workers[i] = new Thread(WorkerLoop);
                workers[i].Name = "Worker " + (i + 1);
                workers[i].Start();
            }
        }

        public void Stop()
        {
            running = false;

            requestQueue.Stop();

            if (listener.IsListening)
            {
                listener.Stop();
            }

            logger.Log("Server je zaustavljen.");
        }

        private void ListenLoop()
        {
            while (running)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();

                    logger.Log("Primljen zahtev: " + context.Request.RawUrl);

                    requestQueue.Enqueue(context);
                }
                catch
                {
                    if (running)
                    {
                        logger.Log("Greška prilikom prijema zahteva.");
                    }
                }
            }
        }

        private void WorkerLoop()
        {
            while (running)
            {
                HttpListenerContext? context = requestQueue.Dequeue();

                if (context == null)
                {
                    break;
                }

                try
                {
                    ProcessRequest(context);
                }
                catch (Exception ex)
                {
                    logger.Log("Greška u worker niti: " + ex.Message);

                    try
                    {
                        SendTextResponse(context, 500, "500 Internal Server Error");
                    }
                    catch
                    {
                        logger.Log("Greška prilikom slanja 500 odgovora.");
                    }
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            string imageName = context.Request.Url?.AbsolutePath.TrimStart('/') ?? "";

            imageName = Uri.UnescapeDataString(imageName);

            if (string.IsNullOrWhiteSpace(imageName))
            {
                SendTextResponse(context, 400, "400 Bad Request - unesite naziv slike.");
                return;
            }

            // Zaštita: uzimamo samo ime fajla, ne dozvoljavamo putanje tipa ../
            imageName = Path.GetFileName(imageName);

            string imagePath = Path.Combine(rootFolder, imageName);

            if (!File.Exists(imagePath))
            {
                logger.Log("Slika nije pronađena: " + imageName);
                SendTextResponse(context, 404, "404 Not Found - slika ne postoji.");
                return;
            }

            logger.Log(Thread.CurrentThread.Name + " obrađuje sliku: " + imageName);

            byte[] result = imageCache.GetOrCreate(imageName, () =>
            {
                logger.Log("Cache miss - konverzija slike: " + imageName);
                return imageProcessor.ConvertToGrayscale(imagePath);
            });

            
            SendImageResponse(context, result, imageName);
            logger.Log("Odgovor poslat za sliku: " + imageName);
        }


        private void SendImageResponse(HttpListenerContext context, byte[] imageBytes, string imageName)
        {
            string extension = Path.GetExtension(imageName).ToLower();

            context.Response.StatusCode = 200;

            if (extension == ".png")
            {
                context.Response.ContentType = "image/png";
            }
            else
            {
                context.Response.ContentType = "image/jpeg";
            }

            context.Response.ContentLength64 = imageBytes.Length;

            context.Response.OutputStream.Write(imageBytes, 0, imageBytes.Length);
            context.Response.OutputStream.Close();
        }

        private void SendTextResponse(HttpListenerContext context, int statusCode, string message)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;

            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
    }
}