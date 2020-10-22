﻿#region License
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using BrickScan.Library.Core.Dto;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        //private static async Task<List<DatasetImageDto>> PostImagesAsync(HttpClient client,
        //    IEnumerable<string> images, 
        //    string filenameTemplate = "image[{0}].png")
        //{
        //    var disposableContents = new List<ByteArrayContent>();
        //    try
        //    {
        //        var formData = new MultipartFormDataContent();
        //        var count = 0;
        //        foreach (var file in images)
        //        {
        //            var content = new ByteArrayContent(await File.ReadAllBytesAsync(file));
        //            content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        //            disposableContents.Add(content);
        //            formData.Add(content, "images", string.Format(filenameTemplate, count.ToString()));
        //            count++;
        //        }

        //        var response = await client.PostAsync("images", formData);
        //        var responseString = await response.Content.ReadAsStringAsync();
        //        var jsonObj = JObject.Parse(responseString);
        //        var jsonData = jsonObj["data"];

        //        if (jsonData == null)
        //        {
        //            throw new InvalidOperationException(
        //                "Received JSON response (for request POST /images) did not contain a 'data' object.");
        //        }

        //        var data = jsonData.ToObject<List<DatasetImageDto>>();
        //        return data;
        //    }
        //    catch (Exception e)
        //    {
        //        //TODO: log exception!
        //        return new List<DatasetImageDto>();
        //    }
        //    finally
        //    {
        //        foreach (var content in disposableContents)
        //        {
        //            content.Dispose();
        //        }
        //    }
        //}

        //TODO: keep this snippet.
        //static async Task Temp()
        //{
        //    var httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:5001/api/v1/") };
        //    const string dir = @"D:\Baygent\Pictures\BrickScan"; 

        //    var response = await httpClient.GetAsync("dataset/colors");
        //    var responseString = await response.Content.ReadAsStringAsync();
        //    var json = JObject.Parse(responseString);
        //    var colors = json["data"]?.ToObject<List<ColorDto>>() ?? new List<ColorDto>();

        //    var dirs = Directory.EnumerateDirectories(dir);

        //    foreach (var d in dirs)
        //    {
        //        var partNoColor = Path.GetFileName(d).Split('_');
        //        var partNo = partNoColor[0];
        //        var colorStr = partNoColor[1];
        //        var color = (PartColor)Enum.Parse(typeof(PartColor), colorStr, true);
        //        var blColorId = (int)color;

        //        var files = Directory.GetFiles(d, "*", SearchOption.TopDirectoryOnly);

        //        var trainImages = await PostImagesAsync(httpClient, files);
        //        var datasetClass = new DatasetClassDto();

        //        datasetClass.TrainingImageIds.AddRange(trainImages.Select(img => img.Id));
        //        datasetClass.DisplayImageIds.Add(datasetClass.TrainingImageIds.First());

        //        var item = new DatasetItemDto
        //        {
        //            Number = partNo,
        //            AdditionalIdentifier = null,
        //            DatasetColorId = colors.First(c => c.BricklinkColorId == blColorId).Id
        //        };

        //        datasetClass.Items.Add(item);

        //        var submitBody = JsonConvert.SerializeObject(datasetClass);
        //        var postResponse = await httpClient.PostAsync("dataset/classes/submit",
        //            new StringContent(submitBody, Encoding.UTF8, "application/json"));
        //    }
        //}

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
                        await downloadService.DownloadAsync(options.DatasetDirectory);
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