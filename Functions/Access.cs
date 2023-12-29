using NeonService.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NeonService.Functions
{
    public class Access
    {
        private readonly ILogger<Worker> _logger;
        public Access(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public void GetFilesList(string token, string connectionID) 
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

            if (jsonToken != null)
            {
                foreach (var claim in jsonToken.Claims)
                {
                    if (claim.Type.Contains("data"))
                    {
                        string forRequestToConnectionID = connectionID;
                        var data = JsonConvert.DeserializeObject<GetFilesListRequest>(claim.Value);

                        try
                        {
                            if (data.path == "")
                            {
                                string defaultPath = "C:\\";
                                string[] entries = Directory.GetFiles(defaultPath).Select(x => WebUtility.UrlEncode(x)).ToArray(); //Directory.GetFileSystemEntries("C:\\Users", "*", SearchOption.AllDirectories)
                                string[] directories = Directory.GetDirectories(defaultPath).Select(x => WebUtility.UrlEncode(x)).ToArray();
                                string resultFiles = JsonConvert.SerializeObject(entries);
                                string resultDirs = JsonConvert.SerializeObject(directories);
                                string resultJsonString = $"{{ \"deviceID\": \"{data.deviceID}\", \"toID\": \"{forRequestToConnectionID}\", \"path\": \"{WebUtility.UrlEncode(defaultPath)}\", \"dirs\": {resultDirs}, \"files\": {resultFiles} }}";
                                _logger.LogInformation("Directory List: {dirs}", resultJsonString);

                                Requests requests = new Requests();
                                requests.DeviceFilesListResRequest(resultJsonString);
                            }
                            else
                            {
                                string defaultPath = $"{WebUtility.UrlDecode(data.path)}";
                                string[] entries = Directory.GetFiles(defaultPath).Select(x => WebUtility.UrlEncode(x)).ToArray(); //Directory.GetFileSystemEntries("C:\\Users", "*", SearchOption.AllDirectories)
                                string[] directories = Directory.GetDirectories(defaultPath).Select(x => WebUtility.UrlEncode(x)).ToArray();
                                string resultFiles = JsonConvert.SerializeObject(entries);
                                string resultDirs = JsonConvert.SerializeObject(directories);
                                string resultJsonString = $"{{ \"deviceID\": \"{data.deviceID}\",  \"toID\": \"{forRequestToConnectionID}\", \"path\": \"{WebUtility.UrlEncode(defaultPath)}\", \"dirs\": {resultDirs}, \"files\": {resultFiles} }}";
                                _logger.LogInformation("Directory List: {dirs}", resultJsonString);

                                Requests requests = new Requests();
                                requests.DeviceFilesListResRequest(resultJsonString);
                            }
                        }
                        catch(Exception ex)
                        {
                            _logger.LogInformation(ex.Message);
                            _logger.LogInformation("Directory List: Error");
                        }
                    }
                }
            }
            else
            {
                _logger.LogInformation("Neon Service: Invalid Token");
            }
        }
    }
}
