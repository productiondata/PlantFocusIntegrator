using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PlantFocus_Integrator
{
    public class ApiHelper
    {
        public async Task<string> MakeHTTPGetRequestAsync(string url)
        {
            try
            {
                string responseContent = "";

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.Content != null && response.IsSuccessStatusCode)
                    {
                        responseContent = await response.Content.ReadAsStringAsync();
                    }
                }
                return responseContent;
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"Exception in MakeHTTPGetRequestAsync : {ex.Message}");
                return null;
            }
        }

        public async Task<bool> MakeHttpPostRequestAsync(string url, string json)
        {
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();
                StringContent stringContent = null;

                using (HttpClient client = new HttpClient())
                {

                    client.Timeout = TimeSpan.FromSeconds(30);              

                    if (json != null)
                    {
                        stringContent = new StringContent(json, Encoding.UTF8, "application/json");
                        stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    }

                    response = await client.PostAsync(url, stringContent);

                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in MakeHttpPostRequestAsync: {ex.Message}");
                return false;
            }
        }
    }
}
