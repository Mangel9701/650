using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting;

namespace Studio650.ColorField
{
    [Preserve]
    public class BaseColorFieldEditorDragTarget : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public enum TargetKind
        {
            SaturationValue,
            Hue,
            Alpha
        }

        [SerializeField] private BaseColorFieldEditorUI editor;
        [SerializeField] private TargetKind targetKind;

        public void Initialize(BaseColorFieldEditorUI targetEditor, TargetKind kind)
        {
            editor = targetEditor;
            targetKind = kind;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            ApplyPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            ApplyPointer(eventData);
        }

        private void ApplyPointer(PointerEventData eventData)
        {
            if (editor == null || transform is not RectTransform rectTransform)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
                return;

            Rect rect = rectTransform.rect;
            float x = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
            float y = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);

            switch (targetKind)
            {
                case TargetKind.SaturationValue:
                    editor.SelectSaturationValue(new Vector2(x, y));
                    break;
                case TargetKind.Hue:
                    editor.SelectHue(y);
                    break;
                case TargetKind.Alpha:
                    editor.SelectAlpha(x);
                    break;
            }
        }
    }
}
