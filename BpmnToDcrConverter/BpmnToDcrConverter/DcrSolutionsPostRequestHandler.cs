using BpmnToDcrConverter.Dcr;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Net.Http.Headers;

namespace BpmnToDcrConverter
{
    public static class DcrSolutionsPostRequestHandler
    {
        private const string URL = @"https://repository.dcrgraphs.net/api/graphs";

        public static async Task Post(DcrGraph dcrGraph)
        {
            Console.WriteLine("Your dcrgraphs.net credentials are needed to make the POST request.");

            Console.Write("Username: ");
            string username = Console.ReadLine();

            Console.Write("Password: ");
            string password = Console.ReadLine();

            string modelJson = DcrToJsonConverter.GetJsonString(dcrGraph).Replace("\"", "\\\"");
            string json = "{\"JSON\":\"" + modelJson + "\"}";

            using (HttpClient client = new HttpClient())
            {
                string authenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                AuthenticationHeaderValue authenticationHeader = new AuthenticationHeaderValue("Basic", authenticationString);
                client.DefaultRequestHeaders.Authorization = authenticationHeader;

                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(URL, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("POST request to DCR solutions failed.");
                }

                Console.WriteLine("POST request succeeded!");
            }
        }
    }
}
