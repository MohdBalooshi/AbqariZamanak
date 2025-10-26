using UnityEngine;
using UnityEngine.EventSystems;

public class SwipeRelay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private CategoryCarouselController controller;
    [SerializeField] private float minSwipeDistance = 80f; // px

    private Vector2 startPos;
    private bool swiped;

    private void Awake()
    {
        if (!controller)
            controller = FindAnyObjectByType<CategoryCarouselController>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPos = eventData.position;
        swiped = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (swiped) return;

        float dx = eventData.position.x - startPos.x;
        if (Mathf.Abs(dx) >= minSwipeDistance)
        {
            // decide direction once, then mark as swiped
            if (dx < 0) controller?.SendMessage("NextCategory", SendMessageOptions.DontRequireReceiver);
            else        controller?.SendMessage("PrevCategory", SendMessageOptions.DontRequireReceiver);

            swiped = true;

            // Cancel any pending click on this object so the Button doesn't fire
            eventData.pointerPress = null;
            eventData.rawPointerPress = null;
            eventData.Use();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // If a swipe was recognized, ensure the click won't trigger after release
        if (swiped)
        {
            eventData.pointerPress = null;
            eventData.rawPointerPress = null;
            eventData.Use();
        }
    }
}
