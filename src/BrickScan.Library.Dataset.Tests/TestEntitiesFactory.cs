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
using BrickScan.Library.Dataset.Model;

namespace BrickScan.Library.Dataset.Tests
{
    internal static class TestEntitiesFactory
    {
        internal static DatasetImage[] CreateRandomDatasetImages(int count, string? urlTemplate = null,
            EntityStatus status = EntityStatus.Unclassified)
        {
            var images = new DatasetImage[count];

            for (var i = 0; i < count; i++)
            {
                images[i] = CreateRandomDatasetImage(string.IsNullOrEmpty(urlTemplate)
                        ? $"https://www.foo.bar/img{i}.png"
                        : string.Format(urlTemplate, i), 
                    status);
            }

            return images;
        }

        internal static DatasetImage CreateRandomDatasetImage(string? url = null, EntityStatus status = EntityStatus.Unclassified)
        {
            return new DatasetImage
            {
                CreatedOn = DateTime.Now,
                Status = status,
                Url = string.IsNullOrEmpty(url) ? "https://www.foo.bar/img1.png" : url
            };
        }

        internal static DatasetColor CreateRandomDatasetColor()
        {
            return new DatasetColor
            {
                BricklinkColorName = "White",
                BricklinkColorType = "Solid",
                BricklinkColorHtmlCode = "#ffffffff",
                BricklinkColorId = 1
            };
        }
    }
}