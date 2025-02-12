﻿using QuantumUNET.Messages;
using QuantumUNET.Transport;

namespace QSB.Messaging
{
	public class PlayerMessage : QMessageBase
	{
		/// <summary>
		/// The Player ID that is sending this message
		/// </summary>
		public uint FromId { get; set; }

		/// <summary>
		/// The Player ID that this message is about
		/// </summary>
		public uint AboutId { get; set; }

		/// <summary>
		/// If true, only send this message to the host of the current session 
		/// (OnReceiveLocal/Remote is not called on any other client)
		/// </summary>
		public bool OnlySendToHost { get; set; }

		public override void Deserialize(QNetworkReader reader)
		{
			FromId = reader.ReadUInt32();
			AboutId = reader.ReadUInt32();
			OnlySendToHost = reader.ReadBoolean();
		}

		public override void Serialize(QNetworkWriter writer)
		{
			writer.Write(FromId);
			writer.Write(AboutId);
			writer.Write(OnlySendToHost);
		}
	}
}