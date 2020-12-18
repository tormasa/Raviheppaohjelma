using System;
using System.Net;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace Raviheppaohjelma {
    class Program {
        static SqliteConnectionStringBuilder _connectionStringBuilder;
        static SqliteConnection _connection;
        static RaceEntry raceEntry;
        static string databaseLoc = "D:/Google Drive/Tietokannat/Heppatietokanta/hepat.db";

        static void Main(string[] args) {
            Console.Clear();
            TaskSelection();
        }

        static void TaskSelection() {
            Console.WriteLine("1. Download Races");
            Console.WriteLine("\n(Press ESC to quit)");

            while (true) {
                ConsoleKeyInfo keyInfo = Console.ReadKey();

                if (keyInfo.Key == ConsoleKey.D1 || keyInfo.Key == ConsoleKey.NumPad1) {
                    Console.Clear();
                    Console.WriteLine("Add new races to database. (Press ESC to quit)");
                    AddRacesToDatabase();

                    break;
                }
                else if (keyInfo.Key == ConsoleKey.Escape) {
                    Environment.Exit(0);
                }
                else {
                    Console.Clear();
                    Console.WriteLine("Invalid selection. Try again:\n");
                }
            }
        }

        static void OpenDatabase() {
            _connectionStringBuilder = new SqliteConnectionStringBuilder();
            _connectionStringBuilder.DataSource = databaseLoc;

            _connection = new SqliteConnection(_connectionStringBuilder.ConnectionString);
            _connection.Open();
            Console.WriteLine("open database");
        }

        static void CloseDatabase() {
           _connection.Close();
           Console.WriteLine("close database");
        }

        static void AddRacesToDatabase() {
            int kisaId;
            int noMoreRacesCounter = 0;
            int noMoreRacesMax = 100;
            bool noMoreRaces = false;
            raceEntry = new RaceEntry();

            OpenDatabase();

            kisaId = MaxRaceID() + 1;

            do {

                while (!Console.KeyAvailable) {
                    DownloadRace downloadRace = new DownloadRace();

                    downloadRace.DownloadPage("https://ravit.is.fi/tulokset/" +kisaId);

                    while (!downloadRace.DownloadCompleted) {
                        Thread.Sleep(1000);
                    }

                    // Tarkistetaan onko lähtöä juostu
                    if (RaceRun(downloadRace._result)) {
                        Analyze(downloadRace._result);

                        for (int h = 0; h < raceEntry.hevoset.Length; h++) {
                            AddToDatabase(kisaId, raceEntry.hevoset[h], raceEntry.odds[h]);
                        }

                        Console.WriteLine("Inserted race " +kisaId +" to database");

                        // Nollataan nollakisojen counter
                        // Idea on, että kun löytyy x määrä nollakisoja, niin todennäköisesti ollaan nykypäivässä
                        noMoreRacesCounter = 0;
                    }
                    else {
                        Console.WriteLine("Race " +kisaId +" has not been ran or there is problem with it");
                        noMoreRacesCounter++;

                        if (noMoreRacesCounter >= noMoreRacesMax) {
                            noMoreRaces = true;
                            break;
                        }
                    }

                    kisaId++;
                }

            } while (!noMoreRaces && Console.ReadKey(true).Key != ConsoleKey.Escape);

            if (noMoreRaces) Console.WriteLine("No more races to add");

            CloseDatabase();
        }

        static bool RaceRun(string result) {
            if (result.IndexOf("<td>1.</td>") > -1) {
                return true;
            }
            else {
                return false;
            }
        }

        static int MaxRaceID() {
            int value = 0;
            string cmdString = "SELECT MAX(kisa_id) FROM sijoitukset";

            using (SqliteCommand cmd = new SqliteCommand(cmdString, _connection)){
                cmd.CommandType = System.Data.CommandType.Text;

                using (var reader = cmd.ExecuteReader()) {
					while (reader.Read()) {
						string str = "";

						for (int i = 0; i < reader.FieldCount; i++) {
							str += reader[i].ToString();
						}

                        if (str.Length > 0) {
                            str.Replace(',', '.');
                            value = Int32.Parse(str, CultureInfo.InvariantCulture);
                        }
					}
				}
            }

            return value;
        }

        static void Analyze(string result) {
            int maxHorses = 20;
            string[] separators = new string[maxHorses + 1];
            string[] stringArr;
            string[] firstArr;
            string[] secondArr;
            string[] firstArrOdds;
            string[] resultOdds;
            bool raceHasOdds;

            raceHasOdds = result.Contains("Kerroin");

            for (int i = 0; i < maxHorses; i++) {
                separators[i] = "<td>" +i +".</td>";
            }
            separators[maxHorses] = "<td>h";

            stringArr = result.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            raceEntry.hevoset = new int[stringArr.Length - 1];
            raceEntry.odds = new float[stringArr.Length - 1];

            for (int i = 1; i < stringArr.Length; i++) {
                firstArr = stringArr[i].Split("hevoset/");
                secondArr = firstArr[1].Split("/");
                raceEntry.hevoset[i-1] = Int32.Parse(secondArr[0]);

                if (raceHasOdds) {
                    firstArrOdds = firstArr[1].Split("<td align=\"right\">");
                    resultOdds = firstArrOdds[1].Split("<");

                    if (resultOdds[0].Length > 0) raceEntry.odds[i-1] = float.Parse(resultOdds[0]);
                    else raceEntry.odds[i-1] = -1f;
                }
                else {
                    raceEntry.odds[i-1] = -1f;
                }
            }
        }

        static void AddToDatabase(int kisaID, int horseID, float odds) {
            string cmdString = "INSERT INTO sijoitukset(kisa_id, hevonen_id, odds) VALUES(@param1, @param2, @param3)";

            using (SqliteCommand cmd = new SqliteCommand(cmdString, _connection)){
                cmd.Parameters.Add("@param1", SqliteType.Integer).Value = kisaID;
                cmd.Parameters.Add("@param2", SqliteType.Integer).Value = horseID;
                cmd.Parameters.Add("@param3", SqliteType.Real).Value = odds;
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

            //client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgress);
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
                //Console.WriteLine("Download completed!");
            }

            _completed = true;

            _result = (string)e.Result;
        }
    }

    class RaceEntry {
        public int[] hevoset;
        public float[] odds;
    }
}
