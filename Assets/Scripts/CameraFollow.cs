using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public CubeManager cubeManager; // CubeManager�����ڶ�̬��ȡ��ͷ
    public Vector3 offset = new Vector3(0, 10, -10); // ���������ͷ��ƫ��
    public float smoothSpeed = 5f; // ƽ��׷���ٶ�
    private Transform snakeHead;

    public void SetSnakeHead(Transform newHead)
    {
        if (newHead == null)
        {
            Debug.LogWarning("New snake head is null!");
            return;
        }

        snakeHead = newHead; // ������ͷ����
        Debug.Log($"Snake head updated to: {snakeHead.name}");
    }



    void FixedUpdate()
    {
        if (snakeHead == null) return;

        // ����Ŀ��λ��
        Vector3 targetPosition = snakeHead.position + offset;

        // ƽ���ƶ������
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.fixedDeltaTime);

        // ʼ��������ͷ
        transform.LookAt(snakeHead);
    }
}