﻿using QSB.Events;
using QSB.ItemSync.WorldObjects;
using QSB.Player;
using QSB.Utility;
using QSB.WorldSync;

namespace QSB.ItemSync.Events
{
	internal class SocketItemEvent : QSBEvent<SocketItemMessage>
	{
		public override EventType Type => EventType.SocketItem;

		public override void SetupListener()
			=> GlobalMessenger<int, int, SocketEventType>.AddListener(EventNames.QSBSocketItem, Handler);

		public override void CloseListener()
			=> GlobalMessenger<int, int, SocketEventType>.RemoveListener(EventNames.QSBSocketItem, Handler);

		private void Handler(int socketId, int itemId, SocketEventType type)
			=> SendEvent(CreateMessage(socketId, itemId, type));

		private SocketItemMessage CreateMessage(int socketId, int itemId, SocketEventType type) => new SocketItemMessage
		{
			AboutId = QSBPlayerManager.LocalPlayerId,
			SocketId = socketId,
			ItemId = itemId,
			SocketType = type
		};

		public override void OnReceiveRemote(bool server, SocketItemMessage message)
		{
			var socketWorldObject = QSBWorldSync.GetWorldFromId<IQSBOWItemSocket>(message.SocketId);
			var itemWorldObject = QSBWorldSync.GetWorldFromId<IQSBOWItem>(message.ItemId);
			switch (message.SocketType)
			{
				case SocketEventType.Socket:
					socketWorldObject.PlaceIntoSocket(itemWorldObject);
					return;
				case SocketEventType.StartUnsocket:
					if (!socketWorldObject.IsSocketOccupied())
					{
						DebugLog.ToConsole($"Warning - Trying to start unsocket on socket that is unoccupied! Socket:{(socketWorldObject as IWorldObject).Name}");
						return;
					}

					socketWorldObject.RemoveFromSocket();
					return;
				case SocketEventType.CompleteUnsocket:
					itemWorldObject.OnCompleteUnsocket();
					return;
			}
		}
	}
}
