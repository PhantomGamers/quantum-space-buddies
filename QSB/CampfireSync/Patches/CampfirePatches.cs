﻿using HarmonyLib;
using QSB.CampfireSync.WorldObjects;
using QSB.Events;
using QSB.Patches;
using QSB.WorldSync;

namespace QSB.CampfireSync.Patches
{
	[HarmonyPatch]
	internal class CampfirePatches : QSBPatch
	{
		public override QSBPatchTypes Type => QSBPatchTypes.OnClientConnect;

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Campfire), nameof(Campfire.OnPressInteract))]
		public static bool LightCampfireEvent(Campfire __instance)
		{
			var qsbCampfire = QSBWorldSync.GetWorldFromUnity<QSBCampfire, Campfire>(__instance);
			if (__instance._state == Campfire.State.LIT)
			{
				qsbCampfire.StartRoasting();
			}
			else
			{
				qsbCampfire.SetState(Campfire.State.LIT);
				QSBEventManager.FireEvent(EventNames.QSBCampfireState, qsbCampfire.ObjectId, Campfire.State.LIT);
				Locator.GetFlashlight().TurnOff(false);
			}

			return false;
		}
	}
}
