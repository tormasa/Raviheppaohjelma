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
            int startKisaId = 166785;
            int kisaId;
            int kisaCount = 1;

            OpenDatabase();

            for (int i = 0; i < kisaCount; i++) {
                kisaId = startKisaId + i;

                DownloadRace downloadRace = new DownloadRace();

                downloadRace.DownloadPage("https://ravit.is.fi/tulokset/" +kisaId);

                while (!downloadRace.DownloadCompleted) {
                    Thread.Sleep(1000);
                }

                // Tarkistetaan onko lähtöä juostu
                if (RaceRun(downloadRace._result)) {
                    int[] hevoset = Analyze(downloadRace._result);

                    for (int h = 0; h < hevoset.Length; h++) {
                        AddToDatabase(kisaId, hevoset[h]);
                    }

                    Console.WriteLine("Inserted race " +kisaId +" to database");
                }
                else {
                    Console.WriteLine("Current race has not been ran");
                    break;
                }
            }

            CloseDatabase();
        }

        static void OpenDatabase() {
            _connectionStringBuilder = new SqliteConnectionStringBuilder();
            _connectionStringBuilder.DataSource = "D:/hepat.db";

            _connection = new SqliteConnection(_connectionStringBuilder.ConnectionString);
            _connection.Open();
            Console.WriteLine("open database");
        }

        static void CloseDatabase() {
           _connection.Close();
           Console.WriteLine("close database");
        }

        static bool RaceRun(string result) {
            if (result.IndexOf("<td>1.</td>") > -1) {
                return true;
            }
            else {
                return false;
            }
        }

        static int[] Analyze(string result) {
            int maxHorses = 20;
            string[] separators = new string[maxHorses + 1];
            string[] stringArr;
            string[] firstArr;
            string[] secondArr;
            int[] hevoset;

            for (int i = 0; i < maxHorses; i++) {
                separators[i] = "<td>" +i +".</td>";
            }
            separators[maxHorses] = "<td>h";

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
