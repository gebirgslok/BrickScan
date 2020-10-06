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
    public class ImagePredictor : IImagePredictor
    {
        private readonly PredictionEnginePool<ModelImageInput, ModelImagePrediction> _predictionEnginePool;
        private readonly IConfiguration _configuration;

        public ImagePredictor(PredictionEnginePool<ModelImageInput, ModelImagePrediction> predictionEnginePool,
            IConfiguration configuration)
        {
            _predictionEnginePool = predictionEnginePool;
            _configuration = configuration;
        }

        private void FilterScoredLabels(List<ScoredLabel> scoredLabels)
        {
            if (scoredLabels.Count < 2)
            {
                return;
            }

            var minScoreThreshold = _configuration.GetValue<double>("Prediction:MinScoreThreshold");
            var uniqueItemScoreDifference = _configuration.GetValue<double>("Prediction:UniqueItemScoreDifference");

            if (scoredLabels[0].Score - scoredLabels[1].Score > uniqueItemScoreDifference)
            {
                scoredLabels.RemoveRange(1, scoredLabels.Count - 1);
            }

            for (var i = scoredLabels.Count - 1; i >= 0 ; i--)
            {
                if (scoredLabels[i].Score < minScoreThreshold)
                {
                    scoredLabels.RemoveAt(i);
                }
            }
        }

        private static List<ScoredLabel> GetLabelsWithScoresSorted(DataViewSchema schema, string name, IReadOnlyList<float> scores, int n)
        {
            var topScoresWithIndexes = scores.Select((s, i) => new {index = i, score = s})
                .OrderByDescending(x => x.score)
                .Take(n);

            var column = schema.GetColumnOrNull(name);
            var slotNames = new VBuffer<ReadOnlyMemory<char>>();

            if (column == null)
            {
                throw new InvalidOperationException("schema.GetColumnOrNull(name) returned Null.");
            }

            column.Value.GetSlotNames(ref slotNames);

            var arr = slotNames.DenseValues().ToArray();
            return topScoresWithIndexes
                .Select(x => new ScoredLabel(arr[x.index].ToString(), x.score))
                .ToList();
        }

        public List<ScoredLabel> Predict(byte[] image)
        {
            var predicationEngine = _predictionEnginePool.GetPredictionEngine("BrickScanModel");

            var output = predicationEngine.Predict(new ModelImageInput(image));
            var maxPredictionMatches = _configuration.GetValue<int>("Prediction:MaxNumOfMatches");

            var scoredLabels = GetLabelsWithScoresSorted(predicationEngine.OutputSchema,
                nameof(output.Score),
                output.Score,
                maxPredictionMatches);

            FilterScoredLabels(scoredLabels);

            return scoredLabels;
        }
    }
}