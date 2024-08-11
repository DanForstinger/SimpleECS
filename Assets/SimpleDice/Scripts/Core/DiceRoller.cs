using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using InnerDriveStudios.DiceCreator;
using MonsterLove.StateMachine;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Mythiria
{
    [System.Serializable]
    public class DiceRoll
    {
        [System.Serializable]
        public class Frame
        {
            public Vector3 position => new Vector3(x, y, z);
            public Quaternion rotation => new Quaternion(rX, yX, zX, w);

            public Frame(Vector3 pos, Quaternion quat)
            {
                x = pos.x;
                y = pos.y;
                z = pos.z;
                rX = quat.x;
                yX = quat.y;
                zX = quat.z;
                w = quat.w;
            }

            public float x, y, z, rX, yX, zX, w;
        }

        public List<Frame> frames = new List<Frame>();
        public DiceType type;
        public int result = 0;

        public void AddFrame(Vector3 pos, Quaternion quat)
        {
            frames.Add(new Frame(pos, quat));
        }
    }

    public class DiceRoller : MonoBehaviour
    {
        public enum States
        {
            Rolled,
            Idle,
            AwaitingDestroy
        }

        public States state => stateMachine.State;

        public DiceRoll roll { get; private set; } = new DiceRoll();
        public DieSides dice { get; private set; }

        private Camera diceCamera;
        private StateMachine<States> stateMachine;
        private DiceType diceType;

        private int recordingIndex;
        private Action<DiceRoller> replayCompleteCallback;

       [SerializeField] private float minVelocity = 7;
       [SerializeField] private float maxVelocity = 9;
       [SerializeField] private float minAngularVelocity = 10;
       [SerializeField] private float maxAngularVelocity = 12;
       [SerializeField] private Camera diceCaptureCamera;
       [SerializeField] private RenderTexture diceCaptureTexture;
       
       public bool IsRolling => dice.rigidbody.angularVelocity.magnitude > 0 || dice.rigidbody.velocity.magnitude > 0;

       public Sprite GetTexture()
       {
           
           // snap a pic and send it back.
           diceCaptureCamera.targetTexture = diceCaptureTexture;
     
           
           var capturePos = dice.transform.position;
           capturePos.z -= 10;
           
           diceCaptureCamera.transform.position = capturePos;
           
           var prevRot = dice.transform.rotation;

           Debug.Log("Result: " + roll.result);

           dice.transform.rotation = Quaternion.Euler(0, 0, 0);

           var sideIndex = dice.GetSideIndex(roll.result);

           Debug.Log("Side index: " + sideIndex);
           dice.transform.rotation = dice.GetWorldRotationFor(sideIndex, Vector3.forward);

           dice.rigidbody.rotation = dice.transform.rotation;
           
           diceCaptureCamera.Render();

           Texture2D texture = new Texture2D(diceCaptureTexture.width, diceCaptureTexture.height);
           Rect rect=new Rect(0, 0, diceCaptureTexture.width, diceCaptureTexture.height);
           RenderTexture currentRenderTexture = RenderTexture.active;
           RenderTexture.active = diceCaptureTexture;
           texture.ReadPixels(rect, 0, 0);
           texture.Apply();
           RenderTexture.active = currentRenderTexture;
           Sprite sprite = Sprite.Create( texture, rect, Vector2.zero );
           
          // dice.transform.rotation = prevRot;
           return sprite;
       }
       
        // This method is used to start receiving input for a local player dice roll
        public void StartSimulate()
        {
            if (dice == null) Debug.LogError("Trying to roll dice, but you haven't yet set the dice to roll!");

            Debug.Log("Started dragging...");
            roll = new DiceRoll();
            roll.type = diceType;

            dice.meshRenderer.enabled = false;
            
            dice.rigidbody.constraints = RigidbodyConstraints.None;

            var xSign = UnityEngine.Random.Range(0, 100) < 50 ? -1 : 1;
            var ySign = UnityEngine.Random.Range(0, 100) < 50 ? -1 : 1;
            var velocity = new Vector2(UnityEngine.Random.Range(minVelocity, maxVelocity) * xSign, UnityEngine.Random.Range(minVelocity, maxVelocity) * ySign);
            dice.rigidbody.velocity = velocity;

            Vector3 randomSpawnRotation = new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
            dice.transform.rotation = Quaternion.Euler(randomSpawnRotation);

            dice.rigidbody.angularVelocity = randomSpawnRotation * UnityEngine.Random.Range(minAngularVelocity, maxAngularVelocity);
            dice.rigidbody.velocity = velocity;
            
            dice.rigidbody.isKinematic = false;
        }
        
        public void StepSimulate(float deltaTime)
        {
            roll.AddFrame(dice.rigidbody.position, dice.rigidbody.rotation);
        }

        // This method is used to replay a dice roll, such as when one is received over networking.
        public void ReplayRoll(Action<DiceRoller> completed)
        {
            if (dice == null) Debug.LogError("Trying to roll dice, but you haven't yet set the dice to roll!");
            
            Debug.Log("Replaying roll...");
            dice.meshRenderer.enabled = true;
            dice.rigidbody.isKinematic = true;
            recordingIndex = 0;
            replayCompleteCallback = completed;
            stateMachine.ChangeState(States.Rolled);
        }
        
        private void Awake()
        {
            stateMachine = StateMachine<States>.Initialize(this);
        }

        private void OnDestroy()
        {
            if (dice && dice.gameObject) Destroy(dice.gameObject);
        }

        public void Configure(DieSides dice, Vector3 dicePosition, DiceType type, Camera camera, Color col)
        {
            this.dice = dice;
            this.diceCamera = camera;
            this.dice.meshRenderer.material.color = col;
            this.diceType = type;
            this.dice.transform.position = dicePosition;
        }

        private void Rolled_Enter()
        {
            dice.rigidbody.isKinematic = true;
            recordingIndex = 0;
        }
        
        private void Rolled_FixedUpdate()
        {
            if (recordingIndex >= roll.frames.Count)
            {
                var result = dice.GetDieSideMatchInfo().closestMatch.values[0];
                Debug.Log(string.Format("Animated rolling! Result: {0}", result));
                stateMachine.ChangeState(States.AwaitingDestroy);
                return;
            }    
            
            var pair = roll.frames[recordingIndex];
            
            dice.rigidbody.position = pair.position;
            dice.rigidbody.rotation = pair.rotation;
            recordingIndex++;
        }

        private void AwaitingDestroy_Enter()
        {
            dice.rigidbody.velocity = Vector3.zero;
            dice.rigidbody.isKinematic = true;
            replayCompleteCallback?.Invoke(this);
        }
    }
}