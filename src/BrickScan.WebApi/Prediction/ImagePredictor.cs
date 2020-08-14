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
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.ML;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace BrickScan.WebApi.Prediction
{
    internal class ImagePredictor : IImagePredictor
    {
        private readonly PredictionEnginePool<ModelInput, ModelOutput> _predictionEnginePool;
        private readonly IConfiguration _configuration;

        public ImagePredictor(PredictionEnginePool<ModelInput, ModelOutput> predictionEnginePool, 
            IConfiguration configuration)
        {
            _predictionEnginePool = predictionEnginePool;
            _configuration = configuration;
        }

        private static Dictionary<string, float> GetScoresWithLabelsSorted(DataViewSchema schema, string name, IReadOnlyList<float> scores, int n)
        {
            var result = new Dictionary<string, float>();
            var column = schema.GetColumnOrNull(name);
            var slotNames = new VBuffer<ReadOnlyMemory<char>>();

            if (column == null)
            {
                throw new InvalidOperationException("schema.GetColumnOrNull(name) returned Null.");
            }

            column.Value.GetSlotNames(ref slotNames);
            var num = 0;

            foreach (var denseValue in slotNames.DenseValues())
            {
                result.Add(denseValue.ToString(), scores[num++]);
            }

            return result.OrderByDescending(c => c.Value)
                .Take(n)
                .ToDictionary(i => i.Key, i => i.Value);
        }

        public ImagePredictionResult Predict(byte[] image)
        {
            var predicationEngine = _predictionEnginePool.GetPredictionEngine("BrickScanModel");
            var output = predicationEngine.Predict(new ModelInput(image));
            var maxPredictionMatches = _configuration.GetValue<int>("MaxPredictionMatches");

            var scoredLabels = GetScoresWithLabelsSorted(predicationEngine.OutputSchema, 
                nameof(output.Score), 
                output.Score, 
                maxPredictionMatches);

            return new ImagePredictionResult(scoredLabels);
        }
    }
}