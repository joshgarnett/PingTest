using System;
using DotNetty.Transport.Channels;
using PingCommon.Protocol;

namespace PingServer {
	public class ServerHandler : ChannelHandlerAdapter {
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger(typeof(ServerHandler));
		
		public override void ChannelActive(IChannelHandlerContext context) {
			Log.Info("Client connected");
		}

		public override void ChannelRead(IChannelHandlerContext context, object message) {
			if (!(message is Envelope envelope)) {
				Log.Error("Envelope is null");
				return;
			}

			if (envelope.Ping == null) {
				Log.Error("Ping is null");
				return;
			}
			
			Log.Info("Received ping: {0}", envelope.Ping.Data);
			
			var response = new Envelope {
				Pong = new Pong { Data = envelope.Ping.Data }
			};
			
			context.WriteAndFlushAsync(response);
		}

		public override void ChannelInactive(IChannelHandlerContext context) {
			Log.Debug("Client Disconnected");
		}

		public override void ExceptionCaught(IChannelHandlerContext context, Exception exception) {
			Log.Error(exception, "There was an exception in the handler");
			context.CloseAsync();
		}
	}
}
