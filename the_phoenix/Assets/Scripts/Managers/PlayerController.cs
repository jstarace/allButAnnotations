using Mono.Cecil.Cil;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    private int cellSize = 3;
    private void Update()
    {
        if (!IsOwner) return;
        if (Input.anyKeyDown && 
            !(
            Input.GetMouseButtonDown(0) || 
            Input.GetMouseButtonDown(1) || 
            Input.GetMouseButtonDown(2)
            ) 
            && !ChatController.Instance.chatInput.isFocused 
            && !DocumentManager.Instance.fileName.isFocused
           )
        {
            var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            var player = playerObject.GetComponent<PlayerNetwork>();
            Vector2Int moveDir = new Vector2Int(0, 0);

            #region 0 - Inputs that do nothing to the document
            if (Input.GetKeyDown(KeyCode.LeftShift) || 
                Input.GetKeyDown(KeyCode.RightShift) || 
                Input.GetKeyDown(KeyCode.CapsLock) || 
                Input.GetKeyDown(KeyCode.LeftControl) ||
                Input.GetKeyDown(KeyCode.RightControl) ||
                Input.GetKeyDown(KeyCode.LeftAlt) || 
                Input.GetKeyDown(KeyCode.RightAlt))
            {
                player.ProcessInput(moveDir, 0);
                return;
            }
            #endregion

            #region 1 - 4 -> Arrows for navigation
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    moveDir.x = +cellSize;
                    player.ProcessInput(moveDir, 1, "Right Arrow");
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    moveDir.x = -cellSize;
                    player.ProcessInput(moveDir, 2, "Left Arrow");
                }
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    moveDir.y = +cellSize;
                    player.ProcessInput(moveDir, 3, "Up Arrow");
                }
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    moveDir.y = -cellSize;
                    player.ProcessInput(moveDir, 4, "Down Arrow");
                }
                
                return;
            }
            #endregion

            #region 5 - Return -> 6 - NumPad Enter
            if (Input.GetKeyDown(KeyCode.Return))
            {
                moveDir.y = -cellSize;
                player.ProcessInput(moveDir, 5, "\n");
                return;
            }

            if (Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                moveDir.y = -cellSize;
                player.ProcessInput(moveDir, 6, "\n");
                return;
            }
            #endregion

            #region 7 - Delete
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                player.ProcessInput(moveDir, 7);
                return;
            }
            #endregion

            #region 8 - Backspace
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                moveDir.x = -cellSize;
                player.ProcessInput(moveDir, 8);
                return;
            }
            #endregion

            #region 9 - Space -> 10 - Tab
            if (Input.GetKeyDown(KeyCode.Space))
            {
                moveDir.x = +cellSize;
                player.ProcessInput(moveDir, 9, " ");
                return;
            }
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                moveDir.x = cellSize * 3;
                player.ProcessInput(moveDir, 10, " ");
            }
            #endregion

            #region 42 - Adding Characters to the document

            if (!string.IsNullOrEmpty(Input.inputString.ToString()) && !string.IsNullOrWhiteSpace(Input.inputString.ToString()) && !(Input.GetKeyDown(KeyCode.Backspace)))
            {
                int i = Input.inputString.ToString().Length;
                int j = 0;
                while (i > 0)
                {
                    var singleChar = Input.inputString[j].ToString();
                    //player.AddText(singleChar);
                    player.ProcessInput(new Vector2Int(3, 0), 42, singleChar);
                    j++;
                    i--;
                }
                return;
            }

            #endregion

        }
    }
}
