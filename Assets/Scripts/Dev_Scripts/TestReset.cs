using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

//this will be not used in the final game only in test scene to test the reset of objects after they get destroyed
public class TestReset : MonoBehaviour, IDamageable
{
    [SerializeField] private List<GameObject> resetPonts;
    [SerializeField] private GameObject ObjToReset;

    public void TakeDamage(float amount, string Source)
    {
        Reset();
    }
    private void Reset()
    {
        foreach (var point in resetPonts)
        {
            Instantiate(ObjToReset, point.transform.position, Quaternion.identity);
        }
    }
}
