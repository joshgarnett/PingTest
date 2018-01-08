using System;
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
		private static readonly Logger Log = LogManager.GetCurrentClassLogger(typeof(Client));

		private static async Task StartClient(Bootstrap bootstrap, string host, int port) {
			Log.Info("Connecting to {0}:{1}", host, port);
			IChannel bootstrapChannel = await bootstrap.ConnectAsync(host, port);
			var handler = (ClientHandler) bootstrapChannel.Pipeline.Last();

			var connectionTask = handler.ConnectionTask;
			if (await Task.WhenAny(connectionTask, Task.Delay(ConnectionTimeoutMs)) != connectionTask) {
				Log.Error("Failed to connect to {0}", host);
				return;
			}

			for (;;) {
				if (handler.IsConnected) {
					var task = handler.SendMessage();
					if (await Task.WhenAny(task, Task.Delay(SendTimeoutMs)) == task) {
						if (task.IsFaulted) {
							Log.Error(task.Exception, "Exception sending message");
							break;
						}
						if (task.Result) {
							Log.Info("Received Pong");
						}
						else {
							Log.Info("Ping failed");
							break;
						}
					}
					else {
						Log.Error("Pong not received within {0}ms", SendTimeoutMs);
						break;
					}
				}
				else {
					Log.Error("Channel is closed");
					break;
				}
			}

			await bootstrapChannel.CloseAsync();
		}

		public static void Main(string[] args) {
			var group = new MultithreadEventLoopGroup();
			InternalLoggerFactory.DefaultFactory.AddProvider(new NLogLoggerProvider());

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
					pipeline.AddLast(new ClientHandler());
				}));


			for (;;) {
				try {
					StartClient(bootstrap, args[0], Port).Wait();
				}
				catch (Exception ex) {
					Log.Error("Exception in client: {0}", ex.Message);
				}
			}
			// ReSharper disable once FunctionNeverReturns
		}
	}
}