﻿using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Ch9
{
	public class TaskNotifier<TResult> : ITaskNotifier<TResult>
	{
		private readonly Action<Exception> _onFaulted;
		private readonly TaskScheduler _dispatcherTaskScheduler;

		public TaskNotifier(
			Task<TResult> task, 
			Action<Exception> onFaulted = null, 
			TaskScheduler dispatcherTaskScheduler = null
		)
		{
			_dispatcherTaskScheduler = dispatcherTaskScheduler;
			_onFaulted = onFaulted;

			Task = task;

			if (!task.IsCompleted)
			{
				RunTask(task);
			}
		}

		/// <inheritdoc />
		public event PropertyChangedEventHandler PropertyChanged;

		/// <inheritdoc />
		public Task<TResult> Task { get; }

		/// <inheritdoc />
		public TResult Result => Task.Status == TaskStatus.RanToCompletion
					? Task.Result
					: default;

		/// <inheritdoc />
		public TaskStatus Status => Task.Status;

		/// <inheritdoc />
		public bool IsCanceled => Task.IsCanceled;

		/// <inheritdoc />
		public bool IsCompleted => Task.IsCompleted;

		/// <inheritdoc />
		public bool IsExecuting => !Task.IsCompleted;

		/// <inheritdoc />
		public bool IsFaulted => Task.IsFaulted;

		/// <inheritdoc />
		public bool IsSuccess => Task.Status == TaskStatus.RanToCompletion;

		/// <inheritdoc />
		public bool IsInternetFaulted { get; set; }

		/// <inheritdoc />
		public AggregateException Exception => Task.Exception;

		/// <inheritdoc />
		Task ITaskNotifier.Task => Task;

		/// <inheritdoc />
		object ITaskNotifier.Result => Result;

		private static TaskScheduler GetDefaultScheduler()
		{
			return SynchronizationContext.Current == null
				? TaskScheduler.Current
				: TaskScheduler.FromCurrentSynchronizationContext();
		}

		private void RunTask(Task taskToExecute)
		{
			taskToExecute.ContinueWith(
				task =>
				{
					if (task.IsFaulted)
					{
#if !__WASM__
						if (Connectivity.NetworkAccess != NetworkAccess.Internet)
						{
							IsInternetFaulted = true;
							PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInternetFaulted)));
						}
#endif
						Console.Error.WriteLine(task.Exception);
						_onFaulted?.Invoke(task.Exception);
					}

#if WINDOWS_UWP
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
#else
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Result)));
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCanceled)));
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCompleted)));
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExecuting)));
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFaulted)));
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSuccess)));
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Exception)));
#endif
				},
				scheduler: _dispatcherTaskScheduler ?? GetDefaultScheduler());
		}
	}
}