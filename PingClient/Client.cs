using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Codecs.Protobuf;
using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using NLog;
using NLog.Extensions.Logging;
using PingCommon.Protocol;

namespace PingClient {
	internal class Client {
		private const int Port = 5000;
		private const int ConnectionTimeoutMs = 5000;
		private const int SendTimeoutMs = 5000;
		private static readonly Logger _log = LogManager.GetCurrentClassLogger(typeof(Client));

		public async Task StartClient(string host, int port) {
			InternalLoggerFactory.DefaultFactory.AddProvider(new NLogLoggerProvider());

			var group = new MultithreadEventLoopGroup();

			var handler = new ClientHandler();

			var bootstrap = new Bootstrap();
			bootstrap
				.Group(group)
				.Channel<TcpSocketChannel>()
				.Handler(new LoggingHandler(typeof(Client).FullName))
				.Handler(new ActionChannelInitializer<ISocketChannel>(channel => {
					IChannelPipeline pipeline = channel.Pipeline;
					pipeline.AddLast(new ProtobufVarint32FrameDecoder());
					pipeline.AddLast(new ProtobufDecoder(Envelope.Parser));
					pipeline.AddLast(new ProtobufVarint32LengthFieldPrepender());
					pipeline.AddLast(new ProtobufEncoder());
					pipeline.AddLast(handler);
				}));

			_log.Info("Connecting to {0}:{1}", host, port);
			IChannel bootstrapChannel = await bootstrap.ConnectAsync(host, port);

			var connectionTask = handler.ConnectionTask;
			if (await Task.WhenAny(connectionTask, Task.Delay(ConnectionTimeoutMs)) != connectionTask) {
				_log.Error("Failed to connect to {0}", host);
				return;
			}

			for (;;) {
				if (handler.IsConnected) {
					var task = handler.SendMessage();
					if (await Task.WhenAny(task, Task.Delay(SendTimeoutMs)) == task) {
						if (task.IsFaulted) {
							_log.Error(task.Exception, "Exception sending message");
							break;
						}
						if (task.Result) {
							_log.Info("Pong received");
						}
						else {
							_log.Info("Ping failed");
							break;
						}
					}
					else {
						_log.Error("Pong not received within {0}ms", SendTimeoutMs);
						break;
					}
				}
				else {
					_log.Error("Channel is closed");
					break;
				}
			}

			await bootstrapChannel.CloseAsync();
		}

		public static void Main(string[] args) {
			for (;;) {
				try {
					var client = new Client();
					client.StartClient(args[0], Port).Wait();
				}
				catch (Exception ex) {
					_log.Error("Exception in client: {0}", ex.Message);
				}
			}
		}
	}
}