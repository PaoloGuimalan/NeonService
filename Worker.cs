using NeonService.Models;
using NeonService.Functions;
using Newtonsoft.Json;
using System.Net.Http;
using System.Reflection;
using System.Reflection.PortableExecutable;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using static System.Runtime.InteropServices.JavaScript.JSType;
using NeonService.Variables;

namespace NeonService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private HttpClient client;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            client = new HttpClient();
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            client.Dispose();
            return base.StopAsync(cancellationToken);
        }

        public void ActionDeterminer(string listener, string result, string connectionID)
        {
            switch(listener)
            {
                case "devicefileslist":
                    Access AccessNeon = new Access(_logger);
                    AccessNeon.GetFilesList(result, connectionID);
                    break;
                default:
                    break;
            }
        }

        public void ExecuteEvent(string event_data, string authtoken)
        {
            var result = $"{{ {event_data} }}";
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(authtoken) as JwtSecurityToken;

            if(jsonToken != null)
            {
                foreach (var claim in jsonToken.Claims)
                {
                    if (claim.Type.Contains("token"))
                    {
                        var authjson = $"{{ token: \"{claim.Value}\", iat: \"\" }}";
                        var authdata = JsonConvert.DeserializeObject<AuthToken>(authjson);
                        var handlerauthcred = new JwtSecurityTokenHandler();
                        var jsonauthcred = handlerauthcred.ReadToken(authdata.token) as JwtSecurityToken;

                        if(jsonauthcred != null)
                        {
                            foreach (var credclaim in jsonauthcred.Claims)
                            {
                                if (credclaim.Type.Contains("userID"))
                                {
                                    var data = JsonConvert.DeserializeObject<ResponseData>(result);
                                    //_logger.LogInformation("Neon Auth Token: {token}", credclaim.Value);
                                    _logger.LogInformation("Neon Service: {listener} {token}", data.data.listener, data.data.result);

                                    ActionDeterminer(data.data.listener, data.data.result, credclaim.Value);
                                }
                            }
                        }
                        else
                        {
                            _logger.LogInformation("Neon Service: Invalid Auth Cred Token");
                        }
                    }
                }
            }
            else
            {
                _logger.LogInformation("Neon Service: Invalid Token");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string NeonExeName = "neonauth.txt";
            string ExtraPath = "AppData\\Local\\Programs\\neonai\\resources\\extraResources\\service";
            var assemblyLocation = Assembly.GetEntryAssembly()?.Location;
            var currentPath = Path.GetDirectoryName(assemblyLocation);
            var finalPath = Path.GetFullPath(Path.Combine(currentPath != null ? currentPath : "", @"..\..\..\..\..\..\..\"));

            var finalExecutable = $"{finalPath}{ExtraPath}\\{NeonExeName}";

            if (File.Exists(finalExecutable))
            {
                string authtext = File.ReadAllText(finalExecutable);

                _logger.LogInformation("Neon Service detected auth token");

                Envs envs = new Envs();

                var requestUri = $"{envs.API}{envs.SSEHandshake}{authtext}"; //neonaiserver.onrender.com
                var stream = client.GetStreamAsync(requestUri).Result;

                using (var reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        while (!stoppingToken.IsCancellationRequested)
                        {
                            try
                            {
                                var line = reader.ReadLine();
                                if (line.Contains("data"))
                                {
                                    ExecuteEvent(line, authtext);
                                }
                            }
                            catch(Exception ex)
                            {
                                _logger.LogInformation("Stream Ended");
                                //await Task.Delay(3000, stoppingToken);
                                await StartAsync(stoppingToken);
                            }
                        }
                    }
                }
            }
            else
            {
                _logger.LogInformation("Neon Service cannot detect authentication file");
            }
        }
    }
}
