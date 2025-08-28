using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    [SerializeField]
    private Transform target;

    [SerializeField]
    private Bounds bounds;

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(
            Mathf.Clamp(target.position.x, bounds.min.x, bounds.max.x),
            Mathf.Clamp(target.position.y, bounds.min.y, bounds.max.y),
            transform.position.z
        );
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
