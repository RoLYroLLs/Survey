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
	private readonly List<InitialSeedingPlaybackEntry> _playbackEntries = [];

	public event Action? Updated;

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
					.ToArray()
			};
		}
	}

	public IReadOnlyList<InitialSeedingPlaybackEntry> GetPlaybackEntriesAfter(long lastSequence)
	{
		lock (_sync)
		{
			return _playbackEntries
				.Where(entry => entry.Sequence > lastSequence)
				.Select(ClonePlaybackEntry)
				.ToArray();
		}
	}

	public long GetLatestPlaybackSequence()
	{
		lock (_sync)
		{
			return _nextSequence;
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
			_playbackEntries.Clear();
		}

		Updated?.Invoke();
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
			_playbackEntries.Clear();

			foreach (var stage in InitialSeedingStages.Ordered)
			{
				_stages[stage.Key] = new InitialSeedingStageSnapshot
				{
					StageKey = stage.Key,
					StageLabel = stage.Label
				};
			}
		}

		Updated?.Invoke();
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

			_playbackEntries.Add(new InitialSeedingPlaybackEntry
			{
				Sequence = ++_nextSequence,
				StageKey = update.StageKey,
				StageLabel = string.IsNullOrWhiteSpace(update.StageLabel) ? InitialSeedingStages.GetLabel(update.StageKey) : update.StageLabel,
				ActivityMessage = update.ActivityMessage ?? string.Empty,
				Processed = update.Processed,
				Total = update.Total,
				IsComplete = update.IsComplete
			});
		}

		Updated?.Invoke();
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
					ActivityMessage = existing.ActivityMessage,
					Processed = existing.Total > 0 ? existing.Total : Math.Max(existing.Processed, 1),
					Total = existing.Total > 0 ? existing.Total : Math.Max(existing.Processed, 1),
					IsStarted = true,
					IsComplete = true
				};
			}
		}

		Updated?.Invoke();
	}

	public void Fail(string? errorMessage)
	{
		lock (_sync)
		{
			_isRunning = false;
			_isComplete = false;
			_errorMessage = errorMessage;
		}

		Updated?.Invoke();
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

	private static InitialSeedingPlaybackEntry ClonePlaybackEntry(InitialSeedingPlaybackEntry entry)
	{
		return new InitialSeedingPlaybackEntry
		{
			Sequence = entry.Sequence,
			StageKey = entry.StageKey,
			StageLabel = entry.StageLabel,
			ActivityMessage = entry.ActivityMessage,
			Processed = entry.Processed,
			Total = entry.Total,
			IsComplete = entry.IsComplete
		};
	}
}
