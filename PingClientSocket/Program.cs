using System;
using System.Net.Sockets;
using Google.Protobuf;
using NLog;
using PingCommon.Protocol;

namespace PingClientSocket {
	internal class Program {
		private const int Port = 5000;
		private static readonly Logger Log = LogManager.GetCurrentClassLogger(typeof(Program));

		private static void StartClient(string host, int port) {
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
					if (response?.Pong != null) {
						Log.Info("Received Pong");
					}
					else {
						Log.Error("Did not receive Pong");
						break;
					}
				}
			}
		}

		public static void Main(string[] args) {
			for (;;) {
				try {
					StartClient(args[0], Port);
				}
				catch (Exception ex) {
					Log.Error("Exception in client: {0}", ex.Message);
				}
			}
			// ReSharper disable once FunctionNeverReturns
		}
	}
}