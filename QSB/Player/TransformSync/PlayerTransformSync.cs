﻿using OWML.Utils;
using QSB.Animation.Player;
using QSB.Events;
using QSB.Instruments;
using QSB.RoastingSync;
using QSB.SectorSync;
using QSB.Syncs.TransformSync;
using QSB.Tools;
using QSB.Utility;
using System.Linq;
using UnityEngine;

namespace QSB.Player.TransformSync
{
	public class PlayerTransformSync : SectoredTransformSync
	{
		static PlayerTransformSync() => AnimControllerPatch.Init();

		private Transform _visibleCameraRoot;
		private Transform _networkCameraRoot => gameObject.transform.GetChild(0);

		private Transform _networkRoastingSystem => gameObject.transform.GetChild(1);
		private Transform _networkStickRoot => _networkRoastingSystem.GetChild(0);

		private Transform _visibleStickPivot;
		private Transform _networkStickPivot => _networkStickRoot.GetChild(0);

		private Transform _visibleStickTip;
		private Transform _networkStickTip => _networkStickPivot.GetChild(0);

		private Transform GetStickPivot()
			=> Resources.FindObjectsOfTypeAll<RoastingStickController>().First().transform.Find("Stick_Root/Stick_Pivot");

		public override void OnStartLocalPlayer()
			=> LocalInstance = this;

		public override void Start()
		{
			base.Start();
			Player.TransformSync = this;
		}

		protected override void OnDestroy()
		{
			QSBPlayerManager.OnRemovePlayer?.Invoke(PlayerId);
			base.OnDestroy();
			if (QSBPlayerManager.PlayerExists(PlayerId))
			{
				Player.HudMarker?.Remove();
				QSBPlayerManager.RemovePlayer(PlayerId);
			}
		}

		protected override Transform InitLocalTransform()
		{
			SectorSync.Init(Locator.GetPlayerSectorDetector(), this);

			// player body
			var player = Locator.GetPlayerTransform();
			var playerModel = player.Find("Traveller_HEA_Player_v2");
			GetComponent<AnimationSync>().InitLocal(playerModel);
			GetComponent<InstrumentsManager>().InitLocal(player);
			Player.Body = player.gameObject;

			// camera
			var cameraBody = Locator.GetPlayerCamera().gameObject.transform;
			Player.Camera = Locator.GetPlayerCamera();
			Player.CameraBody = cameraBody.gameObject;
			_visibleCameraRoot = cameraBody;

			// stick
			var pivot = GetStickPivot();
			Player.RoastingStick = pivot.gameObject;
			_visibleStickPivot = pivot;
			_visibleStickTip = pivot.Find("Stick_Tip");

			Player.PlayerStates.IsReady = true;
			QSBEventManager.FireEvent(EventNames.QSBPlayerReady, true);
			DebugLog.DebugWrite("PlayerTransformSync init done - Request state!");
			QSBEventManager.FireEvent(EventNames.QSBPlayerStatesRequest);

			return player;
		}

