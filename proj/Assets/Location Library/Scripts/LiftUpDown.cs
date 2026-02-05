using UnityEngine;
using System.Collections;

public class LiftUpDown : MonoBehaviour
{
    [Header("Настройки движения")]
    [Tooltip("На сколько единиц поднимать объект вверх от начальной позиции")]
    public float height = 2f;

    [Tooltip("Скорость движения вверх-вниз")]
    public float speed = 2f;

    [Tooltip("Задержка в секундах в верхней и нижней точке")]
    public float pauseTime = 1f;

    private Vector3 _startPos;
    private Vector3 _topPos;
    private bool _isMoving = false;

    void Start()
    {
        _startPos = transform.position;
        _topPos = _startPos + Vector3.up * height;

        StartCoroutine(MoveLoop());
    }

    private IEnumerator MoveLoop()
    {
        _isMoving = true;

        while (true)
        {
            // Вниз -> Вверх
            yield return StartCoroutine(MoveObject(_startPos, _topPos));

            // Пауза вверху
            yield return new WaitForSeconds(pauseTime);

            // Вверх -> Вниз
            yield return StartCoroutine(MoveObject(_topPos, _startPos));

            // Пауза внизу
            yield return new WaitForSeconds(pauseTime);
        }
    }

    private IEnumerator MoveObject(Vector3 from, Vector3 to)
    {
        float distance = Vector3.Distance(from, to);
        float time = distance / speed;     // время движения = расстояние / скорость
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / time;
            transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        transform.position = to;
    }
}