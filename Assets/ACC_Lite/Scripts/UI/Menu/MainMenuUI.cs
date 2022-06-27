using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main menu window.
/// </summary>
public class MainMenuUI :WindowWithShowHideAnimators
{
	[SerializeField] Button StartMultiplayerButton;
	[SerializeField] Button StartGameButton;
	[SerializeField] Button SettingsButton;
	[SerializeField] Button ResultsButton;
	[SerializeField] Button QuitButton;
	[SerializeField] Window MultiplayerWindow;
	[SerializeField] Window SelectTrackWindow;
	[SerializeField] Window SettingsWindow;
	[SerializeField] Window ResultsWindow;

	protected override void Awake ()
	{
		StartMultiplayerButton.onClick.AddListener (StartMultiplayer);
		StartGameButton.onClick.AddListener (StartGame);
		SettingsButton.onClick.AddListener (Settings);
		ResultsButton.onClick.AddListener (Results);
		QuitButton.onClick.AddListener (Quit);
		base.Awake ();
	}

	private void StartGame ()
	{
		WorldLoading.IsMultiplayer = false;
		WindowsController.Instance.OpenWindow (SelectTrackWindow);
	}

	private void StartMultiplayer ()
	{
		WorldLoading.IsMultiplayer = true;
		WindowsController.Instance.OpenWindow (MultiplayerWindow);
	}

	private void Settings ()
	{
		WindowsController.Instance.OpenWindow (SettingsWindow);
	}

	private void Results ()
	{
		WindowsController.Instance.OpenWindow (ResultsWindow);
	}

	private void Quit ()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
	}

}
