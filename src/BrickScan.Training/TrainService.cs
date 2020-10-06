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
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Vision;

namespace BrickScan.Training
{
    static class DefaultColumnNames
    {
        public static string Label = "Label";
        public static string PredictedLabel = "PredictedLabel";
        public static string ImagePath = "ImagePath";
        public static string LabelAsKey = "LabelAsKey";
    }

    public class TrainService : ITrainService
    {
        private readonly ILogger<TrainService> _logger;
        private readonly IConsoleWriter _writer;
        private readonly IDownloadService _downloadService;

        public TrainService(ILogger<TrainService> logger, 
            IConsoleWriter writer, 
            IDownloadService downloadService)
        {
            _logger = logger;
            _writer = writer;
            _downloadService = downloadService;
        }

        private void GenerateTsvFile(string datasetDirectory)
        {
            var builder = new StringBuilder();
            builder.AppendLine(DefaultColumnNames.Label + '\t' + DefaultColumnNames.ImagePath);

            _writer.WriteLine("Generating classes-images tsv file...");

            var files = Directory.EnumerateFiles(datasetDirectory, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".png") || f.EndsWith(".jpeg") || f.EndsWith(".jpg"))
                .ToArray();
            
            _writer.WriteLine($"Discovered {files.Length} image files used for training.");


            foreach (var file in files)
            {
                var parentDir = Directory.GetParent(file).Name;

                if (parentDir == Constants.BottleneckDirectory || !int.TryParse(parentDir, out var classId))
                {
                    continue;
                }

                builder.AppendLine(classId.ToString() + '\t' + file);
            }

            File.WriteAllText(Path.Combine(datasetDirectory, Constants.TsvFileName), builder.ToString());
        }

        private static void SaveModel(MLContext mlContext, ITransformer mlModel, DataViewSchema schema)
        {
            Console.WriteLine("Saving ML model...");
            var modelSavePath = Path.Combine(Directory.GetCurrentDirectory(), $"MlModel_{DateTime.Now:yyyyMmdd-HHmmss}.zip");
            mlContext.Model.Save(mlModel, schema, modelSavePath);
            Console.WriteLine("The model is saved to {0}.", modelSavePath);
        }

        private void PrintMetrics(MulticlassClassificationMetrics metrics)
        {
            _writer.WriteLine($"Macro accuracy: {metrics.MacroAccuracy}.");
            _writer.WriteLine($"Micro accuracy: {metrics.MicroAccuracy}.");
            _writer.WriteLine($"Log loss: {metrics.LogLoss}.");
            _writer.WriteLine($"Log loss reduction: {metrics.LogLossReduction}.");
            _writer.WriteLine($"Top K (K = {metrics.TopKPredictionCount}) accuracy: {metrics.TopKAccuracy}.");
        }

        public async Task TrainAsync(TrainOptions options)
        {
            if (options.DownloadDataset)
            {
                await _downloadService.DownloadAsync(options.DatasetDirectory);
            }

            GenerateTsvFile(options.DatasetDirectory);

            _writer.WriteLine("Starting training ...");

            var mlContext = new MLContext();
            var tsvFilePath = Path.Combine(options.DatasetDirectory, Constants.TsvFileName);
            
            var data = mlContext.Data.LoadFromTextFile<ImageData>(tsvFilePath, hasHeader:true);

            var loadPipeline = mlContext.Transforms.Conversion
                .MapValueToKey(inputColumnName: DefaultColumnNames.Label, outputColumnName: DefaultColumnNames.LabelAsKey)
                .Append(mlContext.Transforms.LoadRawImageBytes(outputColumnName: "Image",
                    imageFolder: options.DatasetDirectory,
                    inputColumnName: DefaultColumnNames.ImagePath));

            var preProcessedData = loadPipeline.Fit(data).Transform(data);
            var trainSplit = mlContext.Data.TrainTestSplit(preProcessedData, 0.3);
            var validationTestSplit = mlContext.Data.TrainTestSplit(trainSplit.TestSet);
            var trainSet = trainSplit.TrainSet;
            var validationSet = validationTestSplit.TrainSet;
            var testSet = validationTestSplit.TestSet;

            var classifierOptions = new ImageClassificationTrainer.Options
            {
                FeatureColumnName = "Image",
                LabelColumnName = DefaultColumnNames.LabelAsKey,
                ValidationSet = validationSet,
                Arch = ImageClassificationTrainer.Architecture.ResnetV250,
                MetricsCallback = m => _writer.WriteLine(m.ToString()),
                TestOnTrainSet = false,
                ReuseTrainSetBottleneckCachedValues = options.ReuseBottleneckCachedValues,
                ReuseValidationSetBottleneckCachedValues = options.ReuseBottleneckCachedValues,
                WorkspacePath = Path.Combine(options.DatasetDirectory, Constants.BottleneckDirectory),
                BatchSize = 20,
                Epoch = options.NumOfEpochs,
                LearningRateScheduler = new ExponentialLRDecay(0.1f),
                EarlyStoppingCriteria = new ImageClassificationTrainer.EarlyStopping(0.001f, 40)
            };

            var trainingPipeline = mlContext
                .MulticlassClassification
                .Trainers
                .ImageClassification(classifierOptions)
                .Append(mlContext.Transforms.Conversion.MapKeyToValue(DefaultColumnNames.PredictedLabel));

            var trainedModel = trainingPipeline.Fit(trainSet);

            _writer.WriteLine("Evaluating test set...");
            var testSetPredictions = trainedModel.Transform(testSet);

            var metrics = mlContext
                .MulticlassClassification.Evaluate(testSetPredictions, 
                    topKPredictionCount: 3, 
                    labelColumnName: DefaultColumnNames.LabelAsKey);

            _writer.WriteLine("Test set metrics:");
            PrintMetrics(metrics);

            var predictionEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(trainedModel);
            SaveModel(mlContext, trainedModel, predictionEngine.OutputSchema);
            _writer.WriteLine("Training finished. WOW");
        }
    }
}