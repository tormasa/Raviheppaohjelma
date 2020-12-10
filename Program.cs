using System;
using System.Net;
using System.ComponentModel;
using System.Threading;

namespace Raviheppaohjelma {
    class Program {
        static void Main(string[] args) {

            DownloadRace downloadRace = new DownloadRace();

            downloadRace.DownloadPage("https://ravit.is.fi/tulokset/166059");

            while (!downloadRace.DownloadCompleted)
                Thread.Sleep(1000);
        }
    }
    class DownloadRace {
        private volatile bool _completed;

        public void DownloadPage(string address) {
            WebClient client = new WebClient();
            Uri uri = new Uri(address);
            _completed = false;

            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(Completed);

            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgress);
            client.DownloadStringAsync(uri);
        }

        public bool DownloadCompleted { get { return _completed; } }

        private void DownloadProgress(object sender, DownloadProgressChangedEventArgs e) {
            Console.WriteLine("{0}    downloaded {1} of {2} bytes. {3} % complete...",
                (string)e.UserState,
                e.BytesReceived,
                e.TotalBytesToReceive,
                e.ProgressPercentage);
        }

        private void Completed(object sender, AsyncCompletedEventArgs e) {
            if (e.Cancelled == true) {
                Console.WriteLine("Download has been canceled.");
            }
            else {
                Console.WriteLine("Download completed!");
            }

            _completed = true;
        }
    }
}
