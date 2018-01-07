using System;
using DotNetty.Transport.Channels;
using PingCommon.Protocol;

namespace PingServer {
	public class ServerHandler : ChannelHandlerAdapter {
		private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger(typeof(ServerHandler));
		
		public override void ChannelActive(IChannelHandlerContext context) {
			_log.Info("Client connected");
		}

		public override void ChannelRead(IChannelHandlerContext context, object message) {
			var envelope = message as Envelope;
			if (envelope == null) {
				_log.Error("Envelope is null");
				return;
			}

			if (envelope.Ping == null) {
				_log.Error("Ping is null");
				return;
			}
			
			_log.Info("Received ping: {0}", envelope.Ping.Data);
			
			var response = new Envelope {
				Pong = new Pong { Data = envelope.Ping.Data }
			};
			
			context.WriteAndFlushAsync(response);
		}

		public override void ChannelInactive(IChannelHandlerContext context) {
			_log.Debug("Client Disconnected");
		}

		public override void ExceptionCaught(IChannelHandlerContext context, Exception exception) {
			_log.Error(exception, "There was an exception in the handler");
			context.CloseAsync();
		}
	}
}
