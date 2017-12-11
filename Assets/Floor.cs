using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Floor : MonoBehaviour {

	//audio
	public AudioClip audio_bgm;
	public AudioClip audio_bird;
	public AudioClip audio_navi;
	public AudioClip audio_warning;
	public AudioClip audio_distance;
	public float volume_bgm;
	Dictionary<string, AudioClip> audio_bgms;
	string currentBGM = "";
	AudioSource audio_source_bgm;

	public AudioClip audio_goal;
	public AudioClip audio_break;
	public AudioClip audio_damage;
	public float volume_effects;
	AudioSource audio_source_effects;

	public GameObject blockPreb;
	public Blocks blocks;
	int dx = 10;
	int dz = 10;

	Transform floor;

	string playerName = "Player";
	string enemyName = "Enemy";
	string goalName = "Goal";
	string startName = "Start";

	Dictionary<string, int[]> objPositions = new Dictionary<string, int[]>();

	public GameObject playerPrefab;
	GameObject player;

	Camera birdEye;
	Camera playersEye;
	public bool start_bird_view;
	GameObject timerText;
	float timer = 0;
	ModalDialog dlg;

	Coroutine timerColor = null;
	IEnumerator TimerColor(Color c0, Color c1, float time){
		int div = 10;
		for (int cnt = 0; cnt < div; cnt++) {
			float r = (float)cnt / (div - 1);
			Color c = c0 * (1 - r) + c1 * r;
			timerText.GetComponent<Text> ().color = c;
			yield return new WaitForSeconds (time / div);
		}
		timerColor = null;
	}

	// Use this for initialization
	void Start () {
		floor = GetComponent<Transform> ();

		// Object setart position
		objPositions[playerName] = new int[] {0,0};
		objPositions[startName] = new int[] {0,0};
		objPositions[goalName] = new int[] {dx -1, dz -1};
		objPositions[enemyName] = new int[] {Mathf.RoundToInt(dx/2), Mathf.RoundToInt(dz /2 ) };

		//blocks
		blockPreb.GetComponent<Transform>().localScale =
			new Vector3(floor.localScale.x / dx, 1f, floor.localScale.z / dz);

		blocks = new Blocks (blockPreb, floor, dx, dz, "map");
		blocks.Init (objPositions);


		// Goal
		GameObject goal = GameObject.Find(goalName);
		goal.name = goalName;
		goal.GetComponent<Transform>().position = blocks.GetBlockPosition(
			objPositions[goalName][0], objPositions[goalName][1]);

		// walls
		Vector3 scale = blockPreb.GetComponent<Transform>().localScale;
		for (int angle = 0; angle < 360; angle += 90)
		{
			float x = Mathf.Cos(Mathf.Deg2Rad * angle);
			float z = Mathf.Sin(Mathf.Deg2Rad * angle);

			blockPreb.GetComponent<Transform>().localScale = new Vector3(
				Mathf.RoundToInt(z * 10) == 0 ? 0.01f : floor.localScale.x,
				scale.y,
				Mathf.RoundToInt(x * 10) == 0 ? 0.01f : floor.localScale.z
			);

			float px = x * floor.localScale.x / 2f;
			float pz = z * floor.localScale.z / 2f;
			float py = floor.localScale.y / 2f + floor.position.y + scale.y / 2f;
			Instantiate(blockPreb, new Vector3(px, py, pz), Quaternion.identity);
		}
		blockPreb.GetComponent<Transform>().localScale = scale;

		// player, enemy
		new string[] { playerName, enemyName }.Select ((v, i) => new {v,i}).All (
			item => {
				GameObject obj = Instantiate (playerPrefab);
				obj.name = item.v;
				Transform transform = obj.GetComponent<Transform> ();
				Vector3 p = blocks.GetBlockPosition (objPositions [item.v] [0], objPositions [item.v] [1]);
				//TODO ???
				p.y = floor.position.y + floor.localScale.y / 2f + transform.localScale.y * transform.Find ("Body").localScale.y / 2f;
				transform.position = p;
				PlayerCellController ctrl = obj.GetComponent<PlayerCellController> ();
			
				if (item.v == playerName) {
					player = obj;
					ctrl.AutoMovingSpan = 0f;

					ctrl.AddTriggerAction(goalName, () => {
						ctrl.CancelMotions();
						dlg.DoModal(name => {}, timer.ToString("0.0"));
						timer = 0.0f;

						transform.position = blocks.GetBlockPosition(objPositions[startName][0], objPositions[startName][1]);
						transform.localRotation = Quaternion.identity;

						audio_source_effects.PlayOneShot(audio_goal);
					});

					ctrl.AddTriggerAction(enemyName, () => {
						ctrl.CancelMotions();
						timer += 5.0f;

						if (timerColor != null) {
							StopCoroutine(timerColor);
						}
						timerColor = StartCoroutine(TimerColor(Color.red, Color.black, 5f));

						audio_source_effects.PlayOneShot(audio_damage);

						//transform.position = blocks.GetBlockPosition(objPositions[startName][0], objPositions[startName][1]);
						//transform.localRotation = Quaternion.identity;
					});
					
				} else if (item.v == enemyName) {
					
					ctrl.AutoMovingSpan = 5f;
					ctrl.SetColor(new Color32(165, 35, 86, 255));			
				}
				return true;
			}
		);

		birdEye = GameObject.FindWithTag ("MainCamera").GetComponent<Camera> ();
		birdEye.enabled = false;
		playersEye = player.GetComponent<Transform> ().Find ("Camera").GetComponent<Camera> ();
		playersEye.enabled = true;
		SetPlayerActionType ();
		if (start_bird_view == true) {
			ChangeCamera ();
		}
		timerText = GameObject.Find ("TimerText");

		// Modal
		dlg = GameObject.Find("Canvas").GetComponent<ModalDialog>();

		//audio
		audio_source_effects = gameObject.AddComponent<AudioSource>();
		audio_source_effects.volume = volume_effects;
		audio_source_bgm = gameObject.AddComponent<AudioSource>();
		audio_source_bgm.loop = true;
		audio_source_bgm.volume = volume_bgm;

		audio_bgms = new Dictionary<string, AudioClip>()
		{
			{"Default", audio_bgm },
			{"Bird",    audio_bird },
			{"Navi",    audio_navi },
			{"Warning", audio_warning },
			{"Distance",audio_distance },
		};
		BGM("Default");
	}

	//audio
	public void BGM(string type)
	{
		if (audio_source_bgm.clip != audio_bgms[type])
		{
			currentBGM = type;
			audio_source_bgm.clip = audio_bgms[type];
			audio_source_bgm.Play();
		}
	}
	public string BGM()
	{
		return currentBGM;
	}

	void SetPlayerActionType(){
		player.GetComponent<PlayerCellController> ().ActionType = birdEye.enabled ? 0 : 1;
	}

	public void ChangeCamera(){
	
		birdEye.enabled = !birdEye.enabled;
		playersEye.enabled = !playersEye.enabled;
		SetPlayerActionType ();
	}

	public void UpdateObjPosition(string name, Vector3 pos, Quaternion rot){
		int[] index = blocks.GetBlockIndexXZ (pos);
		objPositions [name] = index;
	}

	// Update is called once per frame
	void Update () {

		if (dlg.Active == true) {
			return;
		}

		if (birdEye.enabled == true) {
			int i = Enumerable.Range (1, 2).FirstOrDefault (v => Input.GetMouseButtonDown (v - 1));

			if (i != 0) {
				Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
				RaycastHit hit = new RaycastHit ();
				if (Physics.Raycast (ray.origin, ray.direction, out hit, Mathf.Infinity)) {
					Blocks.BlockObj target = blocks.Find (hit.collider.gameObject);
					if (i == 2 && target != null) {
						blocks.RemoveBlock (target);
					} else if (i == 1 && gameObject == hit.collider.gameObject) {
						int[] index = blocks.GetBlockIndexXZ (hit.point);
						blocks.CreateBlock (index [0], index [1]);
					}
				}
			}
		}

		timer += Time.deltaTime;
		timerText.GetComponent<Text>().text = timer.ToString("0.0");
	}
}
