using UnityEngine;

public class DesktopMovement : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float lookSpeed = 2f;

    private float rotationX;
    private float rotationY;

    void Update()
    {
        // Вращение мышью (зажата правая кнопка)
        if (Input.GetMouseButton(1))
        {
            rotationX += Input.GetAxis("Mouse X") * lookSpeed;
            rotationY -= Input.GetAxis("Mouse Y") * lookSpeed;
            rotationY = Mathf.Clamp(rotationY, -80f, 80f);

            transform.localRotation = Quaternion.Euler(rotationY, rotationX, 0f);
        }

        // Движение по WASD
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        transform.position += move * moveSpeed * Time.deltaTime;
    }
}
