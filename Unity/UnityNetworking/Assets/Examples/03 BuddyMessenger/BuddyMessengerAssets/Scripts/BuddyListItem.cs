using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace SFS2XExamples.BuddyMessenger {
	public class BuddyListItem : MonoBehaviour {
		public Image stateIcon;
		public Text mainLabel;
		public Text moodLabel;
		public Button blockButton;
		public Button chatButton;
		public Button removeButton;
	
		public string buddyName;
	}
}