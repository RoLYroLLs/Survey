using System.Threading.Channels;

namespace Survey.Infrastructure.Services;

public sealed class InitialSetupTaskQueue
{
	private readonly Channel<InitialSetupWorkItem> _channel = Channel.CreateUnbounded<InitialSetupWorkItem>(new UnboundedChannelOptions
	{
		SingleReader = true,
		SingleWriter = false,
		AllowSynchronousContinuations = false
	});

	public ValueTask QueueAsync(InitialSetupWorkItem workItem, CancellationToken cancellationToken)
	{
		return _channel.Writer.WriteAsync(workItem, cancellationToken);
	}

	public ValueTask<InitialSetupWorkItem> DequeueAsync(CancellationToken cancellationToken)
	{
		return _channel.Reader.ReadAsync(cancellationToken);
	}
}
