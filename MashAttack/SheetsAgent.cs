
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
        static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
        static string ApplicationName = "MashAttack";
        String spreadsheetId = "1Lc5orsabiVfZHTei6SYLO9t7ORviHUjPkydaDNz1uLs";
        SheetsService service;

        public SheetsAgent()
        {
            Startup().Wait() ;

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
                new FileStream("M:\\Dropbox\\mash_secret.json", FileMode.Open, FileAccess.Read))
            {
                Console.WriteLine("Setting credpath");
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials\\mashattack_creds.json");

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
                    // Print columns A and E, which correspond to indices 0 and 4.
                    usernames.Add(String.Format("{0}", row[0]));
                }
                usernames.Sort();
                return usernames;
            }
            else
            {
                Console.WriteLine("No data found.");
            }

            return null;

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
    }
}
