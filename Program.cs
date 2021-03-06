﻿using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using System.Net.Http;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Net;
using Microsoft.ApplicationInsights.DependencyCollector;

namespace consoleApp {
    class Program {


        static void Main(string[] args) {
            var services = new ServiceCollection();
            var builder = new ConfigurationBuilder()
               .SetBasePath(Path.Combine(AppContext.BaseDirectory))
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();
            var settingsConfig = new Settings();
            configuration.GetSection("MySettings").Bind(settingsConfig);


            // Initialize Service Collection
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, settingsConfig.InstrumentationKey);

            // Initialize Service Provider
            var serviceProvider = serviceCollection.BuildServiceProvider();
            // Handling finalizing when process is ended
            AppDomain.CurrentDomain.ProcessExit += (s, e) => FinalizeApplication(serviceProvider);
            var client = serviceProvider.GetService<TelemetryClient>();
            client.Context.User.Id = settingsConfig.UserId; //Add User ID here for simulating Authen user

            Console.Write("How many times should I loop: ");
            var numLoops = 1;
            try {
                numLoops = int.Parse(Console.ReadLine());
                client.GetMetric("Loops").TrackValue(numLoops);
            }
            catch(Exception e) {
                client.TrackException(e);
            }

            if (numLoops % 2 == 0)
                callIfEven(client, numLoops);


            for (int i = 0; i < numLoops; i++) {
                printConsole("Tracking Main() method", client);
                client.TrackPageView("Main");
                printConsole("Tracing App Startup", client);
                client.TrackTrace("Demo application starting up.");
                printConsole("Tracking Event", client);
                client.TrackEvent($"Trace: {DateTime.UtcNow.ToUniversalTime()}");
                printConsole("Tracking Value", client);
                client.GetMetric("Random Number").TrackValue(new Random().NextDouble());
                printConsole("Tracking Dice roll", client);
                client.GetMetric("Dice Roll").TrackValue(new Random().Next(1, 36));
                printConsole("Tracking Metric", client);
                client.TrackMetric("Milliseconds", DateTime.Now.Ticks);
                printConsole("Tracking noOp() method", client);
                run_python_cmd(client);
                Console.WriteLine($"-------------- {i+1} of {numLoops} ----------------");
            }

            using (var httpClient = new HttpClient()) {
                // Http dependency is automatically tracked!
                httpClient.GetAsync("https://google.com").Wait();
            }

            client.Flush();

            // flush is non blocking so wait a bit
            Console.WriteLine("> Pausing for 5 seconds to flush() stream... ");
            Thread.Sleep(5000);
            printConsole("Sent Telemetry", client);

            //Set a different Exit Page on some occasions
            if (numLoops % 2 == 0)
                noOp(client);


        }

        //Run a Python script
        private static void run_python_cmd(TelemetryClient client) {
            client.TrackPageView("run_python_cmd");

            var startTime = DateTime.UtcNow;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            try {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = "python.exe";
                start.Arguments = string.Format("worldTimeAPI.py");
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                using (Process process = Process.Start(start)) {
                    using (StreamReader reader = process.StandardOutput) {
                        string result = reader.ReadToEnd();
                        Console.Write(result);
                        Call_REST_API(client, result);
                    }
                }
            }
            finally {
                timer.Stop();
                client.TrackDependency("python","worldTimeAPI", "REST API", startTime, timer.Elapsed, true);
            }

        }

        //Call a REST API and get some data
        private static async void Call_REST_API(TelemetryClient client, string url) {
            client.TrackPageView("Call_REST_API");
            string responseText = "";
            var jObject = JObject.Parse(url);

            var startTime = DateTime.UtcNow;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            try {

                using (var httpClient = new HttpClient()) {
                    var result = await httpClient.GetAsync(jObject["Tz"].ToString());
                    responseText = await result.Content.ReadAsStringAsync();
                }

                var JsonResponse = JObject.Parse(responseText);
                var msg = $"Local time in {jObject["City"].ToString()} is {JsonResponse["utc_datetime"].ToString()}";
                Console.Write(msg);
            }
            finally {
                timer.Stop();
                client.TrackDependency("HttpClient", "web-request", jObject["Tz"].ToString(), startTime, timer.Elapsed, true);
            }
        }

        private static void callIfEven(TelemetryClient client, int numLoops) {
            client.TrackPageView("callIfEven");
            Thread.Sleep(numLoops*5);
        }

        private static void noOp(TelemetryClient client) {
            client.TrackPageView("noOp");
            Thread.Sleep(10);
        }

        private static void printConsole(string stringToPrint, TelemetryClient client) {
            client.TrackPageView("printConsole");
            Console.WriteLine(stringToPrint);
        }

        static DependencyTrackingTelemetryModule InitializeDependencyTracking(TelemetryConfiguration configuration) {
            var module = new DependencyTrackingTelemetryModule();

            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("localhost");
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("127.0.0.1");
            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.EventHubs");

            // initialize the module
            module.Initialize(configuration);

            return module;
        }
        #region deprecated
        /*
                static void Main(string[] args) {
                    TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();

                    configuration.InstrumentationKey = "d9ed39ea-e56f-4e8d-b721-3a51f7e2fd87";
                    configuration.TelemetryInitializers.Add(new HttpDependenciesParsingTelemetryInitializer());

                    var telemetryClient = new TelemetryClient(configuration);
                    using (InitializeDependencyTracking(configuration)) {
                        // run app...

                        telemetryClient.TrackTrace("Hello World!");

                        using (var httpClient = new HttpClient()) {
                            // Http dependency is automatically tracked!
                            httpClient.GetAsync("https://microsoft.com").Wait();
                        }

                    }

                    // before exit, flush the remaining data
                    telemetryClient.Flush();

                    // flush is not blocking so wait a bit
                    Task.Delay(5000).Wait();

                }

                private static DependencyTrackingTelemetryModule InitializeDependencyTracking(TelemetryConfiguration configuration) {
                    var module = new DependencyTrackingTelemetryModule();

                    // prevent Correlation Id to be sent to certain endpoints. You may add other domains as needed.
                    module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.windows.net");
                    module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.chinacloudapi.cn");
                    module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.cloudapi.de");
                    module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.usgovcloudapi.net");
                    module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("localhost");
                    module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("127.0.0.1");

                    // enable known dependency tracking, note that in future versions, we will extend this list. 
                    // please check default settings in https://github.com/microsoft/ApplicationInsights-dotnet-server/blob/develop/WEB/Src/DependencyCollector/DependencyCollector/ApplicationInsights.config.install.xdt

                    module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                    module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.EventHubs");

                    // initialize the module
                    module.Initialize(configuration);

                    return module;
                }
        */
        #endregion

        private static void FinalizeApplication(ServiceProvider serviceProvider) {
            // Give TelemetryClient 5 seconds to flush it's content to Application Insights
            serviceProvider.GetService<TelemetryClient>().Flush();
            //Thread.Sleep(5000);
        }

        private static void ConfigureServices(IServiceCollection serviceCollection, String intrumentationKey) {
            // Add Application Insights
            var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            telemetryConfiguration.InstrumentationKey = intrumentationKey;
            telemetryConfiguration.TelemetryInitializers.Add(new HttpDependenciesParsingTelemetryInitializer());
            var telemetryClient = new TelemetryClient(telemetryConfiguration);
            serviceCollection.AddSingleton(telemetryClient);
        }

        

    }
}
