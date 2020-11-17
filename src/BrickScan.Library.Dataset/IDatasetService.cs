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
using System.Threading.Tasks;
using BrickScan.Library.Core;
using BrickScan.Library.Core.Dto;
using BrickScan.Library.Dataset.Model;

namespace BrickScan.Library.Dataset
{
    public interface IDatasetService
    {
        Task<PagedResult<DatasetClassTrainImagesDto>> GetClassTrainImagesListAsync(int page = 1, int pageSize = 200);

        Task<DatasetClass?> GetClassByIdAsync(int id);

        Task<DatasetClass?> GetClassByNumberAndColorIdPairsAsync(List<Tuple<string, int>> numberColorIdPairs);

        Task<DatasetClass> AddUnclassifiedClassAsync(DatasetClassDto datasetClassDto, string? createdByUserId);

        Task<DatasetClass> AddClassifiedClassAsync(DatasetClassDto datasetClassDto, string? createdByUserId);

        Task<DatasetClass> AddRequiresMergeClassAsync(DatasetClassDto datasetClassDto, string? createdByUserId);

        Task<ConfirmUnclassifiedImageResult> ConfirmUnclassififiedImageAsync(int imageId, int classId);

        Task<DatasetImage> AddUnclassifiedImageAsync(ImageData imageData);

        Task<List<DatasetImage>> AddUnclassifiedImagesAsync(List<ImageData> imageDataList);

        Task<DatasetColor> AddColorAsync(ColorDto color);

        Task<List<DatasetColor>> AddColorsAsync(List<DatasetColor> colors);

        Task<DatasetColor?> FindColorByIdAsync(int colorId);

        Task<DatasetColor?> FindColorByBricklinkId(int bricklinkId);

        Task<List<DatasetColor>> GetColorsAsync();

        Task<DatasetImage?> FindImageByIdAsync(int imageId);

        Task<bool> DeleteImageAsync(int imageId);

        Task<List<PredictedDatasetClassDto>> GetPredictedDatasetClassesAsync(List<int> datasetClassesIndexes);
    }
}
