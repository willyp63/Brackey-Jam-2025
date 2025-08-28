using UnityEngine;
using UnityEngine.Rendering;

public class DynamicSortingOrder : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private SortingGroup sortingGroup;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        sortingGroup = GetComponent<SortingGroup>();
    }

    void Update()
    {
        // Update the sorting order based on the Y position
        int sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrder;
        }
        else if (sortingGroup != null)
        {
            sortingGroup.sortingOrder = sortingOrder;
        }
    }
}
