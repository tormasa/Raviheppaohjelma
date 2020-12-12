using System;
using System.Net;
using System.ComponentModel;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace Raviheppaohjelma {
    class Program {
        static SqliteConnectionStringBuilder _connectionStringBuilder;
        static SqliteConnection _connection;

        static void Main(string[] args) {
            int kisaId = 166059;

            OpenDatabase();

            while(!(_connection.State == System.Data.ConnectionState.Open)) {
                Thread.Sleep(1000);
            }

            DownloadRace downloadRace = new DownloadRace();

            downloadRace.DownloadPage("https://ravit.is.fi/tulokset/" +kisaId);

            while (!downloadRace.DownloadCompleted) {
                Thread.Sleep(1000);
            }

            int[] hevoset = Analyze(downloadRace._result);

            for (int i = 0; i < hevoset.Length; i++) {
                AddToDatabase(kisaId, hevoset[i]);
            }

            CloseDatabase();
        }

        static void OpenDatabase() {
            _connectionStringBuilder = new SqliteConnectionStringBuilder();
            _connectionStringBuilder.DataSource = "./main.db";

            using (_connection = new SqliteConnection(_connectionStringBuilder.ConnectionString)) {
                try {
				    _connection.Open();
                    Console.WriteLine("hep");
                }
                catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }
			}

            
        }

        static void CloseDatabase() {
           _connection.Close();
        }

        static int[] Analyze(string result) {
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

            return hevoset;
        }

        static void AddToDatabase(int kisaID, int horseID) {
            string cmdString = "INSERT INTO sijoitukset(kisa_id, hevonen_id) VALUES(@param1, @param2)";

            using (SqliteCommand cmd = new SqliteCommand(cmdString, _connection)){
                cmd.Parameters.Add("@param1", SqliteType.Integer).Value = kisaID;
                cmd.Parameters.Add("@param2", SqliteType.Integer).Value = horseID;
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.ExecuteNonQuery();
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
        }
    }
}
