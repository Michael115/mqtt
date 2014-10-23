﻿using System;
using System.IO;
using System.Threading.Tasks;
using Hermes;
using Hermes.Formatters;
using Hermes.Messages;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Tests.Formatters
{
	public class PingRequestFormatterSpec
	{
		private readonly Mock<IChannel<IMessage>> messageChannel;
		private readonly Mock<IChannel<byte[]>> byteChannel;

		public PingRequestFormatterSpec ()
		{
			this.messageChannel = new Mock<IChannel<IMessage>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}
		
		[Theory]
		[InlineData("Files/Packets/PingRequest.packet")]
		public async Task when_reading_ping_request_packet_then_succeeds(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var sentPingRequest = default(PingRequest);

			this.messageChannel
				.Setup (c => c.SendAsync (It.IsAny<IMessage>()))
				.Returns(Task.Delay(0))
				.Callback<IMessage>(m =>  {
					sentPingRequest = m as PingRequest;
				});

			var formatter = new EmptyMessageFormatter<PingRequest> (MessageType.PingRequest, this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.NotNull (sentPingRequest);
		}

		[Theory]
		[InlineData("Files/Packets/PingRequest_Invalid_HeaderFlag.packet")]
		public void when_reading_invalid_ping_request_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new EmptyMessageFormatter<PingRequest>(MessageType.PingRequest, this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Theory]
		[InlineData("Files/Packets/PingRequest.packet")]
		public async Task when_writing_ping_request_packet_then_succeeds(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var expectedPacket = Packet.ReadAllBytes (packetPath);
			var sentPacket = default(byte[]);

			this.byteChannel
				.Setup (c => c.SendAsync (It.IsAny<byte[]>()))
				.Returns(Task.Delay(0))
				.Callback<byte[]>(b =>  {
					sentPacket = b;
				});

			var formatter = new EmptyMessageFormatter<PingRequest> (MessageType.PingRequest, this.messageChannel.Object, this.byteChannel.Object);
			var pingRequest = new PingRequest ();

			await formatter.WriteAsync (pingRequest);

			Assert.Equal (expectedPacket, sentPacket);
		}
	}
}
