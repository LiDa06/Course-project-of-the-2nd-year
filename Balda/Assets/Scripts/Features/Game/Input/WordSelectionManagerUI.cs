using System.Collections.Generic;
using Balda.Features.Game.Flow;
using Balda.Features.Game.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Balda.Features.Game.Input
{
    public class WordSelectionManagerUI : MonoBehaviour
    {
        [SerializeField] private GameController gameController;
        [SerializeField] private GraphicRaycaster graphicRaycaster;

        private bool isDragging;
        private BoardCellView lastCellUnderPointer;

        private void Update()
        {
            if (gameController == null || !gameController.HasActiveDraft)
            {
                ResetDraggingState();
                return;
            }

            HandleTouchscreen();
            HandleMouse();
        }

        private void HandleTouchscreen()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null)
                return;

            var touch = touchscreen.primaryTouch;

            if (touch.press.wasPressedThisFrame)
                StartSelection(touch.position.ReadValue());
            else if (touch.press.isPressed)
                ContinueSelection(touch.position.ReadValue());
            else if (touch.press.wasReleasedThisFrame)
                EndSelection();
        }

        private void HandleMouse()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return;

            var mouse = Mouse.current;
            if (mouse == null)
                return;

            if (mouse.leftButton.wasPressedThisFrame)
                StartSelection(mouse.position.ReadValue());
            else if (mouse.leftButton.isPressed)
                ContinueSelection(mouse.position.ReadValue());
            else if (mouse.leftButton.wasReleasedThisFrame)
                EndSelection();
        }

        private void StartSelection(Vector2 screenPos)
        {
            BoardCellView cell = GetCellAtScreenPos(screenPos);
            if (cell == null)
                return;

            isDragging = true;
            lastCellUnderPointer = cell;
            gameController.BeginSelectionAt(cell.Row, cell.Col);
        }

        private void ContinueSelection(Vector2 screenPos)
        {
            if (!isDragging)
                return;

            BoardCellView cell = GetCellAtScreenPos(screenPos);
            if (cell == null)
                return;

            if (lastCellUnderPointer != null &&
                lastCellUnderPointer.Row == cell.Row &&
                lastCellUnderPointer.Col == cell.Col)
                return;

            lastCellUnderPointer = cell;
            gameController.ContinueSelectionAt(cell.Row, cell.Col);
        }

        private void EndSelection()
        {
            if (!isDragging)
                return;

            isDragging = false;
            lastCellUnderPointer = null;
            gameController.EndSelection();
        }

        private void ResetDraggingState()
        {
            isDragging = false;
            lastCellUnderPointer = null;
        }

        private BoardCellView GetCellAtScreenPos(Vector2 screenPos)
        {
            if (graphicRaycaster == null || EventSystem.current == null)
                return null;

            var eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };

            var results = new List<RaycastResult>();
            graphicRaycaster.Raycast(eventData, results);

            for (int i = 0; i < results.Count; i++)
            {
                BoardCellView cell = results[i].gameObject.GetComponentInParent<BoardCellView>();
                if (cell != null)
                    return cell;
            }

            return null;
        }
    }
}
