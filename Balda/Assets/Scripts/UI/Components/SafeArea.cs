using UnityEngine;

namespace Assets.Scripts
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        void Awake()
        {
            RectTransform rect = GetComponent<RectTransform>();
            Rect safe = Screen.safeArea;

            Vector2 anchorMin = safe.position;
            Vector2 anchorMax = safe.position + safe.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
        }
    }
}
