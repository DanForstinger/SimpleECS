using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mythiria
{

    public class DiceRollingController : MonoBehaviour
    {
        public Camera diceCamera;

        [SerializeField] private DiceRoller dicePrefab;

        private List<DiceRoller> rolls;
        
        public void SetRenderTarget(RawImage image)
        {
            var texture = new RenderTexture(Screen.width, Screen.height, 32);
            diceCamera.targetTexture = texture;
            image.texture = texture;
        }

        /// Note: For this function, the user is responsible for destroying the roller themselves, otherwise it will stick around.
        public void ReplayDiceRoll(DiceRoller roller, Color color)
        {
            roller.ReplayRoll(roller =>
            {
                Debug.Log("Dice roll replay complete!");
            });
        }
        
        public void SimulateDiceRoll(Vector3 spawnPos, Color color, int count, bool autoReplay, System.Action<List<DiceRoller>> callback)
        {
            Debug.Log("Rolling dice...");
            StartCoroutine(ExecSimulateDiceRoll(count, spawnPos, color, autoReplay, callback));
        }

        private IEnumerator ExecSimulateDiceRoll(int count,  Vector3 spawnPos, Color color, bool autoReplay, System.Action<List<DiceRoller>> callback)
        {
            // remove existing dice rollers.
            if (rolls != null)
            {
                for (int r = rolls.Count - 1; r >= 0; --r)
                {
                    Destroy(rolls[r].gameObject);
                }
            }
            
            rolls = new List<DiceRoller>();

            for (int r = 0; r < count; ++r)
            {
                var offsetX = -(count / 2) + r;
                
                var roller = CreateDiceRoller(spawnPos + new Vector3(offsetX, 0,0), color);
                roller.StartSimulate();
                rolls.Add(roller);
            }

            Physics.autoSimulation = false;
      
            const int maxIter = 5000;
            int i = 0;
            float timeStep = 0;
            diceCamera.enabled = false;
            
            while (rolls.Exists(r => r.IsRolling) && i < maxIter)
            {
                foreach (var roller in rolls)
                {
                    roller.StepSimulate(Time.fixedDeltaTime);
                }

                Physics.Simulate(Time.fixedDeltaTime);
                ++i;
            }

            Physics.autoSimulation = true;

            diceCamera.enabled = true;

            foreach (var pair in rolls)
            {
                var result = pair.dice.GetDieSideMatchInfo().closestMatch.values[0];
                Debug.Log(string.Format("Simulated rolling! Result: {0}", result));
                pair.roll.result = result;
            }
            
            if (autoReplay)
            {
                var rollCount = rolls.Count;
                
                foreach (var pair in rolls)
                {
                    pair.ReplayRoll(roller =>
                    {
                        rollCount--;
                    });
                }
                
                while (rollCount > 0)
                {
                    yield return null;
                } 
            }
            
            callback?.Invoke(rolls);
        }
        private void Awake()
        {
            dicePrefab.gameObject.SetActive(false);
        }

        private DiceRoller CreateDiceRoller(Vector3 dicePosition, Color color)
        {
            var pos = diceCamera.transform.position;
       
            var roller = Instantiate(dicePrefab, pos, Quaternion.identity).GetComponent<DiceRoller>();
            roller.gameObject.SetActive(true);

            roller.Configure(dicePosition, color);
            return roller;
        }
    }
}