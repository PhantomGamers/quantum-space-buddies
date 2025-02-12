﻿using QSB.Events;
using QSB.TranslationSync.WorldObjects;
using QSB.WorldSync;

namespace QSB.TranslationSync.Events
{
	public class SetAsTranslatedEvent : QSBEvent<SetAsTranslatedMessage>
	{
		public override EventType Type => EventType.TextTranslated;

		public override void SetupListener() => GlobalMessenger<NomaiTextType, int, int>.AddListener(EventNames.QSBTextTranslated, Handler);
		public override void CloseListener() => GlobalMessenger<NomaiTextType, int, int>.RemoveListener(EventNames.QSBTextTranslated, Handler);

		private void Handler(NomaiTextType type, int objId, int textId) => SendEvent(CreateMessage(type, objId, textId));

		private SetAsTranslatedMessage CreateMessage(NomaiTextType type, int objId, int textId) => new SetAsTranslatedMessage
		{
			AboutId = LocalPlayerId,
			ObjectId = objId,
			TextId = textId,
			EnumValue = type
		};

		public override void OnReceiveRemote(bool server, SetAsTranslatedMessage message)
		{
			if (!QSBCore.WorldObjectsReady)
			{
				return;
			}

			if (message.EnumValue == NomaiTextType.WallText)
			{
				var obj = QSBWorldSync.GetWorldFromId<QSBWallText>(message.ObjectId);
				obj.HandleSetAsTranslated(message.TextId);
			}
			else if (message.EnumValue == NomaiTextType.Computer)
			{
				var obj = QSBWorldSync.GetWorldFromId<QSBComputer>(message.ObjectId);
				obj.HandleSetAsTranslated(message.TextId);
			}
			else if (message.EnumValue == NomaiTextType.VesselComputer)
			{
				var obj = QSBWorldSync.GetWorldFromId<QSBVesselComputer>(message.ObjectId);
				obj.HandleSetAsTranslated(message.TextId);
			}
			else
			{
				throw new System.NotImplementedException($"TextType <{message.EnumValue}> not implemented.");
			}
		}
	}
}