using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BlockadeLabsSDK
{
    public class ApiRequests
    {
        public static async Task<List<SkyboxStyle>> GetSkyboxStyles(string apiKey)
        {           
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync("https://backend.blockadelabs.com/api/v1/skybox/styles" + "?api_key=" + apiKey);

                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var skyboxStylesList = JsonConvert.DeserializeObject<List<SkyboxStyle>>(response);

                    return skyboxStylesList;                   
                }
                else
                {
                    Debug.WriteLine("Get skybox styles error: " + result.StatusCode);                    
                }
            }           

            return null;
        }

        public static async Task<string> CreateSkybox(List<SkyboxStyleField> skyboxStyleFields, int id, string apiKey)
        {
            // Create a dictionary of string keys and dictionary values to hold the JSON POST params
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("skybox_style_id", id.ToString());

            foreach (var field in skyboxStyleFields)
            {
                if (field.value != "")
                {
                    parameters.Add(field.key, field.value);
                }
            }

            string parametersJsonString = JsonConvert.SerializeObject(parameters);

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(60);                
                string uri = "https://backend.blockadelabs.com/api/v1/skybox?api_key=" + apiKey;
                StringContent jsonContent = new StringContent(parametersJsonString,
                    Encoding.UTF8,
                    "application/json");
            
                
                var response = await client.PostAsync(uri, jsonContent);
            
                if (response.EnsureSuccessStatusCode().IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<CreateSkyboxResult>(jsonResponse);

                    if (result?.obfuscated_id == null)
                    {
                        return string.Empty;
                    }

                    return result.obfuscated_id;
                }

                return string.Empty;
            }            
        }
        
        public static async Task<Dictionary<string, string>> GetImagine(string imagineObfuscatedId, string apiKey)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            using (var client = new HttpClient())
            {
                var getImagineRequest = await client.GetAsync("https://backend.blockadelabs.com/api/v1/imagine/requests/obfuscated-id/" + imagineObfuscatedId + "?api_key=" + apiKey);

                if (getImagineRequest.IsSuccessStatusCode)
                {
                    var response = await getImagineRequest.Content.ReadAsStringAsync();
                    var status = JsonConvert.DeserializeObject<GetImagineResult>(response);                    

                    if (status?.request != null)
                    {
                        if (status.request?.status == "complete")
                        {
                            result.Add("textureUrl", status.request.file_url);
                            result.Add("prompt", status.request.prompt);
                            result.Add("depthMapUrl", status.request.depth_map_url);
                        }
                    }
                }
                else
                {                    
                    Debug.WriteLine("Get Imagine Error: " + getImagineRequest.StatusCode);
                }

                return result;
            }                       
        }
        
        public static async Task<byte[]> GetImagineImage(string textureUrl)
        {
            using (var client = new HttpClient())
            {
                var imagineImageRequest = await client.GetAsync(textureUrl);

                if (imagineImageRequest.IsSuccessStatusCode)
                {
                    var image = await imagineImageRequest.Content.ReadAsByteArrayAsync();
                    imagineImageRequest.Dispose();

                    return image;
                }
                else
                {
                    Debug.WriteLine("Get Imagine Image Error: " + imagineImageRequest.StatusCode);
                }

                return null;
            }                      
        }
    }
}