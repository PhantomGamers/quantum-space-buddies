﻿using QSB.Player;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace QSB.Menus
{
	class MenuManager : MonoBehaviour
	{
		public static MenuManager Instance;

		private IMenuAPI MenuApi => QSBCore.MenuApi;
		private PopupMenu PopupMenu;
		private Button HostButton;
		private GameObject ClientButton;
		private Button DisconnectButton;
		private PopupMenu InfoPopup;
		private bool _addedPauseLock;

		public void Start()
		{
			Instance = this;
			MakeTitleMenus();
			QSBSceneManager.OnSceneLoaded += OnSceneLoaded;
			QSBNetworkManager.Instance.OnClientConnected += OnConnected;
			QSBNetworkManager.Instance.OnClientDisconnected += OnDisconnected;
			QSBNetworkManager.Instance.OnClientErrorThrown += OnClientError;
		}

		void OnSceneLoaded(OWScene oldScene, OWScene newScene, bool isUniverse)
		{
			if (isUniverse)
			{
				InitPauseMenus();
				return;
			}

			if (newScene == OWScene.TitleScreen)
			{
				MakeTitleMenus();
			}
		}

		private void OpenInfoPopup(string message, string buttonText)
		{
			InfoPopup.SetUpPopup(message, InputLibrary.menuConfirm, InputLibrary.cancel, new ScreenPrompt(buttonText), null, true, false);

			OWTime.Pause(OWTime.PauseType.System);
			OWInput.ChangeInputMode(InputMode.Menu);

			var pauseCommandListener = Locator.GetPauseCommandListener();
			if (pauseCommandListener != null)
			{
				pauseCommandListener.AddPauseCommandLock();
				_addedPauseLock = true;
			}

			InfoPopup.EnableMenu(true);
		}

		private void OnCloseInfoPopup()
		{
			var pauseCommandListener = Locator.GetPauseCommandListener();
			if (pauseCommandListener != null && _addedPauseLock)
			{
				pauseCommandListener.RemovePauseCommandLock();
				_addedPauseLock = false;
			}
			OWTime.Unpause(OWTime.PauseType.System);
			OWInput.RestorePreviousInputs();
		}

		private void CreateCommonPopups()
		{
			PopupMenu = MenuApi.MakeInputFieldPopup("IP Address", "IP Address", "Connect", "Cancel");
			PopupMenu.OnPopupConfirm += Connect;

			InfoPopup = MenuApi.MakeInfoPopup("", "");
			InfoPopup.OnDeactivateMenu += OnCloseInfoPopup;
		}

		private void InitPauseMenus()
		{
			CreateCommonPopups();

			HostButton = MenuApi.PauseMenu_MakeSimpleButton("MULTIPLAYER (HOST)");
			HostButton.onClick.AddListener(Host);

			ClientButton = MenuApi.PauseMenu_MakeMenuOpenButton("MULTIPLAYER (CONNECT)", PopupMenu);

			DisconnectButton = MenuApi.PauseMenu_MakeSimpleButton("DISCONNECT");
			DisconnectButton.onClick.AddListener(Disconnect);


			if (QSBCore.IsInMultiplayer)
			{
				ClientButton.SetActive(false);
				HostButton.gameObject.SetActive(false);
				DisconnectButton.gameObject.SetActive(true);
				DisconnectButton.GetComponent<CanvasGroup>().alpha = 1f;
			}
			else
			{
				DisconnectButton.gameObject.SetActive(false);
				DisconnectButton.GetComponent<CanvasGroup>().alpha = 1f;
			}

			OnConnected();
		}

		private void MakeTitleMenus()
		{
			CreateCommonPopups();

			HostButton = MenuApi.TitleScreen_MakeSimpleButton("MULTIPLAYER (HOST)");
			HostButton.onClick.AddListener(Host);

			ClientButton = MenuApi.TitleScreen_MakeMenuOpenButton("MULTIPLAYER (CONNECT)", PopupMenu);

			DisconnectButton = MenuApi.TitleScreen_MakeSimpleButton("DISCONNECT");
			DisconnectButton.onClick.AddListener(Disconnect);

			if (QSBCore.IsInMultiplayer)
			{
				ClientButton.SetActive(false);
				HostButton.gameObject.SetActive(false);
				DisconnectButton.gameObject.SetActive(true);
				DisconnectButton.GetComponent<CanvasGroup>().alpha = 1f;
			}
			else
			{
				DisconnectButton.gameObject.SetActive(false);
				DisconnectButton.GetComponent<CanvasGroup>().alpha = 1f;
			}

			OnConnected();
		}

		private void Disconnect()
		{
			QSBNetworkManager.Instance.StopHost();
			DisconnectButton.gameObject.SetActive(false);
			ClientButton.SetActive(true);
			HostButton.gameObject.SetActive(true);
		}

		private void Host()
		{
			QSBNetworkManager.Instance.StartHost();
			DisconnectButton.gameObject.SetActive(true);
			DisconnectButton.GetComponent<CanvasGroup>().alpha = 1f;
			ClientButton.SetActive(false);
			HostButton.gameObject.SetActive(false);
		}

		private void Connect()
		{
			QSBNetworkManager.Instance.networkAddress = (PopupMenu as PopupInputMenu).GetInputText();
			QSBNetworkManager.Instance.StartClient();
			DisconnectButton.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = "CONNECTING... (STOP)";
			DisconnectButton.gameObject.SetActive(true);
			DisconnectButton.GetComponent<CanvasGroup>().alpha = 1f;
			ClientButton.SetActive(false);
			HostButton.gameObject.SetActive(false);
		}

		private void OnConnected()
		{
			var text = QSBCore.IsHost
				? "STOP HOSTING"
				: "DISCONNECT";
			DisconnectButton.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = text;
		}

		public void OnKicked(KickReason reason)
		{
			string text;
			switch (reason)
			{
				case KickReason.QSBVersionNotMatching:
					text = "Server refused connection as QSB version does not match.";
					break;
				case KickReason.GameVersionNotMatching:
					text = "Server refused connection as Outer Wilds version does not match.";
					break;
				case KickReason.GamePlatformNotMatching:
					text = "Server refused connection as Outer Wilds platform does not match. (Steam/Epic)";
					break;
				case KickReason.None:
					text = "Kicked from server. No reason given.";
					break;
				default:
					text = $"Kicked from server. KickReason:{reason}";
					break;
			}

			OpenInfoPopup(text, "OK");

			DisconnectButton.gameObject.SetActive(false);
			ClientButton.SetActive(true);
			HostButton.gameObject.SetActive(true);
		}

		private void OnDisconnected(NetworkError error)
		{
			if (error == NetworkError.Ok)
			{
				return;
			}

			string text;
			switch (error)
			{
				case NetworkError.Timeout:
					text = "Client disconnected with error!\r\nConnection timed out.";
					break;
				default:
					text = $"Client disconnected with error!\r\nNetworkError:{error}";
					break;
			}

			OpenInfoPopup(text, "OK");

			DisconnectButton.gameObject.SetActive(false);
			ClientButton.SetActive(true);
			HostButton.gameObject.SetActive(true);
		}

		private void OnClientError(NetworkError error)
		{
			if (error == NetworkError.Ok)
			{
				// lol wut
				return;
			}

			string text;
			switch (error)
			{
				case NetworkError.DNSFailure:
					text = "Internal QNet client error!\r\nDNS Faliure. Address was invalid or could not be resolved.";
					DisconnectButton.gameObject.SetActive(false);
					ClientButton.SetActive(true);
					HostButton.gameObject.SetActive(true);
					break;
				default:
					text = $"Internal QNet client error!\n\nNetworkError:{error}";
					break;
			}

			OpenInfoPopup(text, "OK");
		}
	}
}