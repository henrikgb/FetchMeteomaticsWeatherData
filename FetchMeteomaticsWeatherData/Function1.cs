using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using System.IO;
using System.Collections.Generic;

namespace WeatherFunctionApp
{
    public static class MeteomaticsFunction
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string storageConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
        private static readonly string meteomaticsUsername = Environment.GetEnvironmentVariable("METEOMATICS_USERNAME");
        private static readonly string meteomaticsPassword = Environment.GetEnvironmentVariable("METEOMATICS_PASSWORD");

        [FunctionName("FetchMeteomaticsWeatherData")]
        public static async Task Run(
            [TimerTrigger("0 0 * * * *")] TimerInfo myTimer, 
            ILogger log)
        {

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
          

            // Check for missing or empty environment variables
            if (string.IsNullOrEmpty(storageConnectionString))
            {
                log.LogError("Azure Storage connection string is missing or empty.");
                return;
            }
            if (string.IsNullOrEmpty(meteomaticsUsername) || string.IsNullOrEmpty(meteomaticsPassword))
            {
                log.LogError("Meteomatics username and/or password is missing or empty.");
                return;
            }
          

            // Create BlobServiceClient and BlobContainerClient with error handling
            BlobServiceClient blobServiceClient = null;
            BlobContainerClient containerClient = null;
            try
            {
                blobServiceClient = new BlobServiceClient(storageConnectionString);
                containerClient = blobServiceClient.GetBlobContainerClient("weatherdata");
            }
            catch (Exception ex)
            {
                log.LogError($"Error creating BlobServiceClient: {ex.Message}");
                return;
            }


            // Iterate through the coordinates and fetch weather data for each one
            foreach (var coordinate in coordinates)
            {
                // Create request url for getting data from Meteomatics API for the current coordinate
                string apiUrl = ConstructMeteomaticsApiUrl(coordinate);

                // Fetch Meteomatics access token
                try
                {
                    string accessToken = await GetMeteomaticsAccessToken(meteomaticsUsername, meteomaticsPassword);
                    // Add the access token to the API URL
                    apiUrl += $"?access_token={accessToken}";
                }
                catch (Exception ex)
                {
                    log.LogError($"Failed to create accessToken: {ex.Message}");
                    continue; // Continue with the next coordinate if there's an error
                }

                // Try to get data from Meteomatics Weather API
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        log.LogInformation($"Received Meteomatics weather data for {coordinate.NameId}: {content}");

                        // Save data to Azure Blob Storage with the nameId of the coordinate
                        string blobName = $"{coordinate.NameId}_MeteomaticsWeatherData.json";
                        BlobClient blobClient = containerClient.GetBlobClient(blobName);

                        using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)))
                        {
                            await blobClient.UploadAsync(stream, overwrite: true);
                            log.LogInformation($"Meteomatics weather data for {coordinate.NameId} uploaded to Blob storage as blob: {blobName}");
                        }
                    }
                    else
                    {
                        log.LogError($"Failed to fetch Meteomatics weather data for {coordinate.NameId}. Status Code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    log.LogError($"Error fetching Meteomatics weather data for {coordinate.NameId}: {ex.Message}");
                }
            }
        }

        private static async Task<string> GetMeteomaticsAccessToken(string username, string password)
        {
            using var client = new HttpClient();
            var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {authHeader}");

            var response = await client.GetAsync("https://login.meteomatics.com/api/v1/token");
            response.EnsureSuccessStatusCode();

            var tokenResponseJson = await response.Content.ReadAsStringAsync();

            // Deserialize the JSON response to a dynamic object
            dynamic tokenResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(tokenResponseJson);

            // Extract the access_token property
            string accessToken = tokenResponse.access_token;

            return accessToken;
        }


        private static string ConstructMeteomaticsApiUrl(Coordinate coordinate)
        {
            // Calculate the "to" date as 7 days from now
            var toDate = DateTime.UtcNow.Add(TimeSpan.FromDays(7)).ToString("yyyy-MM-ddTHH:mm:ssZ");
            // Construct the Meteomatics API URL using the latitude and longitude from the coordinate
            return $"https://api.meteomatics.com/{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")}--{toDate}:PT1H/wind_speed_10m:ms,wind_dir_10m:d,wind_gusts_10m_1h:ms,precip_1h:mm/{coordinate.Latitude},{coordinate.Longitude}/json";
        }


        private class Coordinate
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string NameId { get; set; }
        }


        private static readonly List<Coordinate> coordinates = new List<Coordinate>
        {
            new Coordinate { Latitude = 59.02050003940309, Longitude = 5.592325942611728, NameId = "Sande" },
            new Coordinate { Latitude = 58.88531361351894, Longitude = 5.602662428854268, NameId = "Sola" },
            new Coordinate { Latitude = 58.84227538997773, Longitude = 5.560516132410946, NameId = "Hellesto" },
            new Coordinate { Latitude = 58.81231852222533, Longitude = 5.546945324943648, NameId = "Sele" },
            new Coordinate { Latitude = 58.80123195118268, Longitude = 5.5480941336096645, NameId = "Bore" },
            new Coordinate { Latitude = 58.740441600947264, Longitude = 5.512925570900187, NameId = "Orre" },
            new Coordinate { Latitude = 58.722027, Longitude = 5.521960, NameId = "X" },
            new Coordinate { Latitude = 58.68756890551574, Longitude = 5.551150818355702, NameId = "Refsnes" },
            new Coordinate { Latitude = 58.53797648002299, Longitude = 5.730672797723366, NameId = "Brusand" },
        };

    }
}
