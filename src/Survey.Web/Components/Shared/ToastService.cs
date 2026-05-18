namespace Survey.Web.Components.Shared;

public enum ToastSeverity
{
	Success,
	Warning,
	Error
}

public sealed class ToastMessage
{
	public required string Title { get; init; }

	public required string Message { get; init; }

	public ToastSeverity Severity { get; init; }

	public bool AutoDismiss { get; init; } = true;

	public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(5);
}

public sealed class ToastService : IDisposable
{
	private CancellationTokenSource? _dismissal;

	public ToastMessage? Current { get; private set; }

	public event Action? Changed;

	public void ShowSuccess(string message, string title = "Success", int seconds = 5)
	{
		Show(new ToastMessage
		{
			Title = title,
			Message = message,
			Severity = ToastSeverity.Success,
			AutoDismiss = true,
			Duration = TimeSpan.FromSeconds(seconds)
		});
	}

	public void ShowWarning(string message, string title = "Warning", int seconds = 6)
	{
		Show(new ToastMessage
		{
			Title = title,
			Message = message,
			Severity = ToastSeverity.Warning,
			AutoDismiss = true,
			Duration = TimeSpan.FromSeconds(seconds)
		});
	}

	public void ShowError(string message, string title = "Error")
	{
		Show(new ToastMessage
		{
			Title = title,
			Message = message,
			Severity = ToastSeverity.Error,
			AutoDismiss = false,
			Duration = TimeSpan.Zero
		});
	}

	public void Show(ToastMessage message)
	{
		CancelDismissal();
		Current = message;
		NotifyChanged();

		if (!message.AutoDismiss)
		{
			return;
		}

		_dismissal = new CancellationTokenSource();
		_ = DismissAfterDelayAsync(message.Duration, _dismissal.Token);
	}

	public void Dismiss()
	{
		CancelDismissal();
		if (Current is null)
		{
			return;
		}

		Current = null;
		NotifyChanged();
	}

	private async Task DismissAfterDelayAsync(TimeSpan duration, CancellationToken cancellationToken)
	{
		try
		{
			await Task.Delay(duration, cancellationToken);
			if (!cancellationToken.IsCancellationRequested)
			{
				Dismiss();
			}
		}
		catch (TaskCanceledException)
		{
		}
	}

	private void CancelDismissal()
	{
		if (_dismissal is null)
		{
			return;
		}

		_dismissal.Cancel();
		_dismissal.Dispose();
		_dismissal = null;
	}

	private void NotifyChanged()
	{
		Changed?.Invoke();
	}

	public void Dispose()
	{
		CancelDismissal();
	}
}
