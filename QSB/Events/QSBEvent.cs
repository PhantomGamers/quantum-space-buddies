﻿using OWML.Common;
using QSB.Messaging;
using QSB.Player;
using QSB.Player.TransformSync;
using QSB.Utility;
using QuantumUNET.Components;
using System;

namespace QSB.Events
{
	public abstract class QSBEvent<T> : IQSBEvent where T : PlayerMessage, new()
	{
		public abstract EventType Type { get; }
		public uint LocalPlayerId => QSBPlayerManager.LocalPlayerId;

		private readonly MessageHandler<T> _eventHandler;

		protected QSBEvent()
		{
			if (UnitTestDetector.IsInUnitTest)
			{
				return;
			}

			_eventHandler = new MessageHandler<T>(Type);
			_eventHandler.OnClientReceiveMessage += message => OnReceive(false, message);
			_eventHandler.OnServerReceiveMessage += message => OnReceive(true, message);
		}

		public abstract void SetupListener();
		public abstract void CloseListener();

		public virtual void OnReceiveRemote(bool isHost, T message) { }
		public virtual void OnReceiveLocal(bool isHost, T message) { }

		public void SendEvent(T message)
		{
			message.FromId = QSBPlayerManager.LocalPlayerId;
			QSBCore.UnityEvents.RunWhen(
				() => PlayerTransformSync.LocalInstance != null,
				() => _eventHandler.SendToServer(message));
		}

		/// <summary>
		/// Checks whether the message should be processed by the executing client/server.
		/// </summary>
		/// <returns>True if the message should be processed.</returns>
		public virtual bool CheckMessage(bool isServer, T message) => true;

		private void OnReceive(bool isServer, T message)
		{
			/* Explanation :
			 * if <isServer> is true, this message has been received on the server *server*.
			 * Therefore, we don't want to do any event handling code - that should be dealt
			 * with on the server *client* and any other client. So just forward the message
			 * onto all clients. This way, the server *server* just acts as the distribution
			 * hub for all events.
			 */

			if (!CheckMessage(isServer, message))
			{
				return;
			}

			if (isServer)
			{
				_eventHandler.SendToAll(message);
				return;
			}

			if (message.OnlySendToHost && !QSBCore.IsHost)
			{
				return;
			}

			if (PlayerTransformSync.LocalInstance == null || PlayerTransformSync.LocalInstance.GetComponent<QNetworkIdentity>() == null)
			{
				DebugLog.ToConsole($"Warning - Tried to handle message of type <{message.GetType().Name}> before localplayer was established.", MessageType.Warning);
				return;
			}

			try
			{
				if (message.FromId == QSBPlayerManager.LocalPlayerId ||
				QSBPlayerManager.IsBelongingToLocalPlayer(message.FromId))
				{
					OnReceiveLocal(QSBCore.IsHost, message);
					return;
				}

				OnReceiveRemote(QSBCore.IsHost, message);
			}
			catch (Exception ex)
			{
				DebugLog.ToConsole($"Error - Exception handling message {message.GetType().Name} : {ex}", MessageType.Error);
			}
		}
	}
}