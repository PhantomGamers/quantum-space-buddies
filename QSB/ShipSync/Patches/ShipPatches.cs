﻿using HarmonyLib;
using OWML.Utils;
using QSB.Events;
using QSB.Patches;
using QSB.Utility;
using System;
using UnityEngine;

namespace QSB.ShipSync.Patches
{
	[HarmonyPatch]
	internal class ShipPatches : QSBPatch
	{
		public override QSBPatchTypes Type => QSBPatchTypes.OnClientConnect;

		[HarmonyPrefix]
		[HarmonyPatch(typeof(HatchController), nameof(HatchController.OnPressInteract))]
		public static bool HatchController_OnPressInteract()
		{
			if (!PlayerState.IsInsideShip())
			{
				ShipManager.Instance.ShipTractorBeam.ActivateTractorBeam();
				QSBEventManager.FireEvent(EventNames.QSBEnableFunnel);
			}

			QSBEventManager.FireEvent(EventNames.QSBHatchState, true);
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(HatchController), nameof(HatchController.OnEntry))]
		public static bool HatchController_OnEntry(GameObject hitObj)
		{
			if (hitObj.CompareTag("PlayerDetector"))
			{
				QSBEventManager.FireEvent(EventNames.QSBHatchState, false);
			}

			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(ShipTractorBeamSwitch), nameof(ShipTractorBeamSwitch.OnTriggerExit))]
		public static bool ShipTractorBeamSwitch_OnTriggerExit(ShipTractorBeamSwitch __instance, Collider hitCollider)
		{
			if (!__instance._isPlayerInShip && __instance._functional && hitCollider.CompareTag("PlayerDetector") && !ShipManager.Instance.HatchController._hatchObject.activeSelf)
			{
				ShipManager.Instance.HatchController.Invoke("CloseHatch");
				ShipManager.Instance.ShipTractorBeam.DeactivateTractorBeam();
				QSBEventManager.FireEvent(EventNames.QSBHatchState, false);
			}

			return false;
		}

		[HarmonyReversePatch]
		[HarmonyPatch(typeof(SingleInteractionVolume), nameof(SingleInteractionVolume.UpdateInteractVolume))]
		public static void SingleInteractionVolume_UpdateInteractVolume_Stub(object instance)
		{
			throw new NotImplementedException();
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(InteractZone), nameof(InteractZone.UpdateInteractVolume))]
		public static bool InteractZone_UpdateInteractVolume(InteractZone __instance)
		{
			/* Angle for interaction with the ship hatch
			 *  
			 *  \  80°  / - If in ship
			 *   \     /
			 *    \   /
			 *   [=====]  - Hatch
			 *    /   \
			 *   /     \
			 *  / 280°  \ - If not in ship
			 *  
			 */

			if (!QSBCore.WorldObjectsReady || __instance != ShipManager.Instance.HatchInteractZone)
			{
				return true;
			}

			var angle = 2f * Vector3.Angle(__instance._playerCam.transform.forward, __instance.transform.forward);

			__instance._focused = PlayerState.IsInsideShip()
				? angle <= 80
				: angle >= 280;

			SingleInteractionVolume_UpdateInteractVolume_Stub(__instance as SingleInteractionVolume);

			return false;
		}

		[HarmonyReversePatch]
		[HarmonyPatch(typeof(ShipComponent), nameof(ShipComponent.OnEnterShip))]
		public static void ShipComponent_OnEnterShip_Stub(object instance)
		{
			throw new NotImplementedException();
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(ShipElectricalComponent), nameof(ShipElectricalComponent.OnEnterShip))]
		public static bool ShipElectricalComponent_OnEnterShip(ShipElectricalComponent __instance)
		{
			ShipComponent_OnEnterShip_Stub(__instance as ShipComponent);

			return false;
		}

		[HarmonyReversePatch]
		[HarmonyPatch(typeof(ShipComponent), nameof(ShipComponent.OnExitShip))]
		public static void ShipComponent_OnExitShip_Stub(object instance)
		{
			throw new NotImplementedException();
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(ShipElectricalComponent), nameof(ShipElectricalComponent.OnExitShip))]
		public static bool ShipElectricalComponent_OnExitShip(ShipElectricalComponent __instance)
		{
			ShipComponent_OnExitShip_Stub(__instance as ShipComponent);

			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(ShipComponent), nameof(ShipComponent.SetDamaged))]
		public static bool ShipComponent_SetDamaged(ShipComponent __instance, bool damaged)
		{
			if (__instance._damaged == damaged)
			{
				return false;
			}

			if (damaged)
			{
				__instance._damaged = true;
				__instance._repairFraction = 0f;
				__instance.GetType().GetAnyMethod("OnComponentDamaged").Invoke(__instance, null);
				__instance.RaiseEvent("OnDamaged", __instance);
				QSBEventManager.FireEvent(EventNames.QSBComponentDamaged, __instance);
			}
			else
			{
				__instance._damaged = false;
				__instance._repairFraction = 1f;
				__instance.GetType().GetAnyMethod("OnComponentRepaired").Invoke(__instance, null);
				__instance.RaiseEvent("OnRepaired", __instance);
				QSBEventManager.FireEvent(EventNames.QSBComponentRepaired, __instance);
			}

			__instance.GetType().GetAnyMethod("UpdateColliderState").Invoke(__instance, null);
			if (__instance._damageEffect)
			{
				__instance._damageEffect.SetEffectBlend(1f - __instance._repairFraction);
			}

			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(ShipHull), nameof(ShipHull.FixedUpdate))]
		public static bool ShipHull_FixedUpdate(ShipHull __instance, ref ImpactData ____dominantImpact, ref float ____integrity, ref bool ____damaged, DamageEffect ____damageEffect, ShipComponent[] ____components)
		{
			if (____dominantImpact != null)
			{
				var damage = Mathf.InverseLerp(30f, 200f, ____dominantImpact.speed);
				if (damage > 0f)
				{
					var num2 = 0.15f;
					if (damage < num2 && ____integrity > 1f - num2)
					{
						damage = num2;
					}

					____integrity = Mathf.Max(____integrity - damage, 0f);
					if (!____damaged)
					{
						____damaged = true;
						__instance.RaiseEvent("OnDamaged", __instance);
						QSBEventManager.FireEvent(EventNames.QSBHullDamaged, __instance);
					}

					if (____damageEffect != null)
					{
						____damageEffect.SetEffectBlend(1f - ____integrity);
					}

					QSBEventManager.FireEvent(EventNames.QSBHullChangeIntegrity, __instance, ____integrity);
				}

				foreach (var component in ____components)
				{
					if (!(component == null) && !component.isDamaged)
					{
						if (component.ApplyImpact(____dominantImpact))
						{
							break;
						}
					}
				}

				__instance.RaiseEvent("OnImpact", ____dominantImpact, damage);
				QSBEventManager.FireEvent(EventNames.QSBHullImpact, __instance, ____dominantImpact, damage);

				____dominantImpact = null;
			}

			__instance.enabled = false;
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(ShipDamageController), nameof(ShipDamageController.OnImpact))]
		public static bool ShipDamageController_OnImpact()
			=> ShipManager.Instance.HasAuthority;

		[HarmonyPostfix]
		[HarmonyPatch(typeof(ShipComponent), nameof(ShipComponent.RepairTick))]
		public static void ShipComponent_RepairTick(ShipComponent __instance, float ____repairFraction)
		{
			QSBEventManager.FireEvent(EventNames.QSBComponentRepairTick, __instance, ____repairFraction);
			return;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(ShipHull), nameof(ShipHull.RepairTick))]
		public static bool ShipHull_RepairTick(ShipHull __instance, ref float ____integrity, ref bool ____damaged, DamageEffect ____damageEffect, float ____repairTime)
		{
			if (!____damaged)
			{
				return false;
			}

			____integrity = Mathf.Min(____integrity + Time.deltaTime / ____repairTime, 1f);
			QSBEventManager.FireEvent(EventNames.QSBHullRepairTick, __instance, ____integrity);

			if (____integrity >= 1f)
			{
				____damaged = false;
				__instance.RaiseEvent("OnRepaired", __instance);
				QSBEventManager.FireEvent(EventNames.QSBHullRepaired, __instance);
			}

			if (____damageEffect != null)
			{
				____damageEffect.SetEffectBlend(1f - ____integrity);
			}

			return false;
		}
	}
}
