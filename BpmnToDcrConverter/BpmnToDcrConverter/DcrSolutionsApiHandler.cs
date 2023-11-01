using BpmnToDcrConverter.Dcr;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace BpmnToDcrConverter
{
    public static class DcrSolutionsApiHandler
    {
        private const string REPOSITORY_URL = @"https://repository.dcrgraphs.net/api/";

        public static AuthenticationHeaderValue GetDcrSolutionsAuthenticationHeader()
        {
            Console.WriteLine("Your dcrgraphs.net credentials are needed to make API requests.");

            Console.Write("Username: ");
            string username = Console.ReadLine();

            Console.Write("Password: ");
            string password = Console.ReadLine();

            string authenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            AuthenticationHeaderValue authenticationHeader = new AuthenticationHeaderValue("Basic", authenticationString);

            return authenticationHeader;
        }

        private static HttpResponseMessage ApiRequest(ApiRequestType requestType, string url, AuthenticationHeaderValue authenticationHeader, string jsonBody = null)
        {
            return ApiRequestHelper(requestType, url, authenticationHeader, jsonBody).GetAwaiter().GetResult();
        }

        private static async Task<HttpResponseMessage> ApiRequestHelper(ApiRequestType requestType, string url, AuthenticationHeaderValue authenticationHeader, string jsonBody)
        {
            HttpResponseMessage response;
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = authenticationHeader;

                switch (requestType)
                {
                    case ApiRequestType.POST:
                        StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                        response = await client.PostAsync(url, content);
                        break;
                    case ApiRequestType.GET:
                        response = await client.GetAsync(url);
                        break;
                    case ApiRequestType.DELETE:
                        response = await client.DeleteAsync(url);
                        break;
                    default:
                        throw new Exception("Unhandled case.");
                }
                
                if (!response.IsSuccessStatusCode)
                {
                    string typeString = ApiRequestTypeToString(requestType);
                    string errorMessage = $"{typeString} request to {url} failed.";

                    if (requestType == ApiRequestType.POST)
                    {
                        errorMessage += $" Body: {jsonBody}";
                    }

                    throw new Exception(errorMessage);
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
            HttpResponseMessage response = ApiRequest(ApiRequestType.POST, url, authenticationHeader, json);

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

        public static void DeleteGraph(string graphId, AuthenticationHeaderValue authenticationHeader)
        {
            string url = REPOSITORY_URL + $"graphs/{graphId}";
            ApiRequest(ApiRequestType.DELETE, url, authenticationHeader);
        }

        public static string GetGraphXml(string graphId, AuthenticationHeaderValue authenticationHeader)
        {
            string url = REPOSITORY_URL + $"graphs/{graphId}";
            HttpResponseMessage response = ApiRequest(ApiRequestType.GET, url, authenticationHeader);
            return response.Content.ReadAsStringAsync().Result;
        }

        public static void PostTrace(string graphId, GraphTrace trace, AuthenticationHeaderValue authenticationHeader)
        {
            string traceJson = trace.ToXml().OuterXml;
            traceJson = Utilities.EscapeStringForApi(traceJson);

            string json = "{\"log\": \"" + traceJson + "\"}";
            string url = REPOSITORY_URL + $"graphs/{graphId}/sims";
            ApiRequest(ApiRequestType.POST, url, authenticationHeader, json);
        }

        public static (bool, string) ValidateLog(string graphXml, string traceXml, AuthenticationHeaderValue authenticationHeader)
        {
            string escapedGraphXml = Utilities.EscapeStringForApi(graphXml);
            string escapedTraceXml = Utilities.EscapeStringForApi(traceXml);

            string json = "{\"graphXml\": \"" + escapedGraphXml + "\", \"dcrLogXml\": \"" + escapedTraceXml + "\", \"detailed\": true}";
            string url = REPOSITORY_URL + "utility/validatelog";
            HttpResponseMessage response = ApiRequest(ApiRequestType.POST, url, authenticationHeader, json);
            string responseXml = response.Content.ReadAsStringAsync().Result;

            XDocument doc = XDocument.Parse(responseXml);
            XElement replay = doc.Element("replay");
            XElement trace = replay.Element("trace");

            bool valid = bool.Parse(trace.Attribute("valid").Value);
            string explanation = trace.Attribute("explanation").Value;

            return (valid, explanation);
        }

        private static string ApiRequestTypeToString(ApiRequestType type)
        {
            return type switch
            {
                ApiRequestType.POST => "POST",
                ApiRequestType.GET => "GET",
                _ => throw new Exception("Unhandled case.")
            };
        }

        private enum ApiRequestType
        {
            POST,
            GET,
            DELETE
        }
    }

}
