using System;
using System.Net;
using System.ComponentModel;
using System.Threading;

namespace Raviheppaohjelma {
    class Program {
        static void Main(string[] args) {

            DownloadRace downloadRace = new DownloadRace();

            downloadRace.DownloadPage("https://ravit.is.fi/tulokset/166059");

            while (!downloadRace.DownloadCompleted) {
                Thread.Sleep(1000);
            }

            Analyze(downloadRace._result);
        }

        static void Analyze(string result) {
            int maxHorses = 20;
            string[] separators = new string[20];
            string[] stringArr;
            string[] firstArr;
            string[] secondArr;
            int[] hevoset;

            for (int i = 0; i < maxHorses; i++) {
                separators[i] = "<td>" +i +".</td>";
            }

            stringArr = result.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            hevoset = new int[stringArr.Length - 1];

            for (int i = 1; i < stringArr.Length; i++) {
                firstArr = stringArr[i].Split("hevoset/");
                secondArr = firstArr[1].Split("/");
                hevoset[i-1] = Int32.Parse(secondArr[0]);
            }

            foreach(var horse in hevoset) {
                Console.WriteLine(horse);
            }
        }
    }
    class DownloadRace {
        private volatile bool _completed;
        public string _result;

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

        private void Completed(object sender, DownloadStringCompletedEventArgs e) {
            if (e.Cancelled == true) {
                Console.WriteLine("Download has been canceled.");
            }
            else {
                Console.WriteLine("Download completed!");
            }

            _completed = true;

            _result = (string)e.Result;

            //Console.WriteLine(_result);
        }
    }
}
