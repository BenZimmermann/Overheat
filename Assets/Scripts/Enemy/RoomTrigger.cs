using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    RoomController roomController;
    private void Awake()
    {
        roomController = GetComponentInParent<RoomController>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        roomController.ActivateRoom();
    }
}
