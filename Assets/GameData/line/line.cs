using UnityEngine;


[CreateAssetMenu(menuName = "Line/NPC Line")]
public class Line : ScriptableObject
{
    public string SpeakerName;

    [TextArea(2, 5)]
    public string[] lines;
}
