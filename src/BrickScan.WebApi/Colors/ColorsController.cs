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
using BricklinkSharp.Client;
using BrickScan.Library.Core.Dto;
using BrickScan.Library.Dataset;
using BrickScan.Library.Dataset.Model;
using BrickScan.WebApi.Dataset;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace BrickScan.WebApi.Colors
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ColorsController : ControllerBase
    {
        private readonly IDatasetService _datasetService;
        private readonly ILogger<ColorsController> _logger;

        public ColorsController(IDatasetService datasetService, ILogger<ColorsController> logger)
        {
            _datasetService = datasetService;
            _logger = logger;
        }

        private static bool Validate(ColorDto dto, out string? errorMessage)
        {
            if (string.IsNullOrEmpty(dto.BricklinkColorType))
            {
                errorMessage = $"{nameof(dto.BricklinkColorType)} must not be null or empty.";
                return false;
            }

            if (string.IsNullOrEmpty(dto.BricklinkColorHtmlCode))
            {
                errorMessage = $"{nameof(dto.BricklinkColorHtmlCode)} must not be null or empty.";
                return false;
            }

            if (string.IsNullOrEmpty(dto.BricklinkColorName))
            {
                errorMessage = $"{nameof(dto.BricklinkColorName)} must not be null or empty.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        /// <summary>
        /// Adds a new color to database.
        /// </summary>
        /// <param name="color">Color data transfer object.</param>
        /// <param name="apiVersion">Injected by middleware.</param>
        /// <returns>Created <see cref="DatasetColor"/>.</returns>
        /// <response code="201">Created <see cref="DatasetColor"/>.</response>
        /// <response code="400">Input validation of request body failed.</response>
        /// <response code="409">Conflicting resource already exists.</response>
        /// <response code="500">Internal server error occurred.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(DatasetColor))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        [Authorize(Policy = Policies.RequiresTrustedUser)]
        public async Task<IActionResult> AddDatasetColorAsync([FromBody] ColorDto color, ApiVersion apiVersion)
        {
            if (!Validate(color, out var errorMessage))
            {
                var badRequestCode = 400;
                return new BadRequestObjectResult(new ProblemDetails
                {
                    Detail = errorMessage,
                    Instance = HttpContext.Request.Path,
                    Status = badRequestCode,
                    Title = ReasonPhrases.GetReasonPhrase(badRequestCode),
                    Type = $"https://httpstatuses.com/{badRequestCode}",
                    Extensions = { { "traceId", HttpContext.TraceIdentifier } }
                });
            }

            var existing = await _datasetService.FindColorByBricklinkId(color.BricklinkColorId);

            if (existing != null)
            {
                var conflictCode = 409;

                var message = $"A color with bricklink ID = {color.BricklinkColorId} already exists:" +
                              Environment.NewLine +
                              $"{nameof(existing.Id)} = {existing.Id}," +
                              Environment.NewLine +
                              $"{nameof(existing.BricklinkColorName)} = {existing.BricklinkColorName}," +
                              Environment.NewLine +
                              $"{nameof(existing.BricklinkColorType)} = {existing.BricklinkColorType}.";

                return new BadRequestObjectResult(new ProblemDetails
                {
                    Detail = message,
                    Instance = HttpContext.Request.Path,
                    Status = conflictCode,
                    Title = ReasonPhrases.GetReasonPhrase(conflictCode),
                    Type = $"https://httpstatuses.com/{conflictCode}",
                    Extensions = { { "traceId", HttpContext.TraceIdentifier } }
                });
            }

            var datasetColor = await _datasetService.AddColorAsync(color);

            return CreatedAtRoute(nameof(GetColorByIdAsync),
                new
                {
                    colorId = datasetColor.Id,
                    version = apiVersion.ToString()
                },
                datasetColor);
        }

        /// <summary>
        /// Gets and returns the color for the specified ID.
        /// </summary>
        /// <param name="colorId">The ID of the color to return.</param>
        /// <returns>Retrieved <see cref="DatasetColor"/> or problem details if request failed.</returns>
        /// <response code="200">Returns the retrieved <see cref="DatasetColor"/>.</response>
        /// <response code="404">No <see cref="DatasetColor"/> found for the specified ID.</response>
        /// <response code="500">Internal server error occurred.</response>
        [HttpGet("{colorId:int}", Name = nameof(GetColorByIdAsync))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetColorByIdAsync([FromRoute] int colorId)
        {
            _logger.LogDebug($"Retrieving {nameof(DatasetColor)} for ID = {{ColorId}}.", colorId);

            var datasetColor = await _datasetService.FindColorByIdAsync(colorId);

            if (datasetColor == null)
            {
                _logger.LogWarning($"No {nameof(DatasetColor)} found for ID = {{ColorId}}.", colorId);

                return new NotFoundObjectResult(new ProblemDetails
                {
                    Detail = $"No {nameof(DatasetColor)} found for {nameof(colorId)} = {colorId}.",
                    Instance = HttpContext.Request.Path,
                    Status = 404,
                    Title = ReasonPhrases.GetReasonPhrase(404),
                    Type = "https://httpstatuses.com/404"
                });
            }

            return new OkObjectResult(datasetColor);
        }

        /// <summary>
        /// Gets and returns a list of all <see cref="DatasetColor"/>s.
        /// </summary>
        /// <returns><see cref="DatasetColor"/> list or problem details if request failed.</returns>
        /// <response code="200">List of all <see cref="DatasetColor"/>s.</response>
        /// <response code="500">Internal server error occurred.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DatasetColor[]))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllAsync()
        {
            _logger.LogDebug("Retrieving colors from DB.");

            var colors = await _datasetService.GetColorsAsync();

            _logger.LogDebug("Retrieved #{NumOfColors} colors from DB.", colors.Count);

            return new OkObjectResult(colors);
        }

        /// <summary>
        /// Updates / synchronizes color information with bricklink.
        /// </summary>
        /// <returns>No content response or problem details if request failed.</returns>
        /// <response code="204">Colors updated successfully (no content).</response>
        /// <response code="401">Not authorized.</response>
        /// <response code="500">Internal server error occurred.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        [Route("update-from-bl")]
        [Authorize(Policy = Policies.RequiresAdmin)]
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
                .Except(storedColors, new ColorByBricklinkColorIdComparer())
                .ToList();

            if (newColors.Any())
            {
                await _datasetService.AddColorsAsync(newColors);
            }

            return NoContent();
        }
    }
}
