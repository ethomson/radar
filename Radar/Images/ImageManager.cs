using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

using Radar.Util;

namespace Radar.Images
{
    public class ImageManager
    {
        private const int sleepTime = 1000;

        private Object runningLock = new Object();
        private bool running;

        private List<QueuedRequest> requestQueue = new List<QueuedRequest>();

        private class QueuedRequest
        {
            public QueuedRequest(Uri uri, string filename, NetworkCredential credential = null)
            {
                Assert.NotNull(uri, "uri");
                Assert.NotNull(filename, "filename");

                Uri = uri;
                Filename = filename;
                Credential = credential;
            }

            public Uri Uri { get; private set; }
            public string Filename { get; private set; }
            public NetworkCredential Credential { get; private set; }
        }

        public ImageManager(ImageManagerConfiguration configuration)
        {
            Configuration = configuration;
        }

        public ImageManagerConfiguration Configuration
        {
            get;
            private set;
        }

        public bool Running
        {
            get
            {
                lock (runningLock)
                {
                    return running;
                }
            }

            private set
            {
                lock (runningLock)
                {
                    running = value;
                }
            }
        }

        public string GetImage(Uri imageUrl, NetworkCredential credential = null)
        {
            string filename = GetImagePath(imageUrl);

            if (!File.Exists(filename))
            {
                Enqueue(new QueuedRequest(imageUrl, filename, credential));
            }

            Thread.Sleep(sleepTime * 2);

            return filename;
        }

        private string GetImagePath(Uri imageUrl)
        {
            char[] hexChars = new char[]
            {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'
            };

            StringBuilder hex = new StringBuilder();
            SHA1 sha = new SHA1CryptoServiceProvider();

            byte[] hash = sha.ComputeHash(Encoding.ASCII.GetBytes(imageUrl.ToString()));

            for (int i = 0; i < hash.Length; i++)
            {
                hex.Append(hexChars[(hash[i] & 0xf0) >> 4]);
                hex.Append(hexChars[(hash[i] & 0x0f)]);
            }

            return Path.Combine(Configuration.ImageCacheDir, hex.ToString());
        }

        private void Enqueue(QueuedRequest request)
        {
            lock (requestQueue)
            {
                requestQueue.Add(request);
            }
        }

        private QueuedRequest Dequeue()
        {
            QueuedRequest request = null;

            lock (requestQueue)
            {
                if (requestQueue.Count > 0)
                {
                    request = requestQueue[0];
                    requestQueue.RemoveAt(0);
                }
            }

            return request;
        }

        private void HandleRequest(QueuedRequest queuedRequest, NetworkCredential credential = null)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(queuedRequest.Uri);

            if (queuedRequest.Credential != null)
            {
                request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(String.Format("{0}:{1}",
                    queuedRequest.Credential.UserName, queuedRequest.Credential.Password))));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(queuedRequest.Filename));

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream responseStream = response.GetResponseStream())
            using (Stream outputStream = File.Open(queuedRequest.Filename, FileMode.OpenOrCreate, FileAccess.Write))
            {
                byte[] buf = new byte[4096];
                int readlen;

                while ((readlen = responseStream.Read(buf, 0, 4096)) > 0)
                {
                    outputStream.Write(buf, 0, readlen);
                }

                outputStream.Close();
                responseStream.Close();
                response.Close();
            }
        }

        public void Start()
        {
            Running = true;

            while (Running)
            {
                QueuedRequest queuedRequest;

                while ((queuedRequest = Dequeue()) != null)
                {
                    HandleRequest(queuedRequest);
                }

                Thread.Sleep(sleepTime);
            }
        }

        public void Stop()
        {
            Running = false;
        }
    }
}