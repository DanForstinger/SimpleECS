using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class DirectionTransition : Transition
{
 public enum LerpDirection
    {
        None,
        Top, 
        Right,
        Bottom,
        Left
    }

    [SerializeField] private LerpDirection lerpDirection = LerpDirection.None;
    
    protected const float OFFSCREEN_X = 1500;
    protected const float OFFSCREEN_Y = 1500;

    [SerializeField] private float hiddenTweenSpeed = 0.5f;
    [SerializeField] private float shownTweenSpeed = 0.6f;

    private Vector3 containerPos = Vector3.one;
    
    protected override IEnumerator OnTransitionIn()
    {
        // try to lerp.
        var canvas = GetComponentInChildren<Canvas>();

        if (canvas != null)
        {
            // now, show our screen
            if (lerpDirection != LerpDirection.None)
            {
                var container = canvas.transform.Find("Container");
                if (container == null) Debug.LogError("Error: You must have a child with name 'Container' to use this screen as an overlay.");
                
                if (containerPos == Vector3.one)
                {
                    containerPos = container.localPosition;
                }
                
                var lerpPosition = CalculateOffscreenPosition(lerpDirection);
                container.transform.localPosition = lerpPosition;
                container.transform.DOLocalMove(containerPos, shownTweenSpeed).SetEase(Ease.OutCirc);

                var wait = new WaitForSeconds(shownTweenSpeed);
                yield return wait;

                Debug.Log("Finished transition...");
            }
        }
    }

    protected override IEnumerator OnTransitionOut()
    {
        // try to lerp.
        var canvas = GetComponentInChildren<Canvas>();

        if (canvas != null)
        {
            // now, show our screen
            if (lerpDirection != LerpDirection.None)
            {
                var container = canvas.transform.Find("Container");
                if (container == null) Debug.LogError("Error: You must have a child with name 'Container' to use this screen as an overlay.");

                var lerpPosition = CalculateOffscreenPosition(lerpDirection);
                container.transform.localPosition = containerPos;
                
                container.transform.DOLocalMove(lerpPosition, hiddenTweenSpeed).SetEase(Ease.OutCirc);

                var wait = new WaitForSeconds(hiddenTweenSpeed);
                yield return wait;
            }
        }
    }
    
    private Vector2 CalculateOffscreenPosition(LerpDirection lerpDirection)
    {
        switch (lerpDirection)
        {
            case LerpDirection.Top:
                return new Vector2(0, OFFSCREEN_Y);
            case LerpDirection.Bottom:
                return new Vector2(0, -OFFSCREEN_Y);
            case LerpDirection.Left:
                return new Vector2(-OFFSCREEN_X, 0);
            case LerpDirection.Right:
                return new Vector2(OFFSCREEN_X, 0);
                
        }

        return Vector2.zero;
    }
}
