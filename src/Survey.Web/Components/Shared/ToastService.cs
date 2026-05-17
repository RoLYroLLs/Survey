namespace Survey.Web.Components.Shared;

public enum ToastSeverity
{
	Success,
	Warning,
	Error
}

public sealed class ToastMessage
{
	public Guid Id { get; init; } = Guid.NewGuid();

	public required string Title { get; init; }

	public required string Message { get; init; }

	public ToastSeverity Severity { get; init; }

	public bool AutoDismiss { get; init; } = true;

	public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(5);

	public bool IsClosing { get; internal set; }
}

public sealed class ToastService : IDisposable
{
	private readonly List<ToastMessage> _messages = [];
	private readonly Dictionary<Guid, CancellationTokenSource> _dismissals = [];
	private static readonly TimeSpan ClosingDelay = TimeSpan.FromMilliseconds(220);

	public IReadOnlyList<ToastMessage> CurrentMessages => _messages;

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
		_messages.Add(message);
		NotifyChanged();

		if (!message.AutoDismiss)
		{
			return;
		}

		var dismissal = new CancellationTokenSource();
		_dismissals[message.Id] = dismissal;
		_ = DismissAfterDelayAsync(message.Id, message.Duration, dismissal.Token);
	}

	public void Dismiss(Guid id)
	{
		var message = _messages.FirstOrDefault(item => item.Id == id);
		if (message is null || message.IsClosing)
		{
			return;
		}

		CancelDismissal(id);
		message.IsClosing = true;
		NotifyChanged();
		_ = RemoveAfterClosingDelayAsync(id);
	}

	private async Task DismissAfterDelayAsync(Guid id, TimeSpan duration, CancellationToken cancellationToken)
	{
		try
		{
			await Task.Delay(duration, cancellationToken);
			if (!cancellationToken.IsCancellationRequested)
			{
				Dismiss(id);
			}
		}
		catch (TaskCanceledException)
		{
		}
	}

	private async Task RemoveAfterClosingDelayAsync(Guid id)
	{
		await Task.Delay(ClosingDelay);
		var removed = _messages.RemoveAll(message => message.Id == id) > 0;
		CancelDismissal(id);
		if (removed)
		{
			NotifyChanged();
		}
	}

	private void CancelDismissal(Guid id)
	{
		if (!_dismissals.TryGetValue(id, out var dismissal))
		{
			return;
		}

		dismissal.Cancel();
		dismissal.Dispose();
		_dismissals.Remove(id);
	}

	private void NotifyChanged()
	{
		Changed?.Invoke();
	}

	public void Dispose()
	{
		foreach (var dismissal in _dismissals.Values)
		{
			dismissal.Cancel();
			dismissal.Dispose();
		}

		_dismissals.Clear();
	}
}
