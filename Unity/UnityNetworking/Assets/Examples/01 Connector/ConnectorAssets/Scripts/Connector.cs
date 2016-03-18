using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using Sfs2X;
using Sfs2X.Logging;
using Sfs2X.Util;
using Sfs2X.Core;
using Sfs2X.Entities;

namespace SFS2XExamples.Connector {
	public class Connector : MonoBehaviour {
	
		//----------------------------------------------------------
		// UI elements
		//----------------------------------------------------------
	
		public InputField hostInput;
		public InputField portInput;
		public Toggle debugToggle;
		public Button button;
		public Text buttonLabel;
		public ScrollRect debugScrollRect;
		public Text debugText;
	
		//----------------------------------------------------------
		// Private properties
		//----------------------------------------------------------
	
		private string defaultHost = "127.0.0.1";	// Default host
		private int defaultTcpPort = 9933;			// Default TCP port
		private int defaultWsPort = 8888;			// Default WebSocket port
		
		private SmartFox sfs;
	
		//----------------------------------------------------------
		// Unity calback methods
		//----------------------------------------------------------
	
		void Start() {
			// Load IP & TCP Port configuration from global Settings
			defaultHost = SFS2XExamples.Panel.Settings.ipAddress;
			defaultTcpPort = SFS2XExamples.Panel.Settings.port;

			// Initialize UI
			hostInput.text = defaultHost;
	
			#if !UNITY_WEBGL
			portInput.text = defaultTcpPort.ToString();
			#else
			portInput.text = defaultWsPort.ToString();
			#endif
	
			debugText.text = "";
		}
	
		void Update() {
			// As Unity is not thread safe, we process the queued up callbacks on every frame
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
	
		public void OnButtonClick() {
			if (sfs == null || !sfs.IsConnected) {
	
				// CONNECT
	
				#if UNITY_WEBPLAYER
				// Socket policy prefetch can be done if the client-server communication is not encrypted only (read link provided in the note above)
				if (!Security.PrefetchSocketPolicy(hostInput.text, Convert.ToInt32(portInput.text), 500)) {
					Debug.LogError("Security Exception. Policy file loading failed!");
				}
				#endif
	
				// Enable interface
				enableInterface(false);
				
				// Clear console
				debugText.text = "";
				debugScrollRect.verticalNormalizedPosition = 1;
				
				trace("Now connecting...");
				
				// Initialize SFS2X client and add listeners
				// WebGL build uses a different constructor
				#if !UNITY_WEBGL
				sfs = new SmartFox();
				#else
				sfs = new SmartFox(UseWebSocket.WS);
				#endif
				
				// Set ThreadSafeMode explicitly, or Windows Store builds will get a wrong default value (false)
				sfs.ThreadSafeMode = true;
				
				sfs.AddEventListener(SFSEvent.CONNECTION, OnConnection);
				sfs.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
	
				sfs.AddLogListener(LogLevel.INFO, OnInfoMessage);
				sfs.AddLogListener(LogLevel.WARN, OnWarnMessage);
				sfs.AddLogListener(LogLevel.ERROR, OnErrorMessage);
				
				// Set connection parameters
				ConfigData cfg = new ConfigData();
				cfg.Host = hostInput.text;
				cfg.Port = Convert.ToInt32(portInput.text);
				cfg.Zone = "BasicExamples";
				cfg.Debug = debugToggle.isOn;
					
				// Connect to SFS2X
				sfs.Connect(cfg);
			} else {
	
				// DISCONNECT
	
				// Disable button
				button.interactable = false;
				
				// Disconnect from SFS2X
				sfs.Disconnect();
			}
		}
	
		//----------------------------------------------------------
		// Private helper methods
		//----------------------------------------------------------
		
		private void enableInterface(bool enable) {
			hostInput.interactable = enable;
			portInput.interactable = enable;
			debugToggle.interactable = enable;
	
			button.interactable = enable;
			buttonLabel.text = "CONNECT";
		}
		
		private void trace(string msg) {
			debugText.text += (debugText.text != "" ? "\n" : "") + msg;
			debugScrollRect.verticalNormalizedPosition = 0;
		}
	
		private void reset() {
			// Remove SFS2X listeners
			sfs.RemoveEventListener(SFSEvent.CONNECTION, OnConnection);
			sfs.RemoveEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
	
			sfs.RemoveLogListener(LogLevel.INFO, OnInfoMessage);
			sfs.RemoveLogListener(LogLevel.WARN, OnWarnMessage);
			sfs.RemoveLogListener(LogLevel.ERROR, OnErrorMessage);
			
			sfs = null;
			
			// Enable interface
			enableInterface(true);
		}
	
		//----------------------------------------------------------
		// SmartFoxServer event listeners
		//----------------------------------------------------------
		
		private void OnConnection(BaseEvent evt) {
			if ((bool)evt.Params["success"]) {
				trace("Connection established successfully");
				trace("SFS2X API version: " + sfs.Version);
				trace("Connection mode is: " + sfs.ConnectionMode);
				
				// Enable disconnect button
				button.interactable = true;
				buttonLabel.text = "DISCONNECT";
			} else {
				trace("Connection failed; is the server running at all?");
				
				// Remove SFS2X listeners and re-enable interface
				reset();
			}
		}
		
		private void OnConnectionLost(BaseEvent evt) {
			trace("Connection was lost; reason is: " + (string)evt.Params["reason"]);
			
			// Remove SFS2X listeners and re-enable interface
			reset();
		}
		
		//----------------------------------------------------------
		// SmartFoxServer log event listeners
		//----------------------------------------------------------
		
		public void OnInfoMessage(BaseEvent evt) {
			string message = (string)evt.Params["message"];
			ShowLogMessage("INFO", message);
		}
		
		public void OnWarnMessage(BaseEvent evt) {
			string message = (string)evt.Params["message"];
			ShowLogMessage("WARN", message);
		}
		
		public void OnErrorMessage(BaseEvent evt) {
			string message = (string)evt.Params["message"];
			ShowLogMessage("ERROR", message);
		}
		
		private void ShowLogMessage(string level, string message) {
			message = "[SFS > " + level + "] " + message;
			trace(message);
			Debug.Log(message);
		}
	}
}