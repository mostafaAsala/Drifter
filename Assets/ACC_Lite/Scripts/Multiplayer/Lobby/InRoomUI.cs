using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using GameBalance;

/// <summary>
/// To display and control the parameters in the room.
/// </summary>
public class InRoomUI : MonoBehaviour, IInRoomCallbacks, IOnEventCallback
{

	[SerializeField] PlayerItemInRoomUI PlayerItemUIRef;
	[SerializeField] Button SelectCarButton;
	[SerializeField] Button SelectTrackButton;
	[SerializeField] Image SelectedTrackIcon;
	[SerializeField] Image RegimeIcon;
	[SerializeField] TextMeshProUGUI SelectedTrackText;
	[SerializeField] SelectTrackUI SelectTrackUI;
	[SerializeField] SelectCarMenuUI SelectCarMenuUI;
	[SerializeField] int MinimumPlayersForStart = 2;
	[SerializeField] Button ReadyButton;
	[SerializeField] ColorBlock ReadyColors;
	[SerializeField] ColorBlock NotReadyColors;

	//Connected players dictionary.
	Dictionary<Player, PlayerItemInRoomUI> Players = new Dictionary<Player, PlayerItemInRoomUI>();

	Room CurrentRoom { get { return PhotonNetwork.CurrentRoom; } }
	bool IsMaster { get { return PhotonNetwork.IsMasterClient; } }
	bool IsRandomRoom { get { return CurrentRoom.CustomProperties.ContainsKey (C.RandomRoom); } }
	Player LocalPlayer { get { return PhotonNetwork.LocalPlayer; } }

	bool WaitStartGame;

	void Awake ()
	{
		//Initialized all buttons.

		PlayerItemUIRef.SetActive (false);
		SelectTrackButton.onClick.AddListener (() => 
		{
			WindowsController.Instance.OpenWindow (SelectTrackUI);
			SelectTrackUI.OnSelectTrackAction = OnSelectTrack;
		});
		SelectCarButton.onClick.AddListener (() =>
		{
			WindowsController.Instance.OpenWindow (SelectCarMenuUI);
			SelectCarMenuUI.OnSelectCarAction = OnSelectCar;
		});

		ReadyButton.onClick.AddListener (OnReadyClick);
	}

	void OnEnable ()
	{
		PhotonNetwork.AddCallbackTarget (this);

		if (CurrentRoom == null)
		{
			return;
		}

		SelectTrackButton.SetActive (IsMaster || IsRandomRoom);
		OnRoomPropertiesUpdate (CurrentRoom.CustomProperties);

		foreach (var playerKV in Players)
		{
			Destroy (playerKV.Value.gameObject);
		}

		Players.Clear ();

		foreach (var playerKV in CurrentRoom.Players)
		{
			TryUpdateOrCreatePlayerItem (playerKV.Value);
		}

		var localPlayer = PhotonNetwork.LocalPlayer;

		//Choosing the first car at the entrance to the room.
		if (WorldLoading.PlayerCar == null || !LocalPlayer.CustomProperties.ContainsKey(C.CarName))
		{
			var selectedCar = WorldLoading.AvailableCars.First ();
			WorldLoading.PlayerCar = selectedCar;
			LocalPlayer.SetCustomProperties (C.CarName, selectedCar.CarCaption);
			LocalPlayer.SetCustomProperties (C.CarName, selectedCar.CarCaption, C.CarColorIndex, PlayerProfile.GetCarColorIndex (selectedCar), C.IsReady, false);
		}

		//Choosing the first track when create the room.
		if (PhotonNetwork.IsMasterClient && (CurrentRoom.CustomProperties == null || !CurrentRoom.CustomProperties.ContainsKey (C.TrackName)))
		{
			var hashtable = new Hashtable ();
			hashtable.Add (C.TrackName, B.GameSettings.Tracks.First ().name);
			CurrentRoom.SetCustomProperties (hashtable);
		}

		ReadyButton.Select ();
	}

	public virtual void OnDisable ()
	{
		PhotonNetwork.RemoveCallbackTarget (this);
	}

	void OnSelectCar (CarPreset selectedCar)
	{
		WindowsController.Instance.OnBack ();
		WorldLoading.PlayerCar = selectedCar;
		LocalPlayer.SetCustomProperties (C.CarName, selectedCar.CarCaption, C.CarColorIndex, PlayerProfile.GetCarColorIndex(selectedCar));
	}

	void OnSelectTrack (TrackPreset selectedTrack)
	{
		WindowsController.Instance.OnBack ();
		var hashtable = new Hashtable ();
		hashtable.Add (C.TrackName, selectedTrack.name);
		
		if (IsRandomRoom)
		{
			LocalPlayer.SetCustomProperties (hashtable);
		}
		else if (IsMaster)
		{
			CurrentRoom.SetCustomProperties (hashtable);
		}
	}

	void OnReadyClick ()
	{
		LocalPlayer.SetCustomProperties (C.IsReady, !(bool)LocalPlayer.CustomProperties[C.IsReady], C.CarColorIndex, PlayerProfile.GetCarColorIndex(WorldLoading.PlayerCar));
	}

	//Updating a player when changing any property.
	void TryUpdateOrCreatePlayerItem (Player targetPlayer)
	{
		PlayerItemInRoomUI playerItem = null;

		//Get or create PlayerItem.
		if (!Players.TryGetValue(targetPlayer, out playerItem))
		{
			playerItem = Instantiate (PlayerItemUIRef, PlayerItemUIRef.transform.parent);
			Players.Add (targetPlayer, playerItem);
		}

		var customProps = targetPlayer.CustomProperties;

		//SetActive(false) if player without any property.
		if (customProps == null ||
			!customProps.ContainsKey (C.CarName) ||
			!customProps.ContainsKey (C.IsReady)
			)
		{
			playerItem.SetActive (false);
			return;
		}

		var idReady = (bool)customProps[C.IsReady];

		System.Action kickAction = null;
		if (PhotonNetwork.IsMasterClient && !IsRandomRoom)
		{
			kickAction = () =>
			{
				PhotonNetwork.CloseConnection (targetPlayer);
			};
		}

		playerItem.SetActive (true);
		playerItem.UpdateProperties (targetPlayer, kickAction);

		if (targetPlayer.IsLocal)
		{
			ReadyButton.colors = idReady ? ReadyColors : NotReadyColors;
		}

		//We inform all players about the start of the game.
		if (IsMaster && !WaitStartGame &&
			CurrentRoom.PlayerCount >= MinimumPlayersForStart && 
			CurrentRoom.Players.All
			(p =>
				p.Value.CustomProperties.ContainsKey(C.IsReady) &&
				(bool)p.Value.CustomProperties[C.IsReady]
			))
		{
			//Calculate votes
			if (CurrentRoom.CustomProperties.ContainsKey (C.RandomRoom))
			{
				Dictionary<string, int> votesForTracks = new Dictionary<string, int>();

				foreach (var track in B.MultiplayerSettings.AvailableTracksForMultiplayer)
				{
					votesForTracks.Add (track.name, 0);
				}

				//Get all votes.
				foreach (var player in CurrentRoom.Players)
				{
					var track = player.Value.CustomProperties.ContainsKey(C.TrackName)? (string)player.Value.CustomProperties[C.TrackName]: "";
					if (!string.IsNullOrEmpty (track))
					{
						votesForTracks[track]++;
					}
				}

				//Get max votes.
				int maxVotes = votesForTracks.Max(kv => kv.Value);

				//Get tracks with max votes.
				List<string> selectedTracks = new List<string>();
				foreach (var track in votesForTracks)
				{
					if (track.Value >= maxVotes)
					{
						selectedTracks.Add (track.Key);
					}
				}

				//Random choice
				var customProperties = new Hashtable ();
				customProperties.Add (C.TrackName, selectedTracks.RandomChoice());
				CurrentRoom.SetCustomProperties (customProperties);
			}

			PhotonNetwork.RaiseEvent (PE.StartGame, null, new RaiseEventOptions () { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
			WaitStartGame = true;
		}
	}

	//Clear player items.
	public void RemoveAllPlayers ()
	{
		foreach (var player in Players)
		{
			Destroy (player.Value.gameObject);
		}
		Players.Clear ();
	}

	public void OnMasterClientSwitched (Player newMasterClient)
	{
		Debug.LogFormat ("New master is player [{0}]", newMasterClient.NickName);
		SelectTrackButton.SetActive (IsMaster);

		foreach (var player in CurrentRoom.Players)
		{
			TryUpdateOrCreatePlayerItem (player.Value);
		}
		UpdateCustomProperties ();
	}

	public void OnPlayerEnteredRoom (Player newPlayer)
	{
		TryUpdateOrCreatePlayerItem (newPlayer);
		UpdateCustomProperties ();
	}

	public void OnPlayerLeftRoom (Player otherPlayer)
	{
		if (Players.ContainsKey (otherPlayer))
		{
			Destroy(Players[otherPlayer].gameObject);
			Players.Remove (otherPlayer);
		}

		UpdateCustomProperties ();
	}

	public void OnPlayerPropertiesUpdate (Player targetPlayer, Hashtable changedProps)
	{
		TryUpdateOrCreatePlayerItem (targetPlayer);
	}

	public void OnRoomPropertiesUpdate (Hashtable propertiesThatChanged)
	{
		if (propertiesThatChanged.ContainsKey (C.TrackName))
		{
			var trackName = (string)propertiesThatChanged[C.TrackName];

			var track = B.GameSettings.Tracks.FirstOrDefault (t => t.name == trackName);

			SelectedTrackIcon.sprite = track.TrackIcon;
			RegimeIcon.sprite = track.RegimeSettings.RegimeImage;
			SelectedTrackText.text = string.Format ("{0}: {1}", track.TrackName, track.RegimeSettings.RegimeCaption);
		}
	}

	void UpdateCustomProperties ()
	{
		if (IsMaster)
		{
			var customProperties = new Hashtable ();
			customProperties.Add (C.RoomCreator, PlayerProfile.NickName);
			CurrentRoom.SetCustomProperties (customProperties);
		}
	}

	//We get information about the start and start loading the level.
	public void OnEvent (EventData photonEvent)
	{
		if (photonEvent.Code == PE.StartGame)
		{
			WorldLoading.IsMultiplayer = true;
			PhotonNetwork.LocalPlayer.SetCustomProperties (C.IsReady, false, C.IsLoaded, false, C.TrackName, "");

			if (!CurrentRoom.CustomProperties.ContainsKey (C.TrackName))
			{
				return;
			}

			var trackName = (string)CurrentRoom.CustomProperties[C.TrackName];

			var track = B.GameSettings.Tracks.FirstOrDefault (t => t.name == trackName);
			WorldLoading.LoadingTrack = track;

			LoadingScreenUI.LoadScene (track.SceneName, track.RegimeSettings.RegimeSceneName);

			if (IsMaster)
			{
				CurrentRoom.IsOpen = false;
				CurrentRoom.IsVisible = false;
			}
		}
	}
}
