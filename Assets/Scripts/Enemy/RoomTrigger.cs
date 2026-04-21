using UnityEngine;
/// <summary>
/// this script is attached to a trigger collider in the room and is responsible for activating the room when the player enters it.
/// </summary>
public class RoomTrigger : MonoBehaviour
{
   [SerializeField] RoomController roomController;
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        roomController.ActivateRoom();
    }
}
