using System.Collections;
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
    private bool canChange = true;

    private void Update()
    {

        playerCharacter.SetMoveInput(inputController.MoveInput);

        float currentSizeChangeInput = inputController.SizeChangeInput;

        if (previousSizeChangeInput != currentSizeChangeInput && currentSizeChangeInput != 0)
        {
            if (!canChange) return;
            //playerCharacter.canChange
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
        }
        previousSizeChangeInput = currentSizeChangeInput;

        if (inputController.JumpPressed && playerCharacter.IsGrounded)
        {
            playerCharacter.ProcessJump();
        }


    }

    public IEnumerator ChangeBallStat()
    {
        canChange = false;
        playerCharacter.SetTargetVelocity();

        Time.timeScale = 0.3f;
        yield return new WaitForSecondsRealtime(1f);
        Time.timeScale = 1.0f;


        canChange = true;
    }



}