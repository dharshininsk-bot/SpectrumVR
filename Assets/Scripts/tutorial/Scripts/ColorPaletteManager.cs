using System.Collections.Generic;
using UnityEngine;

public class ColorPaletteManager : MonoBehaviour
{
    public List<Renderer> paletteSlots; 
    public Color emptySlotColor = Color.gray; 

    private Renderer firstSelected = null;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Renderer hitRenderer = hit.collider.GetComponent<Renderer>();
                if (hitRenderer != null && paletteSlots.Contains(hitRenderer))
                {
                    OnSlotClicked(hitRenderer);
                }
            }
        }
    }

    void OnSlotClicked(Renderer clickedSlot)
    {
        if (clickedSlot.material.color == emptySlotColor) return;

        if (firstSelected == null)
        {
            firstSelected = clickedSlot;
        }
        else
        {
            if (firstSelected == clickedSlot) { firstSelected = null; return; }

            // Get colors
            Color c1 = firstSelected.material.color;
            Color c2 = clickedSlot.material.color;

            // Simple CMYK Subtractive Mix
            float c = (1f - c1.r) + (1f - c2.r);
            float m = (1f - c1.g) + (1f - c2.g);
            float y = (1f - c1.b) + (1f - c2.b);
            Color mixedColor = new Color(Mathf.Clamp01(1f - c/2f), Mathf.Clamp01(1f - m/2f), Mathf.Clamp01(1f - y/2f));

            // Find next empty space and paint it
            foreach (Renderer slot in paletteSlots)
            {
                if (slot.material.color == emptySlotColor)
                {
                    slot.material.color = mixedColor;
                    break;
                }
            }
            firstSelected = null;
        }
    }
}