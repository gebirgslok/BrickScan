#region License
// Copyright (c) 2020 Jens Eisenbach
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.IO;
using System.Linq;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace BrickScan.Training
{
    static class ReturnCodes
    {
        public static int Success => 0;
        public static int ParseArgsFailed => -1;
        public static int UnhandledException => -2;
    }

    static class Program
    {
        static int Main(string[] args)
        {
            var builder = new ConfigurationBuilder();
            BuildConfiguration(builder);

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Build())
                .CreateLogger();

            Log.ForContext(typeof(Program)).Information("Starting application with args = {Args}.", args);

            try
            {
                var host = Host.CreateDefaultBuilder()
                    .ConfigureServices(ConfigureServices)
                    .UseSerilog()
                    .Build();

                var parserResult = Parser.Default
                    .ParseArguments<TrainOptions, DownloadOptions>(args);

                var returnCode = ReturnCodes.Success;
                parserResult.WithParsed<Options>(options =>
                    {
                        var writer = host.Services.GetService<IConsoleWriter>();
                        writer.Verbose = options.Verbose;
                    })
                    .WithParsed<TrainOptions>(async options =>
                    {
                        var trainService = host.Services.GetService<ITrainService>();
                        await trainService.TrainAsync(options);
                    })
                    .WithParsed<DownloadOptions>(async options =>
                    {
                        var downloadService = host.Services.GetService<IDownloadService>();
                        await downloadService.DownloadAsync(options.DestinationDirectory);
                    })
                    .WithNotParsed(errors =>
                    {
                        errors = errors as Error[] ?? errors.ToArray();

                        if (errors.Any(e =>
                            e.Tag != ErrorType.HelpRequestedError && e.Tag != ErrorType.HelpVerbRequestedError))
                        {
                            returnCode = ReturnCodes.ParseArgsFailed;
                            Log.ForContext(typeof(Program)).Error("Failed to parse args = {Args}, received errors = {@Errors}.", args, errors);
                        }

                    });

                Console.ReadLine();
                //TODO: error codes:
                return returnCode;
            }
            catch (Exception e)
            {
                Log.ForContext(typeof(Program)).Fatal(e.Message, e);
                return ReturnCodes.UnhandledException;
            }
        }

        static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddTransient<ITrainService, TrainService>();
            services.AddTransient<IDownloadService, DownloadService>();
            services.AddHttpClient();
            services.AddSingleton<IConsoleWriter, ConsoleWriter>();
        }

        static void BuildConfiguration(IConfigurationBuilder builder)
        {
            var env = Environment.GetEnvironmentVariable("BRICKSCANTRAIN_ENVIRONMENT");
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env ?? "Production"}.json", optional: true);
        }
    }
}
