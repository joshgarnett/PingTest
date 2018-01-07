using System;
using System.Threading.Tasks;
using DotNetty.Common.Concurrency;
using DotNetty.Transport.Channels;
using NLog;
using PingCommon.Protocol;

namespace PingClient {
	public class ClientHandler : ChannelHandlerAdapter {
		private static readonly Logger _log = LogManager.GetCurrentClassLogger(typeof(Client));

		private volatile string _data;
		private volatile bool _connected;
		private volatile IChannelHandlerContext _context;
		private readonly TaskCompletionSource _connectedPromise;
		private volatile TaskCompletionSource<bool> _promise;

		public ClientHandler() {
			_connectedPromise = new TaskCompletionSource();
		}
		
		public override void ChannelActive(IChannelHandlerContext context) {
			_log.Info("Connected to the server");
			_connectedPromise.Complete();
			_connected = true;
			_context = context;
		}

		public override void ChannelInactive(IChannelHandlerContext context) {
			_log.Debug("Client Disconnected");
			_connected = false;
		}

		public override void ChannelRead(IChannelHandlerContext context, object message) {
			try {
				var envelope = message as Envelope;
				if (envelope == null) {
					_log.Error("Envelope is null");
					_promise.SetResult(false);
					return;
				}

				if (envelope.Pong == null) {
					_log.Error("Pong is null");
					_promise.SetResult(false);
					return;
				}

				if (envelope.Pong.Data != _data) {
					_log.Error("Invalid pong data returned");
					_promise.SetResult(false);
					return;
				}
				_promise.SetResult(true);
			}
			catch (Exception ex) {
				_promise.SetException(ex);
			}
		}

		public Task ConnectionTask {
			get { return _connectedPromise.Task; }
		}

		public bool IsConnected {
			get { return _connected; }
		}

		public Task<bool> SendMessage() {
			_promise = new TaskCompletionSource<bool>();

			if (!_connected) {
				_log.Error("Client is not connected");
				_promise.SetResult(false);
			}
			else {
				_log.Info("Sending Ping");
				_data = Guid.NewGuid().ToString();
				var envelope = new Envelope {
					Ping = new Ping {Data = _data}
				};
				_context.WriteAndFlushAsync(envelope);
			}

			return _promise.Task;
		}

		public override void ChannelReadComplete(IChannelHandlerContext context) {
			context.Flush();
		}

		public override void ExceptionCaught(IChannelHandlerContext context, Exception exception) {
			_log.Error(exception, "There was an exception in the handler");
			context.CloseAsync();
		}
	}
}