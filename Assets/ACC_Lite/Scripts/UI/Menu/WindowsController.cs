using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Windows controller to control the game windows.
/// </summary>
public class WindowsController :Singleton<WindowsController>
{

	[SerializeField] Window MainWindow;
	[SerializeField] GameObject BackButton;

	List<Window> WindowsHistory = new List<Window>();
	bool HasNewWindowInFrame;
	public Window CurrentWindow { get; private set; }
	public bool HasWindowsHistory { get { return WindowsHistory.Count > 0; } }

	protected override void AwakeSingleton ()
	{
		var windows = GameObject.FindObjectsOfType<Window> ();
		foreach (var window in windows)
		{
			window.SetActive (false);
		}
	}

	private void Start ()
	{
		if (MainWindow != null)
		{
			MainWindow.Open ();
			CurrentWindow = MainWindow;
		}
	}

	private void Update ()
	{
		if (HasNewWindowInFrame)
		{
			HasNewWindowInFrame = false;
		}
		else if ((Input.GetKeyDown (KeyCode.Escape) || Input.GetKeyDown (KeyCode.Joystick1Button1)) && CurrentWindow != null && CurrentWindow != MainWindow)
		{
			OnBack ();
		}

		BackButton.SetActive (HasWindowsHistory);
	}

	public void OpenWindow (Window window)
	{
		if (CurrentWindow == window)
			return;

		CloseCurrent ();

		WindowsHistory.Add (window);
		CurrentWindow = window;
		CurrentWindow.Open ();
		HasNewWindowInFrame = true;
	}

	public void OnBack (bool ignoreCustomBackAction = false)
	{
		if (!ignoreCustomBackAction && CurrentWindow != null && CurrentWindow.CustomBackAction != null)
		{
			CurrentWindow.CustomBackAction.SafeInvoke ();
			return;
		}

		if (WindowsHistory.Count > 0)
		{
			WindowsHistory.RemoveAt (WindowsHistory.Count - 1);
		}

		CloseCurrent ();

		if (WindowsHistory.Count > 0)
		{
			CurrentWindow = WindowsHistory[WindowsHistory.Count - 1];
		}
		else
		{
			CurrentWindow = MainWindow;
		}

		if (CurrentWindow != null)
		{
			CurrentWindow.Open ();
		}


	}

	private void CloseCurrent ()
	{
		if (CurrentWindow != null)
		{
			CurrentWindow.Close ();
			CurrentWindow = null;
		}
	}
}
