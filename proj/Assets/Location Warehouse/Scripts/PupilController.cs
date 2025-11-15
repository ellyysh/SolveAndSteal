using UnityEngine;

public class PupilController : MonoBehaviour
{
    public SkinnedMeshRenderer eyeMesh;

    [Header("Blend Shape Names")]
    public string narrowKeyName = "Key 1"; // сужение
    public string wideKeyName = "Key 2"; // расширение

    [Header("Settings")]
    public float changeSpeed = 60f;

    private int narrowIndex;
    private int wideIndex;

    private float narrowValue = 0f;
    private float wideValue = 0f;

    public enum PupilState { Normal, Narrow, Wide }
    public PupilState state = PupilState.Normal;

    void Start()
    {
        if (eyeMesh == null)
        {
            Debug.LogError("Eye Mesh is not assigned!");
            return;
        }

        // ѕолучаем индексы по именам
        narrowIndex = eyeMesh.sharedMesh.GetBlendShapeIndex(narrowKeyName);
        wideIndex = eyeMesh.sharedMesh.GetBlendShapeIndex(wideKeyName);

        if (narrowIndex < 0) Debug.LogError($"BlendShape '{narrowKeyName}' not found!");
        if (wideIndex < 0) Debug.LogError($"BlendShape '{wideKeyName}' not found!");
    }

    void Update()
    {
        switch (state)
        {
            case PupilState.Normal:
                narrowValue = Mathf.MoveTowards(narrowValue, 0, changeSpeed * Time.deltaTime);
                wideValue = Mathf.MoveTowards(wideValue, 0, changeSpeed * Time.deltaTime);
                break;

            case PupilState.Narrow:
                narrowValue = Mathf.MoveTowards(narrowValue, 100, changeSpeed * Time.deltaTime);
                wideValue = Mathf.MoveTowards(wideValue, 0, changeSpeed * Time.deltaTime);
                break;

            case PupilState.Wide:
                wideValue = Mathf.MoveTowards(wideValue, 100, changeSpeed * Time.deltaTime);
                narrowValue = Mathf.MoveTowards(narrowValue, 0, changeSpeed * Time.deltaTime);
                break;
        }

        Apply();
    }

    void Apply()
    {
        if (narrowIndex >= 0)
            eyeMesh.SetBlendShapeWeight(narrowIndex, narrowValue);
        if (wideIndex >= 0)
            eyeMesh.SetBlendShapeWeight(wideIndex, wideValue);
    }

    // API чтобы мен€ть состо€ние извне
    public void SetNarrow() => state = PupilState.Narrow;
    public void SetWide() => state = PupilState.Wide;
    public void SetNormal() => state = PupilState.Normal;
}