		protected override Transform InitRemoteTransform()
		{
			/*
			 * CREATE PLAYER STRUCTURE
			 */
			DebugLog.DebugWrite($"CREATE PLAYER STRUCTURE");
			// Variable naming convention is broken here to reflect OW unity project (with REMOTE_ prefixed) for readability

			var REMOTE_Player_Body = new GameObject("REMOTE_Player_Body");

			var REMOTE_PlayerCamera = new GameObject("REMOTE_PlayerCamera");
			REMOTE_PlayerCamera.transform.parent = REMOTE_Player_Body.transform;
			REMOTE_PlayerCamera.transform.localPosition = new Vector3(0f, 0.8496093f, 0.1500003f);

			var REMOTE_RoastingSystem = new GameObject("REMOTE_RoastingSystem");
			REMOTE_RoastingSystem.transform.parent = REMOTE_Player_Body.transform;
			REMOTE_RoastingSystem.transform.localPosition = new Vector3(0f, 0.4f, 0f);

			var REMOTE_Stick_Root = new GameObject("REMOTE_Stick_Root");
			REMOTE_Stick_Root.transform.parent = REMOTE_RoastingSystem.transform;
			REMOTE_Stick_Root.transform.localPosition = new Vector3(0.25f, 0f, 0.08f);
			REMOTE_Stick_Root.transform.localRotation = Quaternion.Euler(0f, -10f, 0f);

			/*
			 * SET UP PLAYER BODY
			 */
			DebugLog.DebugWrite($"SET UP PLAYER BODY");
			var player = Locator.GetPlayerTransform();
			var playerModel = player.Find("Traveller_HEA_Player_v2");

			var REMOTE_Traveller_HEA_Player_v2 = Instantiate(playerModel);
			REMOTE_Traveller_HEA_Player_v2.transform.parent = REMOTE_Player_Body.transform;
			REMOTE_Traveller_HEA_Player_v2.transform.localPosition = new Vector3(0f, -1.03f, -0.2f);
			REMOTE_Traveller_HEA_Player_v2.transform.localRotation = Quaternion.Euler(-1.500009f, 0f, 0f);
			REMOTE_Traveller_HEA_Player_v2.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

			Player.Body = REMOTE_Player_Body;

			GetComponent<AnimationSync>().InitRemote(REMOTE_Traveller_HEA_Player_v2);
			GetComponent<InstrumentsManager>().InitRemote(REMOTE_Player_Body.transform);

			var marker = REMOTE_Player_Body.AddComponent<PlayerHUDMarker>();
			marker.Init(Player);

			REMOTE_Player_Body.AddComponent<PlayerMapMarker>().PlayerName = Player.Name;

			/*
			 * SET UP PLAYER CAMERA
			 */
			DebugLog.DebugWrite($"SET UP PLAYER CAMERA");
			PlayerToolsManager.Init(REMOTE_PlayerCamera.transform);

			var camera = REMOTE_PlayerCamera.AddComponent<Camera>();
			camera.enabled = false;
			var owcamera = REMOTE_PlayerCamera.AddComponent<OWCamera>();
			owcamera.fieldOfView = 70;
			owcamera.nearClipPlane = 0.1f;
			owcamera.farClipPlane = 50000f;
			Player.Camera = owcamera;
			Player.CameraBody = REMOTE_PlayerCamera;
			_visibleCameraRoot = REMOTE_PlayerCamera.transform;

			/*
			 * SET UP ROASTING STICK
			 */
			DebugLog.DebugWrite($"SET UP ROASTING STICK");

			DebugLog.DebugWrite($"create remote stick pivot");
			var REMOTE_Stick_Pivot = Instantiate(GetStickPivot());
			REMOTE_Stick_Pivot.parent = REMOTE_Stick_Root.transform;
			REMOTE_Stick_Pivot.gameObject.SetActive(false);

			DebugLog.DebugWrite($"destroy arms");
			Destroy(REMOTE_Stick_Pivot.Find("Stick_Tip/Props_HEA_RoastingStick/RoastingStick_Arm").gameObject);
			Destroy(REMOTE_Stick_Pivot.Find("Stick_Tip/Props_HEA_RoastingStick/RoastingStick_Arm_NoSuit").gameObject);

			DebugLog.DebugWrite($"get marshmallow");
			var mallowRoot = REMOTE_Stick_Pivot.Find("Stick_Tip/Mallow_Root");
			mallowRoot.gameObject.SetActive(false);
			var oldMarshmallow = mallowRoot.GetComponent<Marshmallow>();

			// Recreate particle system
			DebugLog.DebugWrite($"recreate particle systems");
			Destroy(mallowRoot.Find("MallowSmoke").GetComponent<RelativisticParticleSystem>());
			var newSystem = mallowRoot.Find("MallowSmoke").gameObject.AddComponent<CustomRelativisticParticleSystem>();
			newSystem.Init(Player);

			// Create new marshmallow
			DebugLog.DebugWrite($"set up new marshmallow");
			var newMarshmallow = mallowRoot.gameObject.AddComponent<QSBMarshmallow>();
			newMarshmallow._fireRenderer = oldMarshmallow.GetValue<MeshRenderer>("_fireRenderer");
			newMarshmallow._smokeParticles = oldMarshmallow.GetValue<ParticleSystem>("_smokeParticles");
			newMarshmallow._mallowRenderer = oldMarshmallow.GetValue<MeshRenderer>("_mallowRenderer");
			newMarshmallow._rawColor = oldMarshmallow.GetValue<Color>("_rawColor");
			newMarshmallow._toastedColor = oldMarshmallow.GetValue<Color>("_toastedColor");
			newMarshmallow._burntColor = oldMarshmallow.GetValue<Color>("_burntColor");
			Destroy(oldMarshmallow);

			DebugLog.DebugWrite($"finish up");
			Player.RoastingStick = REMOTE_Stick_Pivot.gameObject;
			Player.Marshmallow = newMarshmallow;
			mallowRoot.gameObject.SetActive(true);
			_visibleStickPivot = REMOTE_Stick_Pivot;
			_visibleStickTip = REMOTE_Stick_Pivot.Find("Stick_Tip");

			return REMOTE_Player_Body.transform;
		}

		protected override void UpdateTransform()
		{
			base.UpdateTransform();

			if (HasAuthority)
			{
				_networkStickPivot.localPosition = _visibleStickPivot.localPosition;
				_networkStickPivot.localRotation = _visibleStickPivot.localRotation;

				_networkStickTip.localPosition = _visibleStickTip.localPosition;
				_networkStickTip.localRotation = _visibleStickTip.localRotation;

				_networkCameraRoot.localPosition = _visibleCameraRoot.localPosition;
				_networkCameraRoot.localRotation = _visibleCameraRoot.localRotation;

				return;
			}

			_visibleStickPivot.localPosition = _networkStickPivot.localPosition;
			_visibleStickPivot.localRotation = _networkStickPivot.localRotation;

			_visibleStickTip.localPosition = _networkStickTip.localPosition;
			_visibleStickTip.localRotation = _networkStickTip.localRotation;

			_visibleCameraRoot.localPosition = _networkCameraRoot.localPosition;
			_visibleCameraRoot.localRotation = _networkCameraRoot.localRotation;
		}

		public override bool IsReady => Locator.GetPlayerTransform() != null
			&& Player != null
			&& QSBPlayerManager.PlayerExists(Player.PlayerId)
			&& NetId.Value != uint.MaxValue
			&& NetId.Value != 0U;

		public static PlayerTransformSync LocalInstance { get; private set; }

		public override bool UseInterpolation => true;

		public override TargetType Type => TargetType.Player;
	}
}