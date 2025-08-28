using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Block Data", menuName = "Block Data")]
public class BlockData : ScriptableObject
{
    public Sprite sprite;
    public Color color;
    public int maxHealth;
    public bool isIndestructible;
    public float friction;
    public int minGoldDrop;
    public int maxGoldDrop;
}
