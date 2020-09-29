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

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BrickScan.Training
{
    public class TrainService : ITrainService
    {
        private readonly ILogger<TrainService> _logger;
        private readonly IConsoleWriter _writer;
        private readonly IDownloadService _downloadService;

        public TrainService(ILogger<TrainService> logger, 
            IConsoleWriter writer, 
            IDownloadService downloadService)
        {
            _logger = logger;
            _writer = writer;
            _downloadService = downloadService;
        }

        public async Task TrainAsync(TrainOptions options)
        {
            //TODO: Download dataset / API CALL to classes w/ pagination / download images
            await _downloadService.DownloadAsync(options.DestinationDirectory);

            //TODO: Generate TSV file from structure

            //TODO: Run training

            //TODO: Compute stats, write to result file

            _logger.LogInformation("Training with {Parameter}.", "foo");
            _writer.WriteLine("Starting training ...");
            _writer.WriteLineIfVerbose("Verbose message LOL");
            await Task.Delay(1000);
            _writer.WriteLine("Training finished. WOW");
        }
    }
}