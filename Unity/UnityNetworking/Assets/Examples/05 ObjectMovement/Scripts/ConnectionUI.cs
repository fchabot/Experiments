using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using Sfs2X;
using Sfs2X.Logging;
using Sfs2X.Util;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Requests;

namespace SFS2XExamples.ObjectMovement {
	public class ConnectionUI : MonoBehaviour {
	
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
	
		public InputField nameInput;
		public Button loginButton;
		public Text errorText;
	
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
			errorText.text = "";
		}
		
		void Update() {
			if (sfs != null)
				sfs.ProcessEvents();
		}

		// Handle disconnection automagically
		// ** Important for Windows users - can cause crashes otherwise
		void OnApplicationQuit() { 
			if (sfs != null && sfs.IsConnected) {
				sfs.Disconnect();
			}
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
			cfg.Zone = Zone;
			cfg.UseBlueBox = false;
			
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
			
			// Connect to SFS2X
			sfs.Connect(cfg);
		}
	
		//----------------------------------------------------------
		// Private helper methods
		//----------------------------------------------------------
		
		private void enableLoginUI(bool enable) {
			nameInput.interactable = enable;
			loginButton.interactable = enable;
			errorText.text = "";
		}
		
		private void reset() {
			// Remove SFS2X listeners
			sfs.RemoveAllEventListeners();
			
			// Enable interface
			enableLoginUI(true);
		}
	
		//----------------------------------------------------------
		// SmartFoxServer event listeners
		//----------------------------------------------------------
	
		private void OnConnection(BaseEvent evt) {
			if ((bool)evt.Params["success"]) {
				// Save reference to the SmartFox instance in a static field, to share it among different scenes
				SmartFoxConnection.Connection = sfs;
	
				// Login
				sfs.Send(new Sfs2X.Requests.LoginRequest(nameInput.text));
			}
			else {
				// Remove SFS2X listeners and re-enable interface
				reset();
				
				// Show error message
				errorText.text = "Connection failed; is the server running at all?";
			}
		}
		
		private void OnConnectionLost(BaseEvent evt) {
			// Remove SFS2X listeners and re-enable interface
			reset();
			
			string reason = (string) evt.Params["reason"];
			
			if (reason != ClientDisconnectionReason.MANUAL) {
				// Show error message
				errorText.text = "Connection was lost; reason is: " + reason;
			}
		}
		
		private void OnLogin(BaseEvent evt) {
			string roomName = "Game Room";
	
			// We either create the Game Room or join it if it exists already
			if (sfs.RoomManager.ContainsRoom(roomName)) {
				sfs.Send(new JoinRoomRequest(roomName));
			} else {
				RoomSettings settings = new RoomSettings(roomName);
				settings.MaxUsers = 40;
				sfs.Send(new CreateRoomRequest(settings, true));
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
			// Remove SFS2X listeners and re-enable interface before moving to the main game scene
			reset();
	
			// Go to main game scene
			Application.LoadLevel("05 ObjectMovementGame");
		}
		
		private void OnRoomJoinError(BaseEvent evt) {
			// Show error message
			errorText.text = "Room join failed: " + (string) evt.Params["errorMessage"];
		}
	}
}