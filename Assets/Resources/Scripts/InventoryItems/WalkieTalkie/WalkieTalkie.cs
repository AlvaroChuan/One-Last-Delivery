using UnityEngine;
using Mirror;

public class WalkieTalkie : InventoryItem
{
    /*void OnEnable()
    {
        if (isOwned)
        {
            if (WalkieTalkieVoiceChannel.ClientSession != null)
            {
                WalkieTalkieVoiceChannel.ClientSession.OutputsEnabled = true;
            }
        }
    }

    void OnDisable()
    {
        if (isOwned)
        {
            if (WalkieTalkieVoiceChannel.ClientSession != null)
            {
                WalkieTalkieVoiceChannel.ClientSession.OutputsEnabled = false;
                WalkieTalkieVoiceChannel.ClientSession.InputEnabled = false;
            }
        }
    }

    public override void StartUse(GameObject user)
    {
        if (isOwned)
        {
            if (WalkieTalkieVoiceChannel.ClientSession != null)
            {
                WalkieTalkieVoiceChannel.ClientSession.InputEnabled = true;
            }
        }
    }

    public override void EndUse(GameObject user)
    {
        if (isOwned)
        {
            if (WalkieTalkieVoiceChannel.ClientSession != null)
            {
                WalkieTalkieVoiceChannel.ClientSession.InputEnabled = false;
            }
        }
    }*/
    public override void StartUse(GameObject user)
    {
        throw new System.NotImplementedException();
    }
}
