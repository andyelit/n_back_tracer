﻿using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections;
using DG.Tweening;
using UniRx;

public class TouchEffect : MonoBehaviour {
	[SerializeField] GameObject[] elements;
	float animTime = 0.4f;

	void Start () {
		Observable.Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(100))
			.Zip(elements.ToObservable(), (_, obj) => obj)
				.Do(obj => obj.transform.SetParent(transform))
				.Do(obj => obj.transform.position = transform.position)
				.Subscribe(obj => Animate(obj), () => Observable.Timer(TimeSpan.FromSeconds(animTime))
				           .Subscribe(_ => DestroyObject(gameObject))
				           .AddTo(gameObject))
				.AddTo(gameObject);
	}
	
	void Animate(GameObject obj) {
		var image = obj.GetComponent<Image>();

		DOTween.To(() => Vector3.zero, scale => obj.transform.localScale = scale, Vector3.one * 2, animTime);
		DOTween.To(() => Color.white, color => image.color = color, new Color(1, 1, 1, 0), animTime);
	}
}
