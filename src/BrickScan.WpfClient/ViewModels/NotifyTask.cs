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
            catch
            {
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
