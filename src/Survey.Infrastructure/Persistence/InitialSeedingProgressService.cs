using System.Collections.Concurrent;
using Survey.Application.Models;

namespace Survey.Infrastructure.Persistence;

public sealed class InitialSeedingProgressService
{
	private readonly Lock _sync = new();
	private bool _isRunning;
	private bool _isComplete;
	private string? _errorMessage;
	private long _nextSequence;
	private readonly ConcurrentDictionary<string, InitialSeedingStageSnapshot> _stages = new(StringComparer.Ordinal);
	private readonly List<InitialSeedingActivityEntry> _activityEntries = [];

	public InitialSeedingProgressSnapshot GetSnapshot()
	{
		lock (_sync)
		{
			return new InitialSeedingProgressSnapshot
			{
				IsRunning = _isRunning,
				IsComplete = _isComplete,
				ErrorMessage = _errorMessage,
				Stages = InitialSeedingStages.Ordered
					.Select(stage => _stages.TryGetValue(stage.Key, out var value)
						? CloneStage(value)
						: new InitialSeedingStageSnapshot
						{
							StageKey = stage.Key,
							StageLabel = stage.Label
						})
					.ToArray(),
				ActivityEntries = _activityEntries.ToArray()
			};
		}
	}

	public IReadOnlyList<InitialSeedingActivityEntry> GetActivityEntriesAfter(long lastSequence)
	{
		lock (_sync)
		{
			return _activityEntries
				.Where(entry => entry.Sequence > lastSequence)
				.Select(CloneActivityEntry)
				.ToArray();
		}
	}

	public void Reset()
	{
		lock (_sync)
		{
			_isRunning = false;
			_isComplete = false;
			_errorMessage = null;
			_nextSequence = 0;
			_stages.Clear();
			_activityEntries.Clear();
		}
	}

	public void Start()
	{
		lock (_sync)
		{
			_isRunning = true;
			_isComplete = false;
			_errorMessage = null;
			_nextSequence = 0;
			_stages.Clear();
			_activityEntries.Clear();

			foreach (var stage in InitialSeedingStages.Ordered)
			{
				_stages[stage.Key] = new InitialSeedingStageSnapshot
				{
					StageKey = stage.Key,
					StageLabel = stage.Label
				};
			}
		}
	}

	public void Report(InitialSeedingProgressUpdate update)
	{
		lock (_sync)
		{
			_isRunning = true;
			_errorMessage = null;
			_stages[update.StageKey] = new InitialSeedingStageSnapshot
			{
				StageKey = update.StageKey,
				StageLabel = string.IsNullOrWhiteSpace(update.StageLabel) ? InitialSeedingStages.GetLabel(update.StageKey) : update.StageLabel,
				ActivityMessage = update.ActivityMessage ?? string.Empty,
				Processed = update.Processed,
				Total = update.Total,
				IsStarted = true,
				IsComplete = update.IsComplete
			};

			if (!string.IsNullOrWhiteSpace(update.ActivityMessage))
			{
				_activityEntries.Add(new InitialSeedingActivityEntry
				{
					Sequence = ++_nextSequence,
					StageKey = update.StageKey,
					StageLabel = string.IsNullOrWhiteSpace(update.StageLabel) ? InitialSeedingStages.GetLabel(update.StageKey) : update.StageLabel,
					Message = update.ActivityMessage
				});
			}
		}
	}

	public void Complete()
	{
		lock (_sync)
		{
			_isRunning = false;
			_isComplete = true;
			_errorMessage = null;

			foreach (var stage in InitialSeedingStages.Ordered)
			{
				if (!_stages.TryGetValue(stage.Key, out var existing))
				{
					_stages[stage.Key] = new InitialSeedingStageSnapshot
					{
						StageKey = stage.Key,
						StageLabel = stage.Label,
						Processed = 1,
						Total = 1,
						IsStarted = true,
						IsComplete = true
					};
					continue;
				}

				_stages[stage.Key] = new InitialSeedingStageSnapshot
				{
					StageKey = existing.StageKey,
					StageLabel = existing.StageLabel,
					ActivityMessage = string.Empty,
					Processed = existing.Total > 0 ? existing.Total : Math.Max(existing.Processed, 1),
					Total = existing.Total > 0 ? existing.Total : Math.Max(existing.Processed, 1),
					IsStarted = true,
					IsComplete = true
				};
			}
		}
	}

	public void Fail(string? errorMessage)
	{
		lock (_sync)
		{
			_isRunning = false;
			_isComplete = false;
			_errorMessage = errorMessage;
		}
	}

	private static InitialSeedingStageSnapshot CloneStage(InitialSeedingStageSnapshot stage)
	{
		return new InitialSeedingStageSnapshot
		{
			StageKey = stage.StageKey,
			StageLabel = stage.StageLabel,
			ActivityMessage = stage.ActivityMessage,
			Processed = stage.Processed,
			Total = stage.Total,
			IsStarted = stage.IsStarted,
			IsComplete = stage.IsComplete
		};
	}

	private static InitialSeedingActivityEntry CloneActivityEntry(InitialSeedingActivityEntry entry)
	{
		return new InitialSeedingActivityEntry
		{
			Sequence = entry.Sequence,
			StageKey = entry.StageKey,
			StageLabel = entry.StageLabel,
			Message = entry.Message
		};
	}
}
