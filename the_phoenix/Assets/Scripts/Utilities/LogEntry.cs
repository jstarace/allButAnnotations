using UnityEngine;
using System;

[System.Serializable]

public class LogEntry
{

    /*
     * Ok... Here we go... This is the big big.  This will be the generic entry to cover
     * 1. Typing in the document
     * 2. Typing in the chat
     * 3. Moving the mouse
     * 4. Selecting characters
     * 5. Creating an annotation
     * 
     * I should also have a way to save them as individual files
    */

    #region Variables

    #region When it happened
    [SerializeField] public string date;
    [SerializeField] public string time;
    #endregion

    #region User Info
    [SerializeField] public ulong userId;
    [SerializeField] public string userName;
    [SerializeField] public string userType;
    #endregion

    #region User Position at beginning and end of action
    [SerializeField] public Vector3 startPos;
    [SerializeField] public Vector3 endPos;
    #endregion

    #region Action Details
    // Ok... Type... What's this... keyboard, mouse, ui
    [SerializeField] public string actionType;

    // This here... 
    /*
     * 1. Keyboard navigation (arrow keys)
     * 2. Mouse movement
     * 3. Selection (click hold)
     * 4. Keyboard interaction with document
     *      a. Insert:
     *          _character
     *          _newline
     *          _space
     *      b. Delete:
     *          _character
     *          _line
     *      c. Backspace:
     *          _character
     *          _line
     * 5. Interaction with UI:
     *      a. ButtonClick:
     *          _saveDoc
     *          _importDoc
     *          _clearDoc
     *          _saveLog
     *          _activateChat
     *          _quit
     * 
     */
    [SerializeField] public int actionCode;
    [SerializeField] public string actionName;
    [SerializeField] public string actionContent;
    [SerializeField] public string actionAdditionalContent;
    #endregion


    #endregion

    public LogEntry(
        string date,
        string time,
        ulong userId,
        string userName,
        string userType,
        Vector3 startPos,
        Vector3 endPos,
        string actionType,
        int actionCode,
        string actionName,
        string actionContent,
        string actionAdditionalContent
        )
    {
        this.date = date;
        this.time = time;
        this.userId = userId;
        this.userName = userName;
        this.userType = userType;
        this.startPos = startPos;
        this.endPos = endPos;
        this.actionType = actionType;
        this.actionCode = actionCode;
        this.actionName = actionName;
        this.actionContent = actionContent;
        this.actionAdditionalContent = actionAdditionalContent;
    }
}
