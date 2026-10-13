using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileTouchControls : MonoBehaviour
{
    const float JoystickBaseSize = 170f;
    const float JoystickHandleSize = 76f;
    const float JoystickMargin = 120f;

    FreeCamera targetCamera;

    public static MobileTouchControls Create(FreeCamera targetCamera, float joystickRadius)
    {
        if (targetCamera == null || FindAnyObjectByType<MobileTouchControls>() != null)
        {
            return null;
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Mobile Controls Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        EnsureEventSystemExists();

        GameObject controlsObject = new GameObject("Mobile Touch Controls");
        controlsObject.transform.SetParent(canvas.transform, false);

        MobileTouchControls controls = controlsObject.AddComponent<MobileTouchControls>();
        controls.targetCamera = targetCamera;
        controls.CreateJoystick(canvas.transform, joystickRadius);
        controls.CreateLookPad(canvas.transform);
        return controls;
    }

    void CreateJoystick(Transform canvasTransform, float joystickRadius)
    {
        GameObject baseObject = new GameObject("Mobile Joystick", typeof(RectTransform), typeof(Image), typeof(JoystickPointer));
        baseObject.transform.SetParent(canvasTransform, false);
        baseObject.transform.SetAsLastSibling();

        RectTransform baseRect = baseObject.GetComponent<RectTransform>();
        baseRect.anchorMin = Vector2.zero;
        baseRect.anchorMax = Vector2.zero;
        baseRect.pivot = new Vector2(0.5f, 0.5f);
        baseRect.anchoredPosition = new Vector2(JoystickMargin, JoystickMargin);
        baseRect.sizeDelta = new Vector2(JoystickBaseSize, JoystickBaseSize);

        Image baseImage = baseObject.GetComponent<Image>();
        baseImage.color = new Color(1f, 1f, 1f, 0.18f);
        baseImage.raycastTarget = true;

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(baseObject.transform, false);

        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(JoystickHandleSize, JoystickHandleSize);

        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.color = new Color(1f, 1f, 1f, 0.45f);
        handleImage.raycastTarget = false;

        JoystickPointer joystick = baseObject.GetComponent<JoystickPointer>();
        joystick.Initialize(targetCamera, baseRect, handleRect, joystickRadius);
    }

    void CreateLookPad(Transform canvasTransform)
    {
        GameObject lookObject = new GameObject("Mobile Look Area", typeof(RectTransform), typeof(Image), typeof(LookPointer));
        lookObject.transform.SetParent(canvasTransform, false);
        lookObject.transform.SetAsFirstSibling();

        RectTransform lookRect = lookObject.GetComponent<RectTransform>();
        lookRect.anchorMin = new Vector2(0.52f, 0f);
        lookRect.anchorMax = new Vector2(1f, 1f);
        lookRect.offsetMin = Vector2.zero;
        lookRect.offsetMax = Vector2.zero;

        Image lookImage = lookObject.GetComponent<Image>();
        lookImage.color = new Color(1f, 1f, 1f, 0.001f);
        lookImage.raycastTarget = true;

        lookObject.GetComponent<LookPointer>().Initialize(targetCamera);
    }

    static void EnsureEventSystemExists()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    class JoystickPointer : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        FreeCamera targetCamera;
        RectTransform baseRect;
        RectTransform handleRect;
        float radius;

        public void Initialize(FreeCamera targetCamera, RectTransform baseRect, RectTransform handleRect, float radius)
        {
            this.targetCamera = targetCamera;
            this.baseRect = baseRect;
            this.handleRect = handleRect;
            this.radius = Mathf.Max(20f, radius);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            UpdateJoystick(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateJoystick(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            handleRect.anchoredPosition = Vector2.zero;
            targetCamera.ClearMobileMoveInput();
        }

        void UpdateJoystick(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                return;
            }

            Vector2 clampedPoint = Vector2.ClampMagnitude(localPoint, radius);
            handleRect.anchoredPosition = clampedPoint;
            targetCamera.SetMobileMoveInput(clampedPoint / radius);
        }
    }

    class LookPointer : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        FreeCamera targetCamera;
        RectTransform lookRect;
        Vector2 lastPosition;
        int activePointerId;
        bool isTracking;

        public void Initialize(FreeCamera targetCamera)
        {
            this.targetCamera = targetCamera;
            lookRect = GetComponent<RectTransform>();
            activePointerId = int.MinValue;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (lookRect == null || !RectTransformUtility.RectangleContainsScreenPoint(lookRect, eventData.position, eventData.pressEventCamera))
            {
                isTracking = false;
                return;
            }

            activePointerId = eventData.pointerId;
            isTracking = true;
            lastPosition = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isTracking || eventData.pointerId != activePointerId)
            {
                return;
            }

            Vector2 delta = eventData.position - lastPosition;
            targetCamera.AddMobileLookDelta(delta);
            lastPosition = eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId)
            {
                return;
            }

            isTracking = false;
            activePointerId = int.MinValue;
            lastPosition = Vector2.zero;
        }
    }
}
