using BpmnToDcrConverter.Dcr;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace BpmnToDcrConverter
{
    public static class DcrSolutionsPostRequestHandler
    {
        private const string URL = @"https://repository.dcrgraphs.net/api/graphs";

        public static async Task Post(DcrGraph dcrGraph)
        {
            Console.WriteLine("You need a user at dcrgraphs.net to make the POST request.");

            Console.Write("Username: ");
            string username = Console.ReadLine();

            Console.Write("Password: ");
            string password = Console.ReadLine();

            string json = @"{""JSON"": ""{\""id\"": 123456, \""title\"": \""TITLE 5!\"", \""events\"": [{\""id\"": \""event\"", \""label\"": \""Text\"", \""pending\"": true}]}""}";

            using (HttpClient client = new HttpClient())
            {
                string authenticationCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authenticationCredentials);

                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(URL, content);

                Console.WriteLine(response.Content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("POST request to DCR solutions failed.");
                }


            }
        }
    }
}
