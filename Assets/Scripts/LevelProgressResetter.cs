using UnityEngine;

public sealed class LevelProgressResetter : MonoBehaviour
{
    [ContextMenu("Reset Level Progress")]
    public void ResetLevelProgress()
    {
        LevelProgression.ResetProgress();
        Debug.Log("Level progress has been reset.");
    }
}
