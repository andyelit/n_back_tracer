using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameController : MonoBehaviour {
	PatternTracer patternTracer;
	TimeKeeper timeKeeper;
	Text timeLimitText;
	ScreenEffectManager screenEffectManager;

	int cachedTouchTileId = -1;
	int currentTouchTileId = -1;

	enum GameState {
		Standby,
		PriorNRun,
		Wait,
		Play,
		FinishEffect,
		Finish
	};
	GameState gameState = GameState.Standby;

	void Awake() {
		timeKeeper = GetComponent<TimeKeeper>();
		patternTracer = GetComponent<PatternTracer> ();
		screenEffectManager = GetComponent<ScreenEffectManager>();

		timeKeeper.TimeUp += () => {
			gameState = GameState.FinishEffect;
		};
		patternTracer.PriorNRunEnded += () => {
			gameState = GameState.Play;
			timeKeeper.StartCountdown ();
		};

		timeLimitText = GameObject.Find ("TimeLimit").GetComponent<Text>();
	}

	public void TouchedTile(int tileId) {
		if (gameState != GameState.Play)
			return;

		currentTouchTileId = tileId;
	}

	void Update () {
		switch (gameState) {
		case GameState.Standby:
			gameState = GameState.PriorNRun;
			break;
		
		case GameState.PriorNRun:
			patternTracer.StartPriorNRun();
			gameState = GameState.Wait;
			break;
		
		case GameState.Wait:
			// do nothing
			break;
		
		case GameState.Play:
			if (cachedTouchTileId != currentTouchTileId) {
				patternTracer.Touch(currentTouchTileId);
				cachedTouchTileId = currentTouchTileId;
			}
			timeLimitText.text = "Limit: " + timeKeeper.GetRemainingTime().ToString ();
			break;
		
		case GameState.FinishEffect:
			screenEffectManager.EmitFinishAnimation(() => {
				gameState = GameState.Finish;
			});
			gameState = GameState.Wait;
			break;
		
		case GameState.Finish:
			GameObject storageObject = GameObject.Find ("StorageObject");
			Storage storage = storageObject ? storageObject.GetComponent<Storage>() : null;

			if (storage) {
				storage.Set("Score", GetComponent<ScoreManager>().GetScore());
			}
			Application.LoadLevel ("Result");
			break;

		default:
			break;
		
		}
	}
}
