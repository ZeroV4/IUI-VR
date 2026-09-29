using UnityEngine;

// DO NOT CHANGE THIS FILE NOR ITS SETTINGS
public enum FileType { None, Green, LightGreen }

public class FileItem : MonoBehaviour
{   
    // we use this to set the right type for the drawer, this needs to be set per file
    public FileType fileType = FileType.None; 
}
