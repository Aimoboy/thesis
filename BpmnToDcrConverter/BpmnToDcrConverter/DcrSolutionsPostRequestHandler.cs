using BpmnToDcrConverter.Dcr;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace BpmnToDcrConverter
{
    public static class DcrSolutionsPostRequestHandler
    {
        private const string REPOSITORY_URL = @"https://repository.dcrgraphs.net/api/";

        public static AuthenticationHeaderValue GetDcrSolutionsAuthenticationHeader()
        {
            Console.WriteLine("Your dcrgraphs.net credentials are needed to make the POST request.");

            Console.Write("Username: ");
            string username = Console.ReadLine();

            Console.Write("Password: ");
            string password = Console.ReadLine();

            string authenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            AuthenticationHeaderValue authenticationHeader = new AuthenticationHeaderValue("Basic", authenticationString);

            return authenticationHeader;
        }

        private static HttpResponseMessage PostToDcrSolutions(string url, string jsonBody, AuthenticationHeaderValue authenticationHeader)
        {
            return PostToDcrSolutionsHelper(url, jsonBody, authenticationHeader).GetAwaiter().GetResult();
        }

        private static async Task<HttpResponseMessage> PostToDcrSolutionsHelper(string url, string jsonBody, AuthenticationHeaderValue authenticationHeader)
        {
            HttpResponseMessage response;
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = authenticationHeader;
                StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                response = await client.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"POST request to {url} failed. Body: {jsonBody}");
                }
            }

            return response;
        }

        public static string PostGraph(DcrGraph dcrGraph, AuthenticationHeaderValue authenticationHeader)
        {
            string modelJson = DcrToJsonConverter.GetJsonString(dcrGraph);
            modelJson = Utilities.EscapeStringForApi(modelJson);

            string json = "{\"JSON\":\"" + modelJson + "\"}";
            string url = REPOSITORY_URL + "graphs";
            HttpResponseMessage response = PostToDcrSolutions(url, json, authenticationHeader);

            // Get graph id
            string locationHeader = response.Headers.Location.ToString();
            string regexPattern = @"/api/graphs/(\d+)";
            Regex regex = new Regex(regexPattern);
            Match match = regex.Match(locationHeader);

            if (!match.Success)
            {
                throw new Exception("Could not find DCR graph id in the DCR solutions response.");
            }

            return match.Groups[1].Value;
        }

        public static void PostTrace(string graphId, GraphTrace trace, AuthenticationHeaderValue authenticationHeader)
        {
            string traceJson = trace.ToXml().OuterXml;
            traceJson = Utilities.EscapeStringForApi(traceJson);

            string json = "{\"log\": " + traceJson + " \"\"}";
            string url = REPOSITORY_URL + $"graphs/{graphId}/sims";
            PostToDcrSolutions(url, json, authenticationHeader);
        }
    }
}
