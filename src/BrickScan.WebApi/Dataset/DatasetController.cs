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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrickScan.Library.Dataset;
using BrickScan.Library.Dataset.Dto;
using Microsoft.AspNetCore.Mvc;

namespace BrickScan.WebApi.Dataset
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class DatasetController : ControllerBase
    {
        private readonly IDatasetService _datasetService;

        public DatasetController(IDatasetService datasetService)
        {
            _datasetService = datasetService;
        }

        private static bool Validate(DatasetColorDto dto, out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrEmpty(dto.BricklinkColorType))
            {
                errors.Add($"{nameof(dto.BricklinkColorType)} must not be null or empty.");
            }

            if (string.IsNullOrEmpty(dto.BricklinkColorHtmlCode))
            {
                errors.Add($"{nameof(dto.BricklinkColorHtmlCode)} must not be null or empty.");
            }

            if (string.IsNullOrEmpty(dto.BricklinkColorName))
            {
                errors.Add($"{nameof(dto.BricklinkColorName)} must not be null or empty.");
            }

            return !errors.Any();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [Route("colors")]
        public async Task<IActionResult> AddDatasetColor([FromBody] DatasetColorDto color)
        {
            if (Validate(color, out var errors))
            {
                return new BadRequestObjectResult(new ApiResponse(400, errors: errors));
            }

            await _datasetService.AddColorAsync(color);
            return new CreatedResult("", new ApiResponse(201, data: "foo"));
        }

        //public async Task<IActionResult> SubmitDatasetClass([FromBody] SubmitDatasetClassDto datasetClass)
        //{
        //    await Task.Delay(1000);
        //    return new OkObjectResult(new ApiResponse(200, data: "foo"));
        //}
    }
}
