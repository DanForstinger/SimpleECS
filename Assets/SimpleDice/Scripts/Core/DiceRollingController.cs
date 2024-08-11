using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using InnerDriveStudios.DiceCreator;
using MonsterLove.StateMachine;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

namespace Mythiria
{
    public class DiceRollingController : MonoBehaviour
    {
        public Camera diceCamera;

        [SerializeField] private DiceRoller rollerPrefab;

        [SerializeField] private DieSides d4;
        [SerializeField] private DieSides d6;
        [SerializeField] private DieSides d8;
        [SerializeField] private DieSides d10;
        [SerializeField] private DieSides d12;
        [SerializeField] private DieSides d20;
    
        private List<DiceRoller> rolls;
        
        private Dictionary<DiceType, DieSides> diceTypeToPrefabMap;

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
        
        public void SimulateDiceRoll(DiceType diceType, Vector3 spawnPos, Color color, int count, bool autoReplay, System.Action<List<DiceRoller>> callback)
        {
            Debug.Log("Rolling dice...");
            StartCoroutine(ExecSimulateDiceRoll(count, spawnPos, color, diceType, autoReplay, callback));
        }

        private IEnumerator ExecSimulateDiceRoll(int count,  Vector3 spawnPos, Color color, DiceType diceType, bool autoReplay, System.Action<List<DiceRoller>> callback)
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
                
                var roller = CreateDiceRoller(diceType, spawnPos + new Vector3(offsetX, 0,0), color);
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
            

            yield return null;
            
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

                yield return null;
            }
            
            callback?.Invoke(rolls);
        }
        private void Awake()
        {
            rollerPrefab.gameObject.SetActive(false);
            diceTypeToPrefabMap = new Dictionary<DiceType, DieSides>()
            {
                { DiceType.d4, d4 },
                { DiceType.d6, d6 },
                { DiceType.d8, d8 },
                { DiceType.d10, d10 },
                { DiceType.d12, d12 },
                { DiceType.d20, d20 },
            };
            
        }

        private DiceRoller CreateDiceRoller(DiceType type,Vector3 dicePosition, Color color)
        {
            var prefab = diceTypeToPrefabMap[type];

            var pos = diceCamera.transform.position;
       
            var roller = Instantiate(rollerPrefab, pos, Quaternion.identity).GetComponent<DiceRoller>();
            roller.gameObject.SetActive(true);
            var dice = Instantiate(prefab.gameObject, pos, Quaternion.identity).GetComponent<DieSides>();
            
            roller.Configure(dice, dicePosition, type, diceCamera, color);
            return roller;
        }
    }
}