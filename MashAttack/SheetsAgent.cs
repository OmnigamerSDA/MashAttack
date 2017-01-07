
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MashAttack
{
    class SheetsAgent
    {
        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static string ApplicationName = "MashAttack";
        String spreadsheetId = "1Lc5orsabiVfZHTei6SYLO9t7ORviHUjPkydaDNz1uLs";
        SheetsService service;

        public SheetsAgent()
        {
            Startup().Wait();

            // Create Google Sheets API service.
            //service = new SheetsService(new BaseClientService.Initializer()
            //{
            //    HttpClientInitializer = credential,
            //    ApplicationName = ApplicationName,
            //});
        }

        private async Task Startup()
        {
            //UserCredential credential;
            Console.WriteLine("Starting creds");
            using (var stream =
                new FileStream("C:\\MashAttack\\mash_secret.json", FileMode.Open, FileAccess.Read))
            {
                Console.WriteLine("Setting credpath");
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials\\mashattack_creds2.json");

                Console.WriteLine("Authorizing");
                UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "Omnigamer",
                    CancellationToken.None,
                    new FileDataStore(credPath, true));
                Console.WriteLine("Credential file saved to: " + credPath);

                service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });
            }
        }

        public List<String> GetUsernames()
        {
            String range = "Responses!B2:B";
            SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(spreadsheetId, range);

            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;
            List<String> usernames = new List<string>();
            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    if (row != null)
                        // Print columns A and E, which correspond to indices 0 and 4.
                        usernames.Add(String.Format("{0}", row[0]));
                }
                usernames.Sort();
                usernames = usernames.Distinct().ToList();
                return usernames;
            }
            else
            {
                Console.WriteLine("No data found.");
            }

            return null;

        }

        public void AddUsername(String newname)
        {
            String range = "Responses!A1:B1";
            ValueRange myvalues = new ValueRange();
            myvalues.MajorDimension = "ROWS";
            //myvalues.Range = range;
            var oblist = new List<object>() { System.DateTime.Now, newname };
            myvalues.Values = new List<IList<object>> { oblist };
            SpreadsheetsResource.ValuesResource.AppendRequest request = service.Spreadsheets.Values.Append(myvalues, spreadsheetId, range);

            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            request.Execute();

        }

        //    // Define request parameters.
        //    String spreadsheetId = "1Lc5orsabiVfZHTei6SYLO9t7ORviHUjPkydaDNz1uLs";
        //    String range = "Responses!A2:E";
        //    SpreadsheetsResource.ValuesResource.GetRequest request =
        //            service.Spreadsheets.Values.Get(spreadsheetId, range);

        //    // Prints the names and majors of students in a sample spreadsheet:
        //    // https://docs.google.com/spreadsheets/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/edit
        //    ValueRange response = request.Execute();
        //    IList<IList<Object>> values = response.Values;
        //    if (values != null && values.Count > 0)
        //    {
        //        Console.WriteLine("Name, Major");
        //        foreach (var row in values)
        //        {
        //            // Print columns A and E, which correspond to indices 0 and 4.
        //            Console.WriteLine("{0}, {1}", row[0], row[4]);
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("No data found.");
        //    }
        //    Console.Read();
        //}

        public void SaveSession(Stats mystats, string player, string input, string mode)
        {
            int index = GetTotal() + 2 ;
            String range = "Log!A"+index+":I";
            ValueRange myvalues = new ValueRange();
            myvalues.MajorDimension = "ROWS";
            //myvalues.Range = range;
            var oblist = new List<object>() { System.DateTime.Now, player, input, mode, mystats.rate,mystats.median,mystats.up,mystats.down,mystats.score };
            myvalues.Values = new List<IList<object>> { oblist };
            SpreadsheetsResource.ValuesResource.UpdateRequest request = service.Spreadsheets.Values.Update(myvalues,spreadsheetId, range);

            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            request.Execute();

            SetTotal(index - 1);


            range = "Search!C17:E";
            myvalues = new ValueRange();
            myvalues.MajorDimension = "ROWS";
            //myvalues.Range = range;
            oblist = new List<object>() { "\""+player+"\"", "\""+input+ "\"", "\""+mode+ "\"" };
            myvalues.Values = new List<IList<object>> { oblist };
            request = service.Spreadsheets.Values.Update(myvalues, spreadsheetId, range);

            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            request.Execute();
        }

        private int GetTotal()
        {
            String range = "Search!B3";
            SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(spreadsheetId, range);

            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;
            int myval = 0;
            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    if (row != null)
                        // Print columns A and E, which correspond to indices 0 and 4.
                        myval = Convert.ToInt32(row[0]);
                }
            }
            else
            {
                Console.WriteLine("No data found.");
            }

            return myval;
        }

        private void SetTotal(int index)
        {
            String range = "Search!B3";
            ValueRange myvalues = new ValueRange();
            myvalues.MajorDimension = "COLUMNS";
            myvalues.Range = range;
            var oblist = new List<object>() { index };
            myvalues.Values = new List<IList<object>> { oblist };
            SpreadsheetsResource.ValuesResource.UpdateRequest request = service.Spreadsheets.Values.Update(myvalues, spreadsheetId, range);

            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            request.Execute();
        }

        public Stats GetGlobal()
        {
            String range = "Search!C20:J20";
            SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(spreadsheetId, range);
            Stats mystats = new Stats();

            double[] vals = new double[8];
            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;
            int i = 0;
            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    if (row != null)
                    {
                        // Print columns A and E, which correspond to indices 0 and 4.
                        vals[i++] = Convert.ToDouble(row[0]);
                        vals[i++] = Convert.ToDouble(row[1]);
                        vals[i++] = Convert.ToDouble(row[2]);
                        vals[i++] = Convert.ToDouble(row[3]);
                        vals[i++] = Convert.ToDouble(row[4]);
                        vals[i++] = Convert.ToDouble(row[5]);
                        vals[i++] = Convert.ToDouble(row[6]);
                        vals[i++] = Convert.ToDouble(row[7]);
                    }
                }
            }
            else
            {
                Console.WriteLine("No data found.");
            }

            mystats.rate = vals[0];
            mystats.max = vals[1];
            mystats.median = vals[2];
            mystats.up = vals[4];
            mystats.down = vals[5];
            mystats.score = vals[6];
            mystats.maxscore = vals[7];

            return mystats;
        }

        public Stats GetPlayer()
        {
            String range = "Search!C24:J24";
            SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(spreadsheetId, range);
            Stats mystats = new Stats();

            double[] vals = new double[8];
            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;
            int i = 0;
            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    if (row != null)
                    {
                        // Print columns A and E, which correspond to indices 0 and 4.
                        vals[i++] = Convert.ToDouble(row[0]);
                        vals[i++] = Convert.ToDouble(row[1]);
                        vals[i++] = Convert.ToDouble(row[2]);
                        vals[i++] = Convert.ToDouble(row[3]);
                        vals[i++] = Convert.ToDouble(row[4]);
                        vals[i++] = Convert.ToDouble(row[5]);
                        vals[i++] = Convert.ToDouble(row[6]);
                        vals[i++] = Convert.ToDouble(row[7]);
                    }
                }
            }
            else
            {
                Console.WriteLine("No data found.");
            }

            mystats.rate = vals[0];
            mystats.max = vals[1];
            mystats.median = vals[2];
            mystats.up = vals[4];
            mystats.down = vals[5];
            mystats.score = vals[6];
            mystats.maxscore = vals[7];

            return mystats;
        }

        public Stats[] GetBests(string mode)
        {
            String range = "'" + mode + "'!Q3:U12";
            SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(spreadsheetId, range);
            Stats[] mystats = new Stats[10];

            double[,] vals = new double[10,5];
            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;
            int i = 0;
            for (i = 0; i < 10; i++)
            {
                var row = values[i];
                mystats[i].rate = Convert.ToDouble(row[0]);
                mystats[i].median = Convert.ToDouble(row[1]);
                mystats[i].up = Convert.ToDouble(row[2]);
                mystats[i].down = Convert.ToDouble(row[3]);
                mystats[i].score = Convert.ToDouble(row[4]);

            }

            return mystats;
        }
    }
}
