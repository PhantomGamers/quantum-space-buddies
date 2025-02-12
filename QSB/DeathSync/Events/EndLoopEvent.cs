﻿using QSB.ClientServerStateSync;
using QSB.Events;
using QSB.Messaging;
using QSB.Utility;

namespace QSB.DeathSync.Events
{
	internal class EndLoopEvent : QSBEvent<EnumMessage<EndLoopReason>>
	{
		public override EventType Type => EventType.EndLoop;

		public override void SetupListener() => GlobalMessenger<EndLoopReason>.AddListener(EventNames.QSBEndLoop, Handler);
		public override void CloseListener() => GlobalMessenger<EndLoopReason>.RemoveListener(EventNames.QSBEndLoop, Handler);

		private void Handler(EndLoopReason type) => SendEvent(CreateMessage(type));

		private EnumMessage<EndLoopReason> CreateMessage(EndLoopReason type) => new EnumMessage<EndLoopReason>
		{
			AboutId = LocalPlayerId,
			EnumValue = type
		};

		public override void OnReceiveLocal(bool server, EnumMessage<EndLoopReason> message)
			=> OnReceiveRemote(server, message);

		public override void OnReceiveRemote(bool server, EnumMessage<EndLoopReason> message)
		{
			DebugLog.DebugWrite($" ~~~~ END LOOP - Reason:{message.EnumValue} ~~~~ ");
			switch (message.EnumValue)
			{
				case EndLoopReason.AllPlayersDead:
					Locator.GetDeathManager().KillPlayer(DeathType.TimeLoop);
					if (QSBCore.IsHost)
					{
						QSBEventManager.FireEvent(EventNames.QSBServerState, ServerState.WaitingForAllPlayersToDie);
					}

					break;
			}
		}
	}
}
