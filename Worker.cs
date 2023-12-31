using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeonService.Functions;
using NeonService.Models;
using NeonService.Variables;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Timers;

namespace NeonService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private HttpClient client;
        private System.Timers.Timer restartTimer;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            InitializeRestartTimer(); // Initialize the timer in the constructor
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            client = new HttpClient();
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Neon Service stopping");
            client.Dispose();
            return base.StopAsync(cancellationToken);
        }

        public void ActionDeterminer(string listener, string result, string connectionID)
        {
            switch (listener)
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

            if (jsonToken != null)
            {
                foreach (var claim in jsonToken.Claims)
                {
                    if (claim.Type.Contains("token"))
                    {
                        var authjson = $"{{ token: \"{claim.Value}\", iat: \"\" }}";
                        var authdata = JsonConvert.DeserializeObject<AuthToken>(authjson);
                        var handlerauthcred = new JwtSecurityTokenHandler();
                        var jsonauthcred = handlerauthcred.ReadToken(authdata.token) as JwtSecurityToken;

                        if (jsonauthcred != null)
                        {
                            foreach (var credclaim in jsonauthcred.Claims)
                            {
                                if (credclaim.Type.Contains("userID"))
                                {
                                    var data = JsonConvert.DeserializeObject<ResponseData>(result);
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

        private void InitializeRestartTimer()
        {
            restartTimer = new System.Timers.Timer();
            restartTimer.Interval = 10000; // 10 seconds interval
            restartTimer.AutoReset = false; // Only fire once per interval
            restartTimer.Elapsed += async (sender, e) => await RestartTimerElapsed();
        }

        private async Task RestartTimerElapsed()
        {
            //_logger.LogInformation("Restart timer elapsed. No data received for 10 seconds. Restarting the connection.");
            restartTimer.Stop();

            // Trigger the cancellation token to restart the connection
            await ExecuteAsync(CancellationToken.None); // This will restart the ExecuteAsync method
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string NeonExeName = "neonauth.txt";
            var finalExecutable = @$"C:\{NeonExeName}";

            //_logger.LogInformation("Checking current directory {path}", Environment.CurrentDirectory);
            //_logger.LogInformation("Scanning path {path}", Path.GetFullPath(finalExecutable));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (client != null)
                        client.Dispose(); // Dispose of the current client

                    client = new HttpClient(); // Create a new HttpClient instance

                    var authtext = await ReadAuthTokenAsync(finalExecutable);

                    Envs envs = new Envs();
                    var requestUri = $"{envs.API}{envs.SSEHandshake}{authtext}"; // neonaiserver.onrender.com

                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3000))) // Set your desired timeout value
                    {
                        var stream = await client.GetStreamAsync(requestUri).WithCancellation(cts.Token);
                        restartTimer.Start(); // Start the timer when the stream is established

                        using (var reader = new StreamReader(stream))
                        {
                            while (!reader.EndOfStream && !stoppingToken.IsCancellationRequested)
                            {
                                var line = await reader.ReadLineAsync();
                                if (line != null && line.Contains("data"))
                                {
                                    ExecuteEvent(line, authtext);
                                    restartTimer.Stop(); // Reset the timer on each received data
                                    restartTimer.Start();
                                }
                            }
                        }
                    }

                    // Log when the stream ends
                    _logger.LogInformation("Stream ended. Restarting in 3 seconds.");
                    await Task.Delay(3000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Stream connection timed out. Restarting in 3 seconds.");
                    await Task.Delay(3000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Stream disconnected. Retrying in 3 seconds. Exception: {ex.Message}");
                    await Task.Delay(3000, stoppingToken);
                }
            }
        }

        private async Task<string> ReadAuthTokenAsync(string filePath)
        {
            if (File.Exists(filePath))
            {
                return await File.ReadAllTextAsync(filePath);
            }
            else
            {
                _logger.LogInformation("Neon Service cannot detect authentication file at {path}", filePath);
                return string.Empty;
            }
        }
    }

    public static class TaskExtensions
    {
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task))
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            return await task; // Unwrap the original exception
        }
    }
}
