using Mono.Cecil.Cil;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    //private int cellSize = 3;
    private readonly float delayBetweenInputs = .2f;
    private float t = 0;
    private int cellSize;
    private bool arrow;
    private bool mouse;

    Vector2Int moveDir;


    private void Start()
    {
        cellSize = NetworkUI.cell;
        moveDir = new Vector2Int(0, 0);
    }

    private void Update()
    {
        if (!IsOwner) return;


        // Let's handle moving with just arrows first.  We want the user to be able to navigate the world, but only type in
        // the document for now.  In order to do this I capture move arrows first and process those.  So this will be 
        // Something like.... any key and keycode


        if (Input.anyKey)
        {
            var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            var player = playerObject.GetComponent<PlayerNetwork>();
            //Vector2Int moveDir = new Vector2Int(0, 0);

            // First we have to make sure that the player is in the document and nowhere else
            if (ChatController.Instance.chatInput.isFocused || DocumentManager.Instance.fileName.isFocused) return;

            // Then we check if they press an arrow key because we allow this key to be held down
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.RightArrow)) arrow = true;
            else arrow = false;

            // Then we check for mouse clicks... more on this later
            if(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) mouse = true;
            else mouse = false;

            // Handle moving with the arrow keys
            if (arrow)
            {
                moveDir = new Vector2Int(0, 0);
                t += Time.deltaTime;
                
                if (t > delayBetweenInputs || t < 0.000001f)
                {
                    if (Input.GetKey(KeyCode.RightArrow))
                    {
                        moveDir.x = +cellSize;
                        player.ProcessInput(moveDir, 1, "Right Arrow");
                    }
                    if (Input.GetKey(KeyCode.LeftArrow))
                    {
                        moveDir.x = -cellSize;
                        player.ProcessInput(moveDir, 2, "Left Arrow");
                    }
                    if (Input.GetKey(KeyCode.UpArrow))
                    {
                        moveDir.y = +cellSize;
                        player.ProcessInput(moveDir, 3, "Up Arrow");
                    }
                    if (Input.GetKey(KeyCode.DownArrow))
                    {
                        moveDir.y = -cellSize;
                        player.ProcessInput(moveDir, 4, "Down Arrow");
                    }
                    t = 0;
                }
            }

            // Here I'll do mouse


            // Otherwise, they typing... paste that mofo here.

            if (!arrow && !mouse && !NetworkUI.Instance.displayMessage)
            {
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
                    // TODO: Gotta fix this... probably just run this tabNo times
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

     

        /*

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
            int cellSize = NetworkUI.cell;
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
                // TODO: Gotta fix this... probably just run this tabNo times
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

        }*/
    }
}
