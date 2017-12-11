using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCellController : MonoBehaviour {


	Dictionary<string, int[]> nextPosition = new Dictionary<string, int[]>(){
		{"up", new int[]{0,1,0,1}},
		{"down", new int[]{0,-1,0,1}},
		{"left", new int[]{-1,0,0,1}},
		{"right", new int[]{1,0,0,1}},
	};
	Dictionary<string, int[]> nextAction = new Dictionary<string, int[]>(){
		{"up", new int[]{0,1,0,0}},
		{"down", new int[]{0,-1,0,0}},
		{"left", new int[]{0,0,-90,0}},
		{"right", new int[]{0,0,90,0}},
	};

	Dictionary<string, int[]> actions;
	public int ActionType {
		set { actions = value == 0 ? nextPosition : nextAction; }
	}

	Floor floor;
	PlayerMotion pmotion;
	ModalDialog dlg;

	public float AutoMovingSpan{ get; set;}
	float autoMovedTime = 0f;
	float autoMovingSpeed = 1.0f;

	//audio
	public AudioClip audio_walk;
	public AudioClip audio_turn;
	public AudioClip audio_hit_wall;
	public float volume = 0.1f;
	Dictionary<string, AudioClip> sounds;
	AudioSource audio_source;

	Dictionary<string, Action> triggerActions = new Dictionary<string, Action> ();
	public void AddTriggerAction(string opponent, Action a){
		triggerActions [opponent] = a;
	}

	private void OnTriggerEnter(Collider other) {
		if (triggerActions.ContainsKey (other.name)) {
			triggerActions [other.name] ();	
		}
	}
		
	// Use this for initialization
	void Start () {

		//base
		floor = GameObject.Find ("Floor").GetComponent<Floor> ();
		pmotion = GetComponent<PlayerMotion> ();

		//modal
		dlg = GameObject.Find ("Canvas").GetComponent<ModalDialog> ();

		//audio
		audio_source = gameObject.AddComponent<AudioSource>();
		sounds = new Dictionary<string, AudioClip>(){
			{ "walk", audio_walk },
			{ "turn", audio_turn },
			{ "hit_wall",audio_hit_wall },
		};

	}
	// Update is called once per frame
	void Update () {

		if (dlg.Active == true) {
			return;
		}

		if (AutoMovingSpan == 0) {
			foreach (var elem in actions) {
				if (Input.GetKeyDown (elem.Key)) {
					Move (elem.Value);
				}
			}
		} else if (Time.realtimeSinceStartup > autoMovedTime + AutoMovingSpan / autoMovingSpeed) {
			autoMovedTime = Time.realtimeSinceStartup;
			pmotion.Unset ();

			int[] pos = floor.blocks.GetBlockIndexXZ(GetComponent<Transform>().position);
			List<string> avail = new List<string>();
			foreach (var d in nextPosition){
				if (floor.blocks.IsWall(pos[0] + d.Value[0], pos[1] + d.Value[1]) == false) {
					avail.Add(d.Key);
				}
			}
			if (avail.Count != 0) {
				Move (nextPosition[avail[UnityEngine.Random.Range(0, avail.Count)]]);
			}
		}
		floor.UpdateObjPosition(gameObject.name, GetComponent<Transform>().position, GetComponent<Transform>().rotation);
	}
	public void SetColor(Color32 col){
		GetComponent<Transform> ().Find ("Body").GetComponent<Renderer> ().material.color = col;
	}

	public void Move(int[] pos, Action aniComplete = null)
	{
		pmotion.Unset();
		if (pos[0] != 0 || pos[1] != 0)
		{
			Vector3 d = new Vector3(pos[0], 0, pos[1]);
			if (pos[3] == 1)
			{
				Quaternion q = new Quaternion();
				q.SetFromToRotation(Vector3.forward, new Vector3(pos[0], 0, pos[1]));
				int y = Mathf.RoundToInt((q.eulerAngles.y - GetComponent<Transform>().eulerAngles.y)) % 360;
				if (y != 0)
				{
					Turn(NormalizedDegree(y), null);
				}
			}
			else
			{
				d = GetComponent<Transform>().localRotation * d;
			}
			int[] index = floor.blocks.GetBlockIndexXZ(GetComponent<Transform>().position);
			Forward(index[0] + Mathf.RoundToInt(d.x), index[1] + Mathf.RoundToInt(d.z), aniComplete);
		}
		if (pos[2] != 0)
		{
			Turn(pos[2], aniComplete);
		}
	}

	float NormalizedDegree(float deg)
	{
		while (deg >= 180)
		{
			deg -= 360;
		}
		while (deg < -180)
		{
			deg += 360;
		}
		return deg;
	}
	void Forward(int x, int z, Action aniComplete){
		if (floor.blocks.IsWall (x, z) == false) {
			Vector3 pos0 = GetComponent<Transform> ().position;
			Vector3 pos1 = floor.blocks.GetBlockPosition (x, z);
			pos1.y = pos0.y;
			//TODO ???
			pmotion.Add (p => {
				GetComponent<Transform> ().position = (pos1 - pos0) * p + pos0;
			}, 0.5f, aniComplete, sounds["walk"], volume);
		} else {
			audio_source.PlayOneShot (sounds["hit_wall"], volume);
		}
	}
	void Turn(float deg, Action aniComplete){
		float deg0 = GetComponent<Transform>().eulerAngles.y;
		float deg1 = RoundDegree(deg0 + deg);
		pmotion.Add(p =>
			{
				GetComponent<Transform>().rotation = Quaternion.Euler(0f, (deg1 - deg0) * p + deg0, 0f);
			}, 0.5f, aniComplete, sounds["turn"], volume);
	}
	float RoundDegree(float deg)
	{
		return Mathf.FloorToInt((deg + 45) / 90) * 90;
	}

	public void CancelMotions(){
		pmotion.Cancel();
	}
}
