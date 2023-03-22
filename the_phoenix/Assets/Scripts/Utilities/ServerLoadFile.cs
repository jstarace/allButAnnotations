using System.Collections.Generic;

public static class ServerLoadFile
{
    private static List<LogEntry> _entries;

    public static void Load(string path)
    {
        // Make sure the list is empty
        _entries = new List<LogEntry>();

        // Read in the JSON File
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
    }
}
