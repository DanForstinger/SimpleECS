using System.Collections;
using System.Collections.Generic;
using Mythiria;
using UnityEngine;
using UnityEngine.UI;

public class DiceRollTestUI : MonoBehaviour
{
    [SerializeField] private RawImage renderTarget;
    [SerializeField] private RectTransform diceSpawnTarget;
    [SerializeField] private DiceRollingController controller;
    [SerializeField] private Color diceColor;

    [SerializeField] private Image[] diceResultImages;
    
    // Start is called before the first frame update
    void Awake()
    {
        controller.SetRenderTarget(renderTarget);
    }
    
    public void TestRoll()
    {
        var pos = controller.diceCamera.ScreenToWorldPoint(new Vector3(diceSpawnTarget.position.x, diceSpawnTarget.position.y,
            -controller.diceCamera.transform.localPosition.z));

        controller.SimulateDiceRoll(pos, diceColor, 2, true, rolls =>
        {
            Debug.Log("Roll simulated!");
            for (int i = 0; i < rolls.Count && i < diceResultImages.Length; ++i)
            {
                var img = diceResultImages[i];
                var roll = rolls[i];
                var spr = roll.GetTexture();
                img.sprite = spr;
            }
        });    
    }
}
