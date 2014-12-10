﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class PatternRunner : MonoBehaviour {
	const int tileNum = 4 * 5;
	Tile[] tiles = new Tile[tileNum];

	int backNum;
	List<List<int>> patterns = new List<List<int>>();

	PatternGenerator patternGenerator;
	ScoreManager scoreManager;
	GameController gameController;

	int currentPattern = 0;
	int currentIndex = 0;

	void ApplyStates(GameObject storageObject) {
		Storage storage = storageObject ? storageObject.GetComponent<Storage> () : null;

		backNum = storage ? storage.Get("BackNum") : 1 /* default N */;
		patternGenerator.ChainLength = storage ? storage.Get ("Length") : 4 /* default Chain Num */;
	}

	void Awake() {
		gameController = GetComponent<GameController>();
		scoreManager = GetComponent<ScoreManager>();

		patternGenerator = GetComponent<PatternGenerator>();
		patternGenerator.FieldWidth = 4;
		patternGenerator.FieldHeight = 5;

		ApplyStates (GameObject.Find ("StorageObject"));


		// nBack分リスト初期化
		for (int i = 0; i <= backNum; i++) {
			patterns.Add(new List<int>());
		}
		
		// nBack分パターン初期化
		for (int i = 0; i <= backNum; i++) {
			UpdatePattern (currentPattern + i, new List<int>());
		}

		// タイル配列初期化
		for (int i = 0; i < tileNum; i++) {
			tiles[i] = GameObject.Find("Tile" + i.ToString()).GetComponent<Tile>();		
			tiles[i].TileId = i;
		}
	}

	/* テスト用 */
	string ListToString(List<int> list) {
		string res = "";

		foreach (var i in list) {
			res += i.ToString() + " ";
		}
		return res;
	}
	
	bool isStandby = true;
	float hintAnimationTriggerTimer = 0;
	Action updateAnimation;

	// 名前がちょっと変
	void StartAnimation(float interval, int targetPattern, int index, Action<Tile> tileEffectStartDelegate) {
		float timer = 0;

		updateAnimation = () => {
			if ((timer += Time.deltaTime) < interval)
				return;
			
			timer = 0;
			tileEffectStartDelegate (tiles [patterns [targetPattern] [index]]);
			index++;

			if (index >= patterns [targetPattern].Count) {
				updateAnimation = null;
			}
		};
	}
	
	float timer = 0;
	void Update() {
		if (updateAnimation != null) {
			updateAnimation();
		}

		// スタート時のnBarkRun
		if (isStandby) {
			if (updateAnimation != null)
				return;

			timer += Time.deltaTime;
			if (timer < 0.9f)
				return;

			// 次のパターンを走らせるまで少し時間を置く
			timer = 0;
			currentPattern++;
			
			// 条件も仮
			if (currentPattern >= backNum) {
				// finish
				gameController.FinishNBackRun();
				currentPattern = 0;
				isStandby = false;
			} else {
				StartNBackRun();
			}
		
		} else {
			hintAnimationTriggerTimer += Time.deltaTime;
			if (hintAnimationTriggerTimer < 2f) {
				return;
			}
			hintAnimationTriggerTimer = 0;
			
			// start hint animation
			StartAnimation(
				0.10f,
				currentPattern,
				currentIndex,
				(Tile tile) => tile.StartHintEffect()
			);
		}
	}

	public void StartNBackRun() {
		StartAnimation (0.12f, 0, 0, (Tile tile) => tile.StartMarkEffect());
	}
	
	int LoopIndex(int next, int end) {
		if (next < 0) {
			return LoopIndex(end + (next + 1), end);
		}
		return next > end ? LoopIndex(--next - end, end) : next;
	}

	void UpdatePattern(int targetPattern, List<int> ignoreList) {
		patterns [targetPattern] = patternGenerator.Generate (ref ignoreList);
	}

	List<int> BuildIgnoreList(int targetPattern) {
		List<int> ignoreList = new List<int>();

		// トリガーになったやつ
		// List<int> triggerPattern = patterns[LoopIndex (currentPattern - backNum, backNum)];
		// ignoreList.Add (triggerPattern[triggerPattern.Count - 1]);

		foreach (int i in patterns[LoopIndex (targetPattern - backNum, backNum)]) {
			ignoreList.Add(i);
		}

		// 次に出す一個前のパターン全部
		foreach (int i in patterns[LoopIndex(targetPattern - 1, backNum)]) {
			ignoreList.Add(i);
		}

		return ignoreList;
	}

	void PatternIncrement() {
		currentPattern = LoopIndex (currentPattern + 1, backNum);

		int targetPattern = LoopIndex (currentPattern + backNum, backNum);
		List<int> ignoreList = BuildIgnoreList (targetPattern);
		UpdatePattern (targetPattern, ignoreList);
	}

	void IndexIncrement() {
		currentIndex = LoopIndex (currentIndex + 1, patterns [currentPattern].Count - 1);

		// Correct pattern
		if (currentIndex == 0) {
			scoreManager.CorrectPattern();
			PatternIncrement();
		}
	}

	public void Touch(int tileId) {
		hintAnimationTriggerTimer = 0;

		// Correct touch
		if (patterns [currentPattern] [currentIndex] == tileId) {
			scoreManager.CorrectTouch();
			tiles [patterns [currentPattern] [currentIndex]].StartCorrectEffect ();
			
			// start next pattern animation
			if (currentIndex == patternGenerator.ChainLength - 1) {
				StartAnimation(
					0.10f,
					LoopIndex (currentPattern + backNum, backNum),
					0,
					(Tile tile) => tile.StartMarkEffect()
				);
			}
			IndexIncrement ();
			return;
		}
		
		scoreManager.Miss ();
		tiles [tileId].StartMissEffect ();
	}
}
