using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MemeFlip/Meme Library")]
public class MemeLibrary : ScriptableObject
{
    public List<MemeData> memes = new();
}
