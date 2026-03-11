using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Containers/Container Data")]
public class ContainerData : ScriptableObject
{
    public string containerName;
    public string containerID;
    public LayerMask floorMask;

    public float health = 1f;

    public float rewardChance = 0.5f;
    public float rewardMoney = 10f;
    public float rewardhealth = 0.5f;

    public float ItemDropChance = 0.3f;
    public List<GameObject> ItemsToDrop;

    public float fallDamageMultiplier = 1f;
    public float fallDamageThreshold = 5f;

    public GameObject MoneyObj;
    public GameObject HealObj;

    public ParticleSystem hitParticles;
    public ParticleSystem destroyParticles;
}
