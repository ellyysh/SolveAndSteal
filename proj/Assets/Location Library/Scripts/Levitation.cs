using UnityEngine;

public class Levitation : MonoBehaviour
{

    public float floatHeight = 0.5f;


    public float floatSpeed = 1f;


    public float rotationSpeed = 30f;


    public bool enableRotation = true;


    public float horizontalSway = 0.2f;


    public float swaySpeed = 0.8f;

    public bool enableSway = true;


    public bool mirrorTransformations = false;

    private Vector3 _startPosition;
    private float _timeOffset;

    void Start()
    {
        _startPosition = transform.position;
        // Случайное смещение времени для разных объектов
        _timeOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float time = Time.time + _timeOffset;
        float mirrorMultiplier = mirrorTransformations ? -1f : 1f;

        // Вертикальное покачивание (левитация)
        float verticalOffset = Mathf.Sin(time * floatSpeed) * floatHeight * mirrorMultiplier;
        
        // Горизонтальное покачивание
        float horizontalX = 0f;
        float horizontalZ = 0f;
        
        if (enableSway)
        {
            horizontalX = Mathf.Cos(time * swaySpeed) * horizontalSway * mirrorMultiplier;
            horizontalZ = Mathf.Sin(time * swaySpeed * 0.7f) * horizontalSway * mirrorMultiplier;
        }

        // Применяем перемещение
        transform.position = _startPosition + new Vector3(
            horizontalX,
            verticalOffset,
            horizontalZ
        );

        // Применяем вращение
        if (enableRotation)
        {
            transform.Rotate(0f, rotationSpeed * Time.deltaTime * mirrorMultiplier, 0f, Space.World);
        }
    }
}