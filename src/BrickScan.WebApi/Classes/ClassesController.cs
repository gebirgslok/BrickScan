using System.Threading.Tasks;
using BrickScan.Library.Dataset;
using BrickScan.Library.Dataset.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BrickScan.WebApi.Classes
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ClassesController : ControllerBase
    {
        private readonly IDatasetService _datasetService;
        private readonly ILogger<ClassesController> _logger;

        public ClassesController(IDatasetService datasetService, ILogger<ClassesController> logger)
        {
            _datasetService = datasetService;
            _logger = logger;
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
        [HttpPost("{classId}/assign-train-image")]
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