using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

/// <summary>
/// List of available rooms and the ability to create a new one.
/// </summary>
public class RoomListUI : MonoBehaviour
{
	[SerializeField] RoomItemUI RoomItemUIRef;
	[SerializeField] Button ConnectToRandomRoomButton;
	[SerializeField] Button CreateRoomButton;
	[SerializeField] TMP_Dropdown ServerList;

	[Space(10)]
	[SerializeField] TextMeshProUGUI PingText;
	[SerializeField] Image PingIndicatorImage;
	[SerializeField] string AutoText = "(Auto) ";

	List<RoomItemUI> RoomItems = new List<RoomItemUI>();

	List<string> Tokens = new List<string>();

	float Timer = 0;

	private void Start ()
	{
		ConnectToRandomRoomButton.onClick.AddListener (ConnectToRandomRoom);
		CreateRoomButton.onClick.AddListener (CreateRoom);
		RoomItemUIRef.SetActive (false);
		CreateRoomButton.interactable = false;
	}

	void OnEnable()
	{
		Timer = 0;
		ServerList.Select ();
		UpdateServerList ();
	}

	void Update ()
	{
		bool photonIsConnected = PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InLobby;

		CreateRoomButton.interactable = photonIsConnected;
		ConnectToRandomRoomButton.interactable = photonIsConnected;

		var firstOption = ServerList.options[0];
		if (photonIsConnected && firstOption.text == AutoText && ServerList.value == 0)
		{
			var server = B.MultiplayerSettings.Servers.FirstOrDefault(s => s.ServerToken == PhotonNetwork.NetworkingClient.CloudRegion);
			UpdateServerList (server.ServerCaption);
		}

		if (Timer <= 0 && photonIsConnected)
		{
			UpdatePing ();
			Timer = B.MultiplayerSettings.PingUpdateSettings;
		}
		Timer -= Time.deltaTime;
	}

	void ConnectToRandomRoom()
	{
		Hashtable customProperties = new Hashtable ();
		customProperties.Add (C.RandomRoom, true);
		PhotonNetwork.JoinRandomRoom (customProperties, 0, MatchmakingMode.RandomMatching, null, null);
	}

	public void CreateRandomRoom(){

		//set custom properties.
		Hashtable customProperties = new Hashtable ();
		customProperties.Add (C.RandomRoom, true);

		string[] customPropertiesForLobby = new string[1];
		customPropertiesForLobby[0] = C.RandomRoom;

		RoomOptions options = new RoomOptions ()
		{
			IsVisible = true,
			IsOpen = true,
			MaxPlayers = B.MultiplayerSettings.MaxPlayersInRoom,
			CustomRoomProperties = customProperties,
			CustomRoomPropertiesForLobby = customPropertiesForLobby,
		};

		PhotonNetwork.CreateRoom (System.Guid.NewGuid ().ToString (), options);
	}

	void CreateRoom ()
	{
		//set custom properties.
		Hashtable customProperties = new Hashtable ();
		customProperties.Add (C.RoomCreator, PhotonNetwork.NickName);
		customProperties.Add (C.TrackName, B.MultiplayerSettings.AvailableTracksForMultiplayer.First ().name);

		//set custom properties for display in room list.
		string[] customPropertiesForLobby = new string[2];
		customPropertiesForLobby[0] = C.RoomCreator;
		customPropertiesForLobby[1] = C.TrackName;

		RoomOptions options = new RoomOptions ()
		{
			IsVisible = true,
			IsOpen = true,
			MaxPlayers = B.MultiplayerSettings.MaxPlayersInRoom,
			CustomRoomProperties = customProperties,
			CustomRoomPropertiesForLobby = customPropertiesForLobby,
		};
		
		PhotonNetwork.CreateRoom (System.Guid.NewGuid ().ToString (), options);
	}


	public void OnRoomListUpdate (List<RoomInfo> roomList)
	{
        foreach (var roomItem in RoomItems) 
		{
            if (roomItem != null && (roomItem.Room == null || roomItem.Room.RemovedFromList)) {
				Destroy (roomItem.gameObject);
			}
		}

		RoomItems.RemoveAll (i => i == null);

		foreach (RoomInfo room in roomList)
		{
			if (room.CustomProperties != null && room.CustomProperties.ContainsKey(C.RandomRoom))
			{
				continue;
			}

			//Get or create room item.
			RoomItemUI lobbyItem = RoomItems.FirstOrDefault (r => r.Room != null && r.Room.Name == room.Name);

			if (lobbyItem == null)
			{
				lobbyItem = Instantiate (RoomItemUIRef, RoomItemUIRef.transform.parent);
				RoomItems.Add (lobbyItem);
			}

			//Check custom properties.
			if (room.CustomProperties == null ||
				room.PlayerCount == 0 ||
				!room.IsOpen ||
				!room.CustomProperties.ContainsKey (C.RoomCreator) ||
				!room.CustomProperties.ContainsKey (C.TrackName)
			)
			{
				lobbyItem.SetActive (false);
				continue;
			}

			var trackName = (string)room.CustomProperties[C.TrackName];

			var track = B.MultiplayerSettings.AvailableTracksForMultiplayer.Find (t => t.name == trackName);

			lobbyItem.UpdateInfo (
				room,
				track.TrackIcon,
				track.RegimeSettings.RegimeImage,
				(string)room.CustomProperties[C.RoomCreator],
				track.TrackName,
				string.Format ("{0}/{1}", room.PlayerCount, room.MaxPlayers),
				() => PhotonNetwork.JoinRoom (room.Name)
			);

			lobbyItem.SetActive (true);
		}
	}

	void UpdateServerList (string autoRegion = "")
	{
		Tokens = new List<string> ();

		ServerList.ClearOptions ();
		var options = new List<TMP_Dropdown.OptionData> ();

		options.Add (new TMP_Dropdown.OptionData (AutoText + autoRegion));
		Tokens.Add ("");

		foreach (var server in B.MultiplayerSettings.Servers)
		{
			options.Add (new TMP_Dropdown.OptionData (server.ServerCaption));
			Tokens.Add (server.ServerToken);
		}

		ServerList.AddOptions (options);
		ServerList.value = Tokens.FindIndex (t => t == PlayerProfile.ServerToken);
		ServerList.onValueChanged.AddListener ((int value) =>
		{
			PlayerProfile.ServerToken = Tokens[value];
			LobbyManager.ConnectToServer ();
			Timer = 0;
		});
	}

	void UpdatePing ()
	{
		var ping = PhotonNetwork.GetPing();
		PingText.text = ping.ToString();

		if (ping <= B.MultiplayerSettings.VeryGoodPing)
		{
			PingIndicatorImage.sprite = B.MultiplayerSettings.VeryGoodPingSprite;
		}
		else if (ping <= B.MultiplayerSettings.GoodPing)
		{
			PingIndicatorImage.sprite = B.MultiplayerSettings.GoodPingSprite;
		}
		else if (ping <= B.MultiplayerSettings.MediumPing)
		{
			PingIndicatorImage.sprite = B.MultiplayerSettings.MediumPingSprite;
		}
		else
		{
			PingIndicatorImage.sprite = B.MultiplayerSettings.BadPingSprite;
		}
	}

    public void OnDisconnected ()
    {
        if (RoomItems != null)
        {
            foreach (var roomItem in RoomItems)
            {
                if (roomItem != null)
                {
                    Destroy (roomItem.gameObject);
                }
            }
        }
    }
}
