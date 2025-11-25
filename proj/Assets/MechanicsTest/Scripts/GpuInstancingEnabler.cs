using UnityEngine;

public class GpuInstancingEnabler : MonoBehaviour
{
    private void Awake()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        // Если MeshRenderer отсутствует — просто игнорируем
        if (meshRenderer == null)
            return;

        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        meshRenderer.SetPropertyBlock(materialPropertyBlock);
    }
}
