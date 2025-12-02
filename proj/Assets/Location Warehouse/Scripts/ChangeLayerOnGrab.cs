using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ChangeLayerOnGrab : MonoBehaviour
{
    [Header("Ќовый слой, когда VR-игрок взаимодействует")]
    public string activeLayerName = "DistractObject";

    private void Awake()
    {
        var grab = GetComponent<XRGrabInteractable>();
        if (grab != null)
        {
            grab.selectExited.AddListener(OnRelease);
        }
    }
    private void OnRelease(SelectExitEventArgs args)
    {
        gameObject.layer = LayerMask.NameToLayer(activeLayerName);
    }
}
