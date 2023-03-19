using Mono.Cecil.Cil;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public static class ServerLoadFile
{
    //private static List<DocumentEntry> _entries;
    private static List<LogEntry> _entries;

    public static void Load(string path)
    {
        // Make sure the list is empty
        // _entries = new List<DocumentEntry>();
        _entries = new List<LogEntry>();


        // Read in the JSON File
        // _entries = FileHandler.ReadFromJSON<DocumentEntry>(path);
        _entries = FileHandler.ReadFromJSON<LogEntry>(path);


        foreach (LogEntry entry in _entries)
        {
            int code = entry.actionNumber;
            int x, y;

            switch (code)
            {
                case 5:
                    DocumentManager.Instance.InsertRow(entry.startPos);
                    DocumentManager.Instance.UpdateDocumentListClientRpc(entry.startPos, code);
                    break;
                case 6:
                    DocumentManager.Instance.InsertRow(entry.startPos);
                    DocumentManager.Instance.UpdateDocumentListClientRpc(entry.startPos, code);
                    break;
                case 7:
                    DocumentManager.Instance.Delete(entry.startPos, true, out y, out x);
                    DocumentManager.Instance.UpdateDocumentListClientRpc(entry.startPos, code);
                    break;
                case 8:
                    DocumentManager.Instance.Delete(entry.startPos, false, out x, out y);
                    DocumentManager.Instance.UpdateDocumentListClientRpc(entry.startPos, code);
                    break;
                case 9:
                    DocumentManager.Instance.InsertCharacter(entry.startPos, entry.actionContent);
                    DocumentManager.Instance.UpdateDocumentListClientRpc(entry.startPos, code, entry.actionContent);
                    break;
                case 10:
                    DocumentManager.Instance.InsertCharacter(entry.startPos, entry.actionContent);
                    DocumentManager.Instance.UpdateDocumentListClientRpc(entry.startPos, code, entry.actionContent);
                    break;
                case 42:
                    DocumentManager.Instance.InsertCharacter(entry.startPos, entry.actionContent);
                    DocumentManager.Instance.UpdateDocumentListClientRpc(entry.startPos, code, entry.actionContent);
                    break;  
                default:
                    break;
            }


        }

        /*
        foreach (DocumentEntry entry in _entries)
        {
            int code = entry.inputCode;
            int x, y;
            switch (code)
            {
                case 5:
                    DocumentManager.Instance.InsertRow(entry.currentPos);
                    DocumentManager.Instance.UpdateDocumentListClientRpc(entry.currentPos, code);
                    break;
                case 6:
                    DocumentManager.Instance.InsertRow(entry.currentPos);
                    DocumentManager.Instance.UpdateDocumentListClientRpc(entry.currentPos, code);
                    break;
                case 7:
                    DocumentManager.Instance.Delete(entry.currentPos, true, out y, out x);
                    DocumentManager.Instance.UpdateDocumentListClientRpc(entry.currentPos, code);
                    break;
                case 8:
                    DocumentManager.Instance.Delete(entry.currentPos, false, out x, out y);
                    DocumentManager.Instance.UpdateDocumentListClientRpc(entry.currentPos, code);
                    break;
                case 9:
                    DocumentManager.Instance.InsertCharacter(entry.currentPos, entry.inputKey);
                    DocumentManager.Instance.UpdateDocumentListClientRpc(entry.currentPos, code, entry.inputKey);
                    break;
                case 10:
                    DocumentManager.Instance.InsertCharacter(entry.currentPos, entry.inputKey);
                    DocumentManager.Instance.UpdateDocumentListClientRpc(entry.currentPos, code, entry.inputKey);
                    break;
                case 42:
                    DocumentManager.Instance.InsertCharacter(entry.currentPos, entry.inputKey);
                    DocumentManager.Instance.UpdateDocumentListClientRpc(entry.currentPos, code, entry.inputKey);
                    break;
                default:
                    break;
            }
        }*/
    }
}
