using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using Sfs2X;
using Sfs2X.Logging;
using Sfs2X.Util;
using Sfs2X.Core;
using Sfs2X.Entities;

namespace SFS2XExamples.Lobby {
	public class Lobby : MonoBehaviour {
	
		//----------------------------------------------------------
		// Editor public properties
		//----------------------------------------------------------
	
		[Tooltip("IP address or domain name of the SmartFoxServer 2X instance")]
		public string Host = "127.0.0.1";
	
		[Tooltip("TCP port listened by the SmartFoxServer 2X instance; used for regular socket connection in all builds except WebGL")]
		public int TcpPort = 9933;
	
		[Tooltip("WebSocket port listened by the SmartFoxServer 2X instance; used for in WebGL build only")]
		public int WSPort = 8888;
	
		[Tooltip("Name of the SmartFoxServer 2X Zone to join")]
		public string Zone = "BasicExamples";
	
		//----------------------------------------------------------
		// UI elements
		//----------------------------------------------------------
		
		public Animator cameraAnimator;
	
		public InputField zoneInput;
		public InputField nameInput;
		public Button loginButton;
		public Text errorText;
	
		public Text chatText;
		public CanvasGroup chatControls;
		public Text userListText;
		public Text helloText;
		public Transform roomListContent;
		public GameObject roomListItem;
	
		//----------------------------------------------------------
		// Private properties
		//----------------------------------------------------------
	
		private SmartFox sfs;
	
		//----------------------------------------------------------
		// Unity calback methods
		//----------------------------------------------------------
	
		void Start() {
			// Load IP & TCP Port configuration from global Settings
			Host = SFS2XExamples.Panel.Settings.ipAddress;
			TcpPort = SFS2XExamples.Panel.Settings.port;

			#if UNITY_WEBPLAYER
			if (!Security.PrefetchSocketPolicy(Host, TcpPort, 500)) {
				Debug.LogError("Security Exception. Policy file loading failed!");
			}
			#endif
	
			// Initialize UI
			zoneInput.text = Zone;
			errorText.text = "";
		}
		
		// Update is called once per frame
		void Update() {
			if (sfs != null)
				sfs.ProcessEvents();
		}

		// Disconnect from the socket when shutting down the game
		// ** Important for Windows users - can cause crashes otherwise
		public void OnApplicationQuit() {
			if (sfs != null && sfs.IsConnected)
				sfs.Disconnect();
			
			sfs = null;
		}
		
		// Disconnect from the socket when ordered by the main Panel scene
		// ** Important for Windows users - can cause crashes otherwise
		public void Disconnect() {
			OnApplicationQuit();
		}
	
		//----------------------------------------------------------
		// Public interface methods for UI
		//----------------------------------------------------------
	
		public void OnLoginButtonClick() {
			enableLoginUI(false);
			
			// Set connection parameters
			ConfigData cfg = new ConfigData();
			cfg.Host = Host;
			#if !UNITY_WEBGL
			cfg.Port = TcpPort;
			#else
			cfg.Port = WSPort;
			#endif
			cfg.Zone = zoneInput.text;
			
			// Initialize SFS2X client and add listeners
			#if !UNITY_WEBGL
			sfs = new SmartFox();
			#else
			sfs = new SmartFox(UseWebSocket.WS);
			#endif
			
			// Set ThreadSafeMode explicitly, or Windows Store builds will get a wrong default value (false)
			sfs.ThreadSafeMode = true;
			
			sfs.AddEventListener(SFSEvent.CONNECTION, OnConnection);
			sfs.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
			sfs.AddEventListener(SFSEvent.LOGIN, OnLogin);
			sfs.AddEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
			sfs.AddEventListener(SFSEvent.ROOM_JOIN, OnRoomJoin);
			sfs.AddEventListener(SFSEvent.ROOM_JOIN_ERROR, OnRoomJoinError);
			sfs.AddEventListener(SFSEvent.PUBLIC_MESSAGE, OnPublicMessage);
			sfs.AddEventListener(SFSEvent.USER_ENTER_ROOM, OnUserEnterRoom);
			sfs.AddEventListener(SFSEvent.USER_EXIT_ROOM, OnUserExitRoom);
			
			// Connect to SFS2X
			sfs.Connect(cfg);
		}
	
		public void OnSendMessageButtonClick() {
			InputField msgField = (InputField) chatControls.GetComponentInChildren<InputField>();
	
			if (msgField.text != "") {
				// Send public message to Room
				sfs.Send (new Sfs2X.Requests.PublicMessageRequest(msgField.text));
	
				// Reset message field
				msgField.text = "";
			}
		}
	
		public void OnDisconnectButtonClick() {
			// Disconnect from server
			sfs.Disconnect();
		}
		
		public void OnRoomItemClick(int roomId) {
			sfs.Send(new Sfs2X.Requests.JoinRoomRequest(roomId));
		}
	
		//----------------------------------------------------------
		// Private helper methods
		//----------------------------------------------------------
		
		private void enableLoginUI(bool enable) {
			zoneInput.interactable = enable;
			nameInput.interactable = enable;
			loginButton.interactable = enable;
			errorText.text = "";
		}
		
