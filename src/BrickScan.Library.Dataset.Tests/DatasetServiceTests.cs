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
using System.Linq;
using System.Threading.Tasks;
using BrickScan.Library.Core.Dto;
using BrickScan.Library.Dataset.Model;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BrickScan.Library.Dataset.Tests
{
    public class DatasetServiceTests : SqliteTestBase
    {
        private DatasetService CreateDatasetService(IStorageService? storageService = null, 
            DatasetDbContext? dbContext = null, 
            ILogger<DatasetService>? logger = null)
        {
            return new DatasetService(storageService ?? A.Dummy<IStorageService>(),
                dbContext ?? CreateContext(),
                logger ?? A.Dummy<ILogger<DatasetService>>());
        }

        [Fact]
        public async Task DeleteImageAsync_ImageIdExists()
        {
            var storage = A.Fake<IStorageService>();
            var context = CreateContext();

            var image = TestEntitiesFactory.CreateRandomDatasetImage();
            await context.DatasetImages.AddAsync(image);
            await context.SaveChangesAsync();

            var service = CreateDatasetService(dbContext: context, storageService: storage);

            await service.DeleteImageAsync(image.Id);

            Assert.Equal(0, context.DatasetImages.Count());
            A.CallTo(() => storage.DeleteImageAsync(A<Uri>.That.Matches(x => x.AbsoluteUri == image.Url)))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task AddColorAsync_WithNotExistingColor()
        {
            var service = CreateDatasetService();

            var dto = new ColorDto
            {
                BricklinkColorHtmlCode = "FFFFFFFF",
                BricklinkColorId = 1,
                BricklinkColorName = "Red",
                BricklinkColorType = "Solid"
            };

            var color = await service.AddColorAsync(dto);

            Assert.NotEqual(0, color.Id);
            Assert.Equal(dto.BricklinkColorId, color.BricklinkColorId);
            Assert.Equal(dto.BricklinkColorHtmlCode, color.BricklinkColorHtmlCode);
            Assert.Equal(dto.BricklinkColorName, color.BricklinkColorName);
            Assert.Equal(dto.BricklinkColorType, color.BricklinkColorType);
        }
    }
}