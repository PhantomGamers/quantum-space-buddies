﻿using QSB.ClientServerStateSync;
using QSB.Events;
using QSB.Player;
using QSB.Utility;

namespace QSB.DeathSync.Events
{
	public class PlayerDeathEvent : QSBEvent<PlayerDeathMessage>
	{
		public override EventType Type => EventType.PlayerDeath;

		public override void SetupListener() => GlobalMessenger<DeathType>.AddListener(EventNames.QSBPlayerDeath, Handler);
		public override void CloseListener() => GlobalMessenger<DeathType>.RemoveListener(EventNames.QSBPlayerDeath, Handler);

		private void Handler(DeathType type) => SendEvent(CreateMessage(type));

		private PlayerDeathMessage CreateMessage(DeathType type) => new PlayerDeathMessage
		{
			AboutId = LocalPlayerId,
			EnumValue = type,
			NecronomiconIndex = Necronomicon.GetRandomIndex(type)
		};

		public override void OnReceiveLocal(bool server, PlayerDeathMessage message)
		{
			var player = QSBPlayerManager.GetPlayer(message.AboutId);
			RespawnManager.Instance.OnPlayerDeath(player);
			ClientStateManager.Instance.OnDeath();
		}

		public override void OnReceiveRemote(bool server, PlayerDeathMessage message)
		{
			var player = QSBPlayerManager.GetPlayer(message.AboutId);
			var playerName = player.Name;
			var deathMessage = Necronomicon.GetPhrase(message.EnumValue, message.NecronomiconIndex);
			DebugLog.ToAll(string.Format(deathMessage, playerName));

			RespawnManager.Instance.OnPlayerDeath(player);
		}
	}
}