﻿using QSB.Events;
using UnityEngine;

namespace QSB.Tools.ProbeLauncherTool
{
	internal class ProbeLauncherListener : MonoBehaviour
	{
		private PlayerProbeLauncher _attachedLauncher;

		public void Init(PlayerProbeLauncher localLauncher)
		{
			_attachedLauncher = localLauncher;
			_attachedLauncher.OnLaunchProbe += OnLaunchProbe;
		}

		private void OnLaunchProbe(SurveyorProbe probe) => QSBEventManager.FireEvent(EventNames.QSBPlayerLaunchProbe);
	}
}
