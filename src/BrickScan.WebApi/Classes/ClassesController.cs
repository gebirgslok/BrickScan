
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
using System.Linq;
using System.Threading.Tasks;
using BrickScan.Library.Core;
using BrickScan.Library.Core.Dto;
using BrickScan.Library.Dataset;
using BrickScan.Library.Dataset.Model;
using BrickScan.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace BrickScan.WebApi.Classes
{
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/[controller]")]
    public class ClassesController : ControllerBase
    {
        private const int MIN_NUM_OF_TRAIN_IMAGES = 5;
        private readonly IDatasetService _datasetService;
        private readonly ILogger<ClassesController> _logger;

        public ClassesController(IDatasetService datasetService, ILogger<ClassesController> logger)
        {
            _datasetService = datasetService;
            _logger = logger;
        }

        private static bool Validate(DatasetClassDto dto, out string? errorMessage)
        {
            if (!dto.Items.Any())
            {
                errorMessage = $"Dataset class must contain at least one item (see {nameof(dto.Items)}).";
                return false;
            }

            if (dto.TrainingImageIds.Count < MIN_NUM_OF_TRAIN_IMAGES)
            {
                errorMessage = $"Dataset class must contain at least {MIN_NUM_OF_TRAIN_IMAGES} of train images ({nameof(dto.TrainingImageIds)}), " +
                           $"but received only {dto.TrainingImageIds.Count}.";
                return false;
            }

            if (dto.Items.Any(i => string.IsNullOrEmpty(i.Number)))
            {
                errorMessage = $"Each dataset class item must contain a valid item number (see {nameof(DatasetItemDto.Number)}), " +
                           "but received at least one item with an empty or Null entry.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private async Task<IActionResult> SubmitDatasetClassForTrustedUser(DatasetClassDto datasetClassDto, ApiVersion apiVersion)
        {
            var numberColorIdPairs =
                datasetClassDto.Items.Select(x => new Tuple<string, int>(x.Number, x.DatasetColorId))
                    .ToList();

            var exisiting = await _datasetService.GetClassByNumberAndColorIdPairsAsync(numberColorIdPairs);
            var userId = HttpContext.User.GetUserId();

            DatasetClass datasetClass;
            if (exisiting != null)
            {
                datasetClass = await _datasetService.AddRequiresMergeClassAsync(datasetClassDto, userId);
            }
            else
            {
                datasetClass = await _datasetService.AddClassifiedClassAsync(datasetClassDto, userId);
            }

            return CreatedAtRoute(nameof(GetClassByIdAsync),
                new
                {
                    classId = datasetClass.Id,
                    version = apiVersion.ToString()
                },
                datasetClass);
        }
        
        /// <summary>
        /// Gets and returns the class for the specified ID.
        /// </summary>
        /// <param name="classId">The ID of the class to return.</param>
        /// <returns>Retrieved <see cref="DatasetClass"/> or problem details if request failed.</returns>
        /// <response code="200">Returns the retrieved <see cref="DatasetClass"/>.</response>
        /// <response code="404">No <see cref="DatasetClass"/> found for the specified ID.</response>
        /// <response code="500">Internal server error occurred.</response>
        [HttpGet("{classId:int}", Name = nameof(GetClassByIdAsync))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DatasetClass))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetClassByIdAsync([FromRoute] int classId)
        {
            _logger.LogDebug($"Retrieving {nameof(DatasetClass)} for ID = {{ClassId}}.", classId);

            var datasetClass = await _datasetService.GetClassByIdAsync(classId);

            if (datasetClass == null)
            {
                _logger.LogWarning($"No {nameof(DatasetClass)} found for ID = {{ClassId}}.", classId);

                return new NotFoundObjectResult(new ProblemDetails
                {
                    Detail = $"No {nameof(DatasetClass)} found for {nameof(classId)} = {classId}.",
                    Instance = HttpContext.Request.Path,
                    Status = 404,
                    Title = ReasonPhrases.GetReasonPhrase(404),
                    Type = "https://httpstatuses.com/404"
                });
            }

            return new OkObjectResult(datasetClass);
        }

        /// <summary>
        /// Submits a new class.
        /// </summary>
        /// <param name="datasetClassDto">Data transfer object which contains all required data of the new class.</param>
        /// <param name="apiVersion">Injected by middleware.</param>
        /// <returns>Created <see cref="DatasetClass"/> or problem details if request failed.</returns>
        /// <response code="201">Returns the created <see cref="DatasetClass"/>.</response>
        /// <response code="400">Input validation of the submitted body failed.</response>
        /// <response code="500">Internal server error occurred.</response>
        [Authorize]
        [HttpPost("submit")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(DatasetClass))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> SubmitClassAsync([FromBody] DatasetClassDto datasetClassDto, [FromRoute] ApiVersion apiVersion)
        {
            if (!Validate(datasetClassDto, out var errorMessage))
            {
                return new BadRequestObjectResult(new ProblemDetails
                {
                    Detail = errorMessage,
                    Instance = HttpContext.Request.Path,
                    Status = 400,
                    Title = ReasonPhrases.GetReasonPhrase(400),
                    Type = "https://httpstatuses.com/400",
                    Extensions = { { "traceId", HttpContext.TraceIdentifier } }
                });
            }

            var isTrustedUser = HttpContext.User.IsTrusted();

            if (isTrustedUser)
            {
                return await SubmitDatasetClassForTrustedUser(datasetClassDto, apiVersion);
            }

            var userId = HttpContext.User.GetUserId();
            var datasetClass = await _datasetService.AddUnclassifiedClassAsync(datasetClassDto, userId);

            return CreatedAtRoute(nameof(GetClassByIdAsync),
                new
                {
                    classId = datasetClass.Id,
                    version = apiVersion.ToString()
                },
                datasetClass);
        }

        /// <summary>
        /// Gets and returns a <c>Class Train Images List</c> for the specified pagination parameters.
        /// </summary>
        /// <param name="page">The page number of the <see cref="DatasetClass"/> table.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns><c>Class Train Images List</c> for the specified pagination parameters or problem details if request fails.</returns>
        /// <response code="200"><c>Class Train Images List</c> was retrieved successfully.</response>
        /// <response code="500">Internal server error occurred.</response>
        [HttpGet("training-images", Name = nameof(GetClassesTrainingImagesListAsync))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<DatasetClassTrainImagesDto>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> GetClassesTrainingImagesListAsync(int page = 1, int pageSize = 200)
        {
            _logger.LogInformation("Retrieving class train images list for page = {Page} and page size = {PageSize}.",
                page, pageSize);

            var map = await _datasetService.GetClassTrainImagesListAsync(page, pageSize);
            return new OkObjectResult(map);
        }

        /// <summary>
        /// Assigns an unconfirmed image as train image to a data set class.
        /// </summary>
        /// <param name="classId">The ID of the class to which the (train) image will be assigned to.</param>
        /// <param name="imageId">The ID of the unconfirmed (train) image to assign.</param>
        /// <returns>The updated <see cref="DatasetImage"/> instance or problem details if request failed.</returns>
        /// <response code="200">Train image was successfully assigned to the class.</response>
        /// <response code="400">No image found for the posted image ID.</response>
        /// <response code="500">Internal server error occurred.</response>
        [HttpPost("{classId:int}/assign-train-image")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DatasetImage))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        [Authorize(Policy = Policies.RequiresTrustedUser)]
        public async Task<IActionResult> AssignTrainImageAsync([FromRoute] int classId, [FromQuery] int imageId)
        {
            _logger.LogDebug("Assigning unclassified image (ID = {ImageId}) for class (ID = {ClassId}).", imageId, classId);

            var result = await _datasetService.ConfirmUnclassififiedImageAsync(imageId, classId);

            if (!result.Success)
            {
                _logger.LogWarning("Received erroneous confirmation result (message: {ErrorMessage})", result.ErrorMessage);

                return result.GetActionResult(HttpContext.Request.Path, HttpContext.TraceIdentifier);
            }

            _logger.LogDebug("Assignment of image (ID = {ImageId}) for class (ID = {ClassId}) successful.", imageId, classId);

            return new OkObjectResult(result.Image);
        }
    }
}