﻿using UnityEngine;

namespace QSB.Tools.FlashlightTool
{
	static class FlashlightCreator
	{
		private static readonly Vector3 FlashlightOffset = new Vector3(0.7196316f, -0.2697681f, 0.3769455f);

		internal static void CreateFlashlight(Transform cameraBody)
		{
			var flashlightRoot = Object.Instantiate(GameObject.Find("FlashlightRoot"));
			flashlightRoot.name = "REMOTE_FlashlightRoot";
			flashlightRoot.SetActive(false);
			var oldComponent = flashlightRoot.GetComponent<Flashlight>();
			var component = flashlightRoot.AddComponent<QSBFlashlight>();

			component.Init(oldComponent);
			oldComponent.enabled = false;

			flashlightRoot.transform.parent = cameraBody;
			flashlightRoot.transform.localPosition = FlashlightOffset;
			flashlightRoot.SetActive(true);
		}
	}
}
