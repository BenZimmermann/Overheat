using UnityEngine;

[CreateAssetMenu(menuName = "Containers/Collect Data")]
public class CollectableData : ScriptableObject
{
    public float attractRadius = 3f;
    public float attractSpeed = 8f;
    public float pickupRadius = 0.4f;
    public float pickupDelay = 0.5f;

    public LineRenderer floatTrail;
    public float amount = 1f;
}
