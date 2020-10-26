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
using System.Threading.Tasks;
using BricklinkSharp.Client;
using BrickScan.Library.Core.Dto;
using BrickScan.Library.Dataset;
using BrickScan.Library.Dataset.Model;
using BrickScan.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BrickScan.WebApi.Dataset
{
    //TODO: XML doc
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class DatasetController : ControllerBase
    {
        private const int MIN_NUM_OF_TRAIN_IMAGES = 2;
        private readonly IDatasetService _datasetService;
        private readonly ILogger<DatasetController> _logger;

        public DatasetController(IDatasetService datasetService, ILogger<DatasetController> logger)
        {
            _datasetService = datasetService;
            _logger = logger;
        }

        private static bool Validate(DatasetClassDto dto, out List<string> errors)
        {
            errors = new List<string>();

            if (!dto.Items.Any())
            {
                errors.Add($"Dataset class must contain at least one item (see {nameof(dto.Items)}).");
            }

            if (dto.TrainingImageIds.Count < MIN_NUM_OF_TRAIN_IMAGES)
            {
                errors.Add($"Dataset class must contain at least {MIN_NUM_OF_TRAIN_IMAGES} of train images ({nameof(dto.TrainingImageIds)}), " +
                           $"but received only {dto.TrainingImageIds.Count}.");
            }

            if (dto.Items.Any(i => string.IsNullOrEmpty(i.Number)))
            {
                errors.Add($"Each dataset class item must contain a valid item number (see {nameof(DatasetItemDto.Number)}), " +
                           "but received at least one item with an empty or Null entry.");
            }

            return !errors.Any();
        }

        private static bool Validate(ColorDto dto, out List<string> errors)
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

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("colors")]
        public async Task<IActionResult> AddDatasetColor([FromBody] ColorDto color)
        {
            if (Validate(color, out var errors))
            {
                return new BadRequestObjectResult(new ApiResponse(400, errors: errors));
            }

            await _datasetService.AddColorAsync(color);
            return new CreatedResult("", new ApiResponse(201, data: "foo"));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("colors")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDatasetColors()
        {
            _logger.LogDebug("Retrieving colors from DB.");

            var colors = await _datasetService.GetColorsAsync();

            _logger.LogDebug("Retrieved {NumOfColors} from DB.", colors.Count);

            return new OkObjectResult(new ApiResponse(200, data: colors));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("colors/update")]
        public async Task<IActionResult> UpdateDatasetColorsFromBricklink()
        {
            BricklinkClientConfiguration.Instance.TokenValue = "2EBD09FD75BE4A3BBA9E556B297D0CAF";
            BricklinkClientConfiguration.Instance.TokenSecret = "95F3584C389340CBB20965839D1B737D";
            BricklinkClientConfiguration.Instance.ConsumerKey = "67BD8A6AD39E441D855DBDA9613DBE0B";
            BricklinkClientConfiguration.Instance.ConsumerSecret = "E36EADA87AC342D2AFD345E2769FD31A";

            var client = BricklinkClientFactory.Build();
            var blColorList = await client.GetColorListAsync();
            var blColors = blColorList.Select(blColor => new DatasetColor
            {
                BricklinkColorId = blColor.ColorId,
                BricklinkColorName = blColor.Name,
                BricklinkColorType = blColor.Type,
                BricklinkColorHtmlCode = blColor.HtmlCode.StartsWith("#") ? blColor.HtmlCode : $"#{blColor.HtmlCode}"
            });

            var storedColors = await _datasetService.GetColorsAsync();

            var newColors = blColors
                .Except(storedColors, new DatasetByColorComparer())
                .ToList();

            if (newColors.Any())
            {
                await _datasetService.AddColorsAsync(newColors);
            }

            return NoContent();
        }

        //TODO: XML DOC
        [HttpPatch("images/confirm")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConfirmDatasetImage([FromQuery] int imageId,
            [FromQuery] int classId)
        {
            _logger.LogDebug("Confirming unclassified image (ID = {ImageId}) for class (ID = {ClassId}).", imageId, classId);

            var result = await _datasetService.ConfirmUnclassififiedImageAsync(imageId, classId);

            if (!result.Success)
            {
                _logger.LogWarning("Received erroneous confirmation result ({@Errors})", result.Errors);

                return new BadRequestObjectResult(new ApiResponse(StatusCodes.Status400BadRequest, errors: result.Errors));
            }

            _logger.LogDebug("Confirmation of image (ID = {ImageId}) for class (ID = {ClassId}) successful.", imageId, classId);

            return new OkObjectResult(new ApiResponse(StatusCodes.Status200OK, data: result.Image));
        }

        //TODO: XML doc
        [HttpGet("classes/{id:int}", Name = nameof(GetDatasetClass))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDatasetClass([FromRoute] int id)
        {
            var datasetClass = await _datasetService.GetClassByIdAsync(id);

            if (datasetClass == null)
            {
                var errors = new List<string>
                {
                    $"No {nameof(DatasetClass)} found for {nameof(id)} = {id}."
                };
                return new NotFoundObjectResult(new ApiResponse(404, errors: errors));
            }

            return new OkObjectResult(new ApiResponse(200, data: datasetClass));
        }

        //TODO: XML Doc
        [HttpGet("classes/training-images", Name = nameof(GetDatasetTrainingImagesList))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDatasetTrainingImagesList(int page = 1, int pageSize = 200)
        {
            _logger.LogInformation("Retrieving class train images list for page = {Page} and page size = {PageSize}.",
                page, pageSize);

            var map = await _datasetService.GetClassTrainImagesListAsync(page, pageSize);
            return new OkObjectResult(new ApiResponse(200, data: map));
        }

        private async Task<IActionResult> SubmitDatasetClassForTrustedUser(DatasetClassDto datasetClassDto, ApiVersion apiVersion)
        {
            var numberColorIdPairs =
                datasetClassDto.Items.Select(x => new Tuple<string, int>(x.Number, x.DatasetColorId))
                    .ToList();

            var exisiting = await _datasetService.GetClassByNumberAndColorIdPairsAsync(numberColorIdPairs);

            var userName = HttpContext.User.GetUserName();
            DatasetClass datasetClass;
            if (exisiting != null)
            {
                datasetClass = await _datasetService.AddRequiresMergeClassAsync(datasetClassDto, userName);
            }
            else
            {
                datasetClass = await _datasetService.AddClassifiedClassAsync(datasetClassDto, userName);
            }

            return CreatedAtRoute(nameof(GetDatasetClass),
                new
                {
                    id = datasetClass?.Id ?? -1,
                    version = apiVersion.ToString()
                },
                new ApiResponse(201, data: datasetClass));
        }

        //TODO: XML DOC
        [HttpPost("classes/submit")]
        [ProducesResponseType(201)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SubmitDatasetClass([FromBody] DatasetClassDto datasetClassDto, ApiVersion apiVersion)
        {
            if (!Validate(datasetClassDto, out var errors))
            {
                return new BadRequestObjectResult(new ApiResponse(400, errors: errors));
            }

            var isTrustedUser = HttpContext.User.IsTrusted();

            if (isTrustedUser)
            {
                return await SubmitDatasetClassForTrustedUser(datasetClassDto, apiVersion);
            }

            var userName = HttpContext.User.GetUserName();
            var datasetClass = await _datasetService.AddUnclassifiedClassAsync(datasetClassDto, userName);

            return CreatedAtRoute(nameof(GetDatasetClass),
                new
                {
                    id = datasetClass.Id,
                    version = apiVersion.ToString()
                },
                new ApiResponse(201, data: datasetClass));
        }
    }
}
