using BpmnToDcrConverter.Dcr;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;

namespace BpmnToDcrConverter
{
    public static class DcrSolutionsPostRequestHandler
    {
        private const string URL = @"https://repository.dcrgraphs.net/api/graphs";

        public static async void Post(DcrGraph dcrGraph)
        {
            Console.WriteLine("You need a user at dcrgraphs.net to make the POST request.");

            Console.Write("Username: ");
            //string username = Console.ReadLine();
            string username = "Aimo";

            Console.Write("Password: ");
            //string password = Console.ReadLine();
            string password = "P8s#H0nZmunB3o5qksE9";

            string json = "{ \"JSON\": \" \\\"id\\\": 123456, \\\"title\\\": \\\"TITLE!\\\" \" }";

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
