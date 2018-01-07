using System;
using System.Net.Sockets;
using Google.Protobuf;
using NLog;
using PingCommon.Protocol;

namespace PingClientSocket {
	internal class Program {
		private const int Port = 5000;
		private static readonly Logger _log = LogManager.GetCurrentClassLogger(typeof(Program));

		public void StartClient(string host, int port) {
			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Connect(host, port);
			using (var stream = new NetworkStream(socket)) {
				for (;;) {
					var data = Guid.NewGuid().ToString();
					var envelope = new Envelope {
						Ping = new Ping {Data = data}
					};

					envelope.WriteDelimitedTo(stream);
					var response = Envelope.Parser.ParseDelimitedFrom(stream);
					if (response != null && response.Pong != null) {
						_log.Info("Received Pong");
					}
					else {
						_log.Error("Did not receive Pong");
						break;
					}
				}
			}
		}

		public static void Main(string[] args) {
			for (;;) {
				try {
					var client = new Program();
					client.StartClient(args[0], Port);
				}
				catch (Exception ex) {
					_log.Error("Exception in client: {0}", ex.Message);
				}
			}
		}
	}
}