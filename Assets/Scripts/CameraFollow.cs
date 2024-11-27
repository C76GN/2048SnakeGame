using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public CubeManager cubeManager; // CubeManager，用于动态获取蛇头
    public Vector3 offset = new Vector3(0, 10, -10); // 摄像机与蛇头的偏移
    public float smoothSpeed = 5f; // 平滑追踪速度
    private Transform snakeHead;

    public void SetSnakeHead(Transform newHead)
    {
        if (newHead == null)
        {
            Debug.LogWarning("New snake head is null!");
            return;
        }

        snakeHead = newHead; // 更新蛇头引用
        Debug.Log($"Snake head updated to: {snakeHead.name}");
    }



    void FixedUpdate()
    {
        if (snakeHead == null) return;

        // 计算目标位置
        Vector3 targetPosition = snakeHead.position + offset;

        // 平滑移动摄像机
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.fixedDeltaTime);

        // 始终面向蛇头
        transform.LookAt(snakeHead);
    }
}