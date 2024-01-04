using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceBox : MonoBehaviour
{
    [SerializeField] private Camera diceCamera;

    [SerializeField] private Transform topTransform;
    [SerializeField] private Transform bottomTransform;
    [SerializeField] private Transform leftTransform;
    [SerializeField] private Transform rightTransform;

    // Start is called before the first frame update
    void Start()
    {
        var zPos = diceCamera.transform.localPosition.z;
        topTransform.localPosition = (Vector2)diceCamera.ViewportToWorldPoint(new Vector3(0.5f, 1f, zPos)) - new Vector2(0, topTransform.localScale.y / 2);
        bottomTransform.localPosition = (Vector2) diceCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, zPos)) + new Vector2(0, bottomTransform.localScale.y / 2);
        leftTransform.localPosition = (Vector2) diceCamera.ViewportToWorldPoint(new Vector3(0f, 0.5f, zPos)) + new Vector2(leftTransform.localScale.x / 2, 0);
        rightTransform.localPosition = (Vector2) diceCamera.ViewportToWorldPoint(new Vector3(1f, 0.5f, zPos)) - new Vector2(rightTransform.localScale.x / 2, 0);
    }
}
