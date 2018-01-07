using System.Threading.Tasks;
using DotNetty.Codecs.Protobuf;
using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Mono.Unix;
using Mono.Unix.Native;
using NLog;
using NLog.Extensions.Logging;
using PingCommon.Protocol;

namespace PingServer {
	internal class Server {
		private const int Port = 5000;
		private static readonly Logger _log = LogManager.GetCurrentClassLogger(typeof(Server));
		
		private IChannel _channel;

		public async Task StartServer(int port) {
			InternalLoggerFactory.DefaultFactory.AddProvider(new NLogLoggerProvider());
			
			_log.Info("Waiting 15s before starting the server");
			await Task.Delay(15000);

			var bossGroup = new MultithreadEventLoopGroup(1);
			var workerGroup = new MultithreadEventLoopGroup();

			var bootstrap = new ServerBootstrap();
			bootstrap
				.Group(bossGroup, workerGroup)
				.Channel<TcpServerSocketChannel>()
				.Handler(new LoggingHandler(typeof(Server).FullName))
				.ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel => {
					IChannelPipeline pipeline = channel.Pipeline;
					pipeline.AddLast(new ProtobufVarint32FrameDecoder());
					pipeline.AddLast(new ProtobufDecoder(Envelope.Parser));
					pipeline.AddLast(new ProtobufVarint32LengthFieldPrepender());
					pipeline.AddLast(new ProtobufEncoder());

					pipeline.AddLast(new ServerHandler());
				}));

			_channel = await bootstrap.BindAsync(port);
		}

		public void Stop() {
			_channel?.CloseAsync();
		}
		
		public static void Main(string[] args) {
			_log.Info("Starting server on port {0}", Port);
			
			var server = new Server();
			server.StartServer(Port).Wait();
			
			// Use Unix Signals
			UnixSignal.WaitAny(GetUnixTerminationSignals());
			server.Stop();

			_log.Info("Shutting down server");
		}
		
		private static UnixSignal[] GetUnixTerminationSignals() {
			return new[] {
				new UnixSignal(Signum.SIGINT),
				new UnixSignal(Signum.SIGTERM),
				new UnixSignal(Signum.SIGQUIT),
				new UnixSignal(Signum.SIGHUP)
			};
		}
	}
}
