using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ModalDialog : MonoBehaviour {

	GameObject modalPanel;
	public bool Active { get; private set;}
	List<GameObject> gameObjects = new List<GameObject>();
	Action<string> done;

	// Use this for initialization
	void Start () {
		this.Active = false;
		modalPanel = GetComponent<Transform> ().Find ("ModalPanel").gameObject;
		int sindex = modalPanel.GetComponent<Transform> ().GetSiblingIndex ();
		foreach (Transform c in GetComponent<Transform>()) {
			if (sindex <= c.GetSiblingIndex ()) {
				gameObjects.Add (c.gameObject);
			}
		}
		Cancel();
		gameObjects.ToList().ForEach(o => {
			Button b = o.GetComponent<Button>();
			if (b != null){
				b.onClick.AddListener(() => onClicked(b.name));
			}
		});
	}
	void onClicked(string name){
		if (this.done != null){
			this.done(name);
		}
		Cancel ();
	}

	public void DoModal(Action<string> done, string text = ""){
		this.Active = true;
		this.done = done;

		// replace model text to time score
		gameObjects.Where (o => o.name == "Text").First ().GetComponent<Text> ().text = text;
		gameObjects.ForEach (o => o.SetActive (true));
		StartCoroutine (Fade (0.1f));
	}

	IEnumerator Fade(float df){
		var c = modalPanel.GetComponent <CanvasRenderer> ().GetColor ();
		var a0 = df > 0 ? 0f : 1f;
		var count = Mathf.Abs (Mathf.RoundToInt (1.0f / df));

		for (int cnt = 0; cnt < count + 1; cnt++) {
			c.a = a0 + df * cnt;
			modalPanel.GetComponent<CanvasRenderer> ().SetColor (c);
			yield return new WaitForSeconds (0.1f);
		}
	}

	public void Cancel(){
		this.Active = false;
		gameObjects.ToList ().ForEach (o => o.SetActive (false));
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
