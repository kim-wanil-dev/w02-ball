using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameInputController inputController;
    [SerializeField] private PlayerCharacter playerCharacter;
    //[SerializeField] private Transform cameraTransform;
    private float previousSizeChangeInput;

    [Header("Gravity Control")]
    [SerializeField] private float gravityTiltAmount = 0.7f;
    [SerializeField] private List<BallStat> ownedBalls;
    [SerializeField] private int currentBallNum = 0;

    private void Update()
    {
        //Vector2 moveInput = inputController.MoveInput;


        playerCharacter.SetMoveInput(inputController.MoveInput);

        float currentSizeChangeInput = inputController.SizeChangeInput;
        //if()
        if (previousSizeChangeInput != currentSizeChangeInput && currentSizeChangeInput != 0)
        {
            int diff = (int)currentSizeChangeInput;
            int num;

            if (diff == 1)
            {
                num = (currentBallNum + diff) % ownedBalls.Count;
            }
            else if (diff == -1)
            {
                num = (currentBallNum + diff + ownedBalls.Count) % ownedBalls.Count;
            }

            else
            {
                Debug.LogError($"currentSizeChangeInput :{currentSizeChangeInput} error");
                return;
            }
            currentBallNum = num;

            playerCharacter.PlaySizeChange(ownedBalls[currentBallNum]);
            // 입력값이 변했을 때만 실행
            Debug.Log($"Input Changed: diff:{diff} , num:{num}");
            Debug.Log($"Input Changed: {previousSizeChangeInput} → {currentSizeChangeInput}");
        }
        previousSizeChangeInput = currentSizeChangeInput;

        if (inputController.JumpPressed)
        {
            playerCharacter.JumpRequested();
        }


    }



}