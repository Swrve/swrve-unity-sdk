using UnityEngine;
using System.Collections;

public class MainScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
		SwrveComponent.Instance.Init (1, "your_api_key");
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