		private void reset() {
			// Remove SFS2X listeners
			sfs.RemoveEventListener(SFSEvent.CONNECTION, OnConnection);
			sfs.RemoveEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
			sfs.RemoveEventListener(SFSEvent.LOGIN, OnLogin);
			sfs.RemoveEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
			sfs.RemoveEventListener(SFSEvent.ROOM_JOIN, OnRoomJoin);
			sfs.RemoveEventListener(SFSEvent.ROOM_JOIN_ERROR, OnRoomJoinError);
			sfs.RemoveEventListener(SFSEvent.PUBLIC_MESSAGE, OnPublicMessage);
			sfs.RemoveEventListener(SFSEvent.USER_ENTER_ROOM, OnUserEnterRoom);
			sfs.RemoveEventListener(SFSEvent.USER_EXIT_ROOM, OnUserExitRoom);
			
			sfs = null;
			
			// Enable interface
			enableLoginUI(true);
		}
	
		private void printSystemMessage(string message) {
			chatText.text += "<color=#808080ff>" + message + "</color>\n";
		}
		
		private void printUserMessage(User user, string message) {
			chatText.text += "<b>" + (user == sfs.MySelf ? "You" : user.Name) + ":</b> " + message + "\n";
		}
	
		private void populateRoomList(List<Room> rooms) {
			// For the roomlist we use a scrollable area containing a separate prefab button for each Room
			// Buttons are clickable to join Rooms
	
			foreach (Room room in rooms) {
				int roomId = room.Id;
	
				GameObject newListItem = Instantiate(roomListItem) as GameObject;
				RoomItem roomItem = newListItem.GetComponent<RoomItem>();
				roomItem.nameLabel.text = room.Name;
				roomItem.maxUsersLabel.text = "[max " + room.MaxUsers + " users]";
				roomItem.roomId = roomId;
	
				roomItem.button.onClick.AddListener(() => OnRoomItemClick(roomId));
	
				newListItem.transform.SetParent(roomListContent, false);
			}
		}
	
		private void populateUserList(List<User> users) {
			// For the userlist we use a simple text area, with a user name in each row
			// No interaction is possible in this example
	
			// Get user names
			List<string> userNames = new List<string>();
	
			foreach (User user in users) {
				if (user != sfs.MySelf)
					userNames.Add(user.Name);
			}
	
			// Sort list
			userNames.Sort();
	
			// Display list
			userListText.text = "";
			userListText.text = String.Join("\n", userNames.ToArray());
		}
	
		//----------------------------------------------------------
		// SmartFoxServer event listeners
		//----------------------------------------------------------
	
		private void OnConnection(BaseEvent evt) {
			if ((bool)evt.Params["success"])
			{
				// Login
				sfs.Send(new Sfs2X.Requests.LoginRequest(nameInput.text));
			}
			else
			{
				// Remove SFS2X listeners and re-enable interface
				reset();
	
				// Show error message
				errorText.text = "Connection failed; is the server running at all?";
			}
		}
		
		private void OnConnectionLost(BaseEvent evt) {
			// Rotate camera to login panel
			cameraAnimator.SetBool("loggedIn", false);
	
			// Remove SFS2X listeners and re-enable interface
			reset();
	
			string reason = (string) evt.Params["reason"];
	
			if (reason != ClientDisconnectionReason.MANUAL) {
				// Show error message
				errorText.text = "Connection was lost; reason is: " + reason;
			}
		}
		
		private void OnLogin(BaseEvent evt) {
			User user = (User) evt.Params["user"];
	
			// Rotate camera to main panel
			cameraAnimator.SetBool("loggedIn", true);
	
			// Set "Hello" text
			helloText.text = "Hello " + user.Name;
	
			// Clear chat panel, user list
			chatText.text = "";
			userListText.text = "";
	
			// Show system message
			string msg = "Connection established successfully\n";
			msg += "SFS2X API version: " + sfs.Version + "\n";
			msg += "Connection mode is: " + sfs.ConnectionMode + "\n";
			msg += "Logged in as " + user.Name;
			printSystemMessage(msg);
	
			// Populate Room list
			populateRoomList(sfs.RoomList);
	
			// Join first Room in Zone
			if (sfs.RoomList.Count > 0) {
				sfs.Send(new Sfs2X.Requests.JoinRoomRequest(sfs.RoomList[0].Name));
			}
		}
		
		private void OnLoginError(BaseEvent evt) {
			// Disconnect
			sfs.Disconnect();
	
			// Remove SFS2X listeners and re-enable interface
			reset();
			
			// Show error message
			errorText.text = "Login failed: " + (string) evt.Params["errorMessage"];
		}
		
		private void OnRoomJoin(BaseEvent evt) {
			Room room = (Room) evt.Params["room"];
			
			// Show system message
			printSystemMessage("\nYou joined room '" + room.Name + "'\n");
	
			// Enable chat controls
			chatControls.interactable = true;
	
			// Populate users list
			populateUserList(room.UserList);
		}
		
		private void OnRoomJoinError(BaseEvent evt) {
			// Show error message
			printSystemMessage("Room join failed: " + (string) evt.Params["errorMessage"]);
		}
		
		private void OnPublicMessage(BaseEvent evt) {
			User sender = (User) evt.Params["sender"];
			string message = (string) evt.Params["message"];
	
			printUserMessage(sender, message);
		}
		
		private void OnUserEnterRoom(BaseEvent evt) {
			User user = (User) evt.Params["user"];
			Room room = (Room) evt.Params["room"];
	
			// Show system message
			printSystemMessage("User " + user.Name + " entered the room");
	
			// Populate users list
			populateUserList(room.UserList);
		}
		
		private void OnUserExitRoom(BaseEvent evt) {
			User user = (User) evt.Params["user"];
	
			if (user != sfs.MySelf) {
				Room room = (Room)evt.Params["room"];
				
				// Show system message
				printSystemMessage("User " + user.Name + " left the room");
				
				// Populate users list
				populateUserList(room.UserList);
			}
		}
	}
}