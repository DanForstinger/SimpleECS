using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class AlphaTransition : Transition
{
    [SerializeField] private CanvasGroup alphaGroup;
    [SerializeField] private float alphaTweenSpeed = 1f;
    
    protected override IEnumerator OnTransitionIn()
    {
        var tween = DOTween.To(() => alphaGroup.alpha, value => alphaGroup.alpha = value, 1, alphaTweenSpeed).SetEase(Ease.OutSine);
        yield return tween.WaitForCompletion();
    }

    protected override IEnumerator OnTransitionOut()
    {
        var tween = DOTween.To(() => alphaGroup.alpha, value => alphaGroup.alpha = value, 0, alphaTweenSpeed).SetEase(Ease.OutSine);
        yield return tween.WaitForCompletion();
    }
}
