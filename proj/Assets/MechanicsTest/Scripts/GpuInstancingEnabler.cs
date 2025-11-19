using UnityEngine;

public class GpuInstancingEnabler : MonoBehaviour
{
    // Update is called once per frame
    private void Awake()
    {
        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.SetPropertyBlock(materialPropertyBlock);
    }
}
