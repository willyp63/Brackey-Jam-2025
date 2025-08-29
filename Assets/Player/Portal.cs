using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null && player.Gold > 0)
        {
            GameManager.Instance.EndGame(false);
        }
    }
}
