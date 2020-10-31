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

        ////TODO: keep this snippet.
        //static async Task Temp()
        //{
        //    var httpClient = new HttpClient { BaseAddress = new Uri("https://brickscan-app-prod-001.azurewebsites.net/api/v1/") };
        //    const string dir = @"D:\Baygent\Pictures\BrickScan";
        //    const string destDir = @"E:\dataset_images_downloaded";

        //    var response = await httpClient.GetAsync("dataset/colors");
        //    var responseString = await response.Content.ReadAsStringAsync();
        //    var json = JObject.Parse(responseString);
        //    var colors = json["data"]?.ToObject<List<ColorDto>>() ?? new List<ColorDto>();

        //    var dirs = Directory.EnumerateDirectories(dir).ToArray();

        //    var count = 0;
        //    foreach (var d in dirs)
        //    {
        //        var partNoColor = Path.GetFileName(d).Split('_');
        //        var partNo = partNoColor[0];
        //        var colorStr = partNoColor[1];

        //        Console.WriteLine($"Adding item #{++count} / {dirs.Length}:");
        //        Console.WriteLine($"Adding item (part no = {partNo} / color = {colorStr}...");

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
        //        var classResponseString = await postResponse.Content.ReadAsStringAsync();
        //        var classJson = JObject.Parse(classResponseString);
        //        var classId = classJson["data"]["id"].ToObject<int>();

        //        Console.WriteLine($"Successfully added class (ID = {classId}) to Azure...");

        //        for (var index = 0; index < trainImages.Count; index++)
        //        {
        //            DatasetImageDto datasetImageDto = trainImages[index];
        //            var filename = Path.GetFileName(new Uri(datasetImageDto.Url).LocalPath);
        //            var destClassDir = Path.Combine(destDir, classId.ToString("D6"));
        //            var destFilename = Path.Combine(destClassDir, filename);
        //            var file = files[index];

        //            Directory.CreateDirectory(destClassDir);
        //            File.Copy(file, destFilename, true);
        //        }

        //        Console.WriteLine("Copied all images to local drive.");
        //        Console.WriteLine();
        //    }
        //}

        static int Main(string[] args)
        {
            //await Temp();
            //return 0;

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
