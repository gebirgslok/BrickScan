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
using System.Threading.Tasks;
using Serilog;
using Stylet;

namespace BrickScan.WpfClient.ViewModels
{
    internal sealed class NotifyTask<TResult> : PropertyChangedBase
    {
        private readonly ILogger _logger;
        private readonly TResult _defaultResult;

        public Task<TResult> Task { get; }

        public Task TaskCompleted { get; }

        public TResult Result => Task.Status == TaskStatus.RanToCompletion ? Task.Result : _defaultResult;

        public TaskStatus Status => Task.Status;

        public bool IsCompleted => Task.IsCompleted;

        public bool IsNotCompleted => !Task.IsCompleted;

        public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;

        public bool IsCanceled => Task.IsCanceled;

        public bool IsFaulted => Task.IsFaulted;

        public AggregateException? Exception => Task.Exception;

        public Exception? InnerException => Exception?.InnerException;

        public string? ErrorMessage => InnerException?.Message;

        public NotifyTask(Task<TResult> task, TResult defaultResult, ILogger logger)
        {
            _defaultResult = defaultResult;
            _logger = logger;
            Task = task;
            TaskCompleted = MonitorTaskAsync(task);
        }

        private async Task MonitorTaskAsync(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "An occurred while awaiting task. Message: {Message}", exception.Message);
            }
            finally
            {
                NotifyProperties(task);
            }
        }

        private void NotifyProperties(Task task)
        {
            if (task.IsCanceled)
            {
                NotifyOfPropertyChange(nameof(IsCanceled));
            }
            else if (task.IsFaulted)
            {
                NotifyOfPropertyChange(nameof(Exception));
                NotifyOfPropertyChange(nameof(InnerException));
                NotifyOfPropertyChange(nameof(ErrorMessage));
                NotifyOfPropertyChange(nameof(IsFaulted));
            }
            else
            {
                NotifyOfPropertyChange(nameof(IsSuccessfullyCompleted));
                NotifyOfPropertyChange(nameof(Result));
            }

            NotifyOfPropertyChange(nameof(Status));
            NotifyOfPropertyChange(nameof(IsCompleted));
            NotifyOfPropertyChange(nameof(IsNotCompleted));
        }
    }
}
