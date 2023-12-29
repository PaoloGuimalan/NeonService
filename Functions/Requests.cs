using NeonService.Variables;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NeonService.Functions
{
    public class Requests
    {
        public void DeviceFilesListResRequest (string responsetoken) 
        {
            Envs envs = new Envs();
            string apiUrl = $"{envs.API}{envs.GetFilesListResponse}";
            string jsonContent = responsetoken;

            // Create an instance of HttpClient
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    using (var wb = new WebClient())
                    {
                        var data = new NameValueCollection();
                        data["token"] = jsonContent;

                        wb.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

                        var response = wb.UploadValues(apiUrl, "POST", data);
                        string responseInString = Encoding.UTF8.GetString(response);
                        Console.WriteLine($"{responseInString}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }
    }
}
