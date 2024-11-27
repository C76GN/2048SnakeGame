using UnityEngine;

public class GameManager : MonoBehaviour
{
    public InputManager inputManager;
    public CubeManager cubeManager;
    public ScoreManager scoreManager;

    void Start()
    {
        // 订阅事件
        inputManager.OnDirectionChanged += HandleDirectionChange;
        inputManager.OnSpawnCubeRequested += HandleSpawnCube;
        cubeManager.OnCubeMerged += HandleCubeMerged;
        cubeManager.OnSnakeHeadChanged += HandleSnakeHeadChanged;

        // 初始生成立方体
        cubeManager.SpawnCube(2);

        // 开始生成食物
        InvokeRepeating("SpawnFood", 0f, cubeManager.foodSpawnInterval);

        CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (cameraFollow != null && cubeManager.Cubes.Count > 0)
        {
            cameraFollow.SetSnakeHead(cubeManager.Cubes[0].transform); // 初始化蛇头
        }
    }

    void FixedUpdate()
    {
        cubeManager.MoveSnake();
        cubeManager.CheckAndMergeCubes(); // 自动检查并处理食物碰撞
    }

    void HandleDirectionChange(Vector3 direction)
    {
        cubeManager.SetMoveDirection(direction);
    }

    void HandleSpawnCube()
    {
        cubeManager.SpawnCube(2);
        cubeManager.CheckAndMergeCubes();
    }

    void HandleCubeMerged(int points)
    {
        scoreManager.AddScore(points);
    }
    void HandleSnakeHeadChanged(Transform newHead)
    {
        CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.SetSnakeHead(newHead); // 使用方法更新摄像机的蛇头目标
        }
    }

    void SpawnFood()
    {
        cubeManager.SpawnFood();
    }
    void OnDestroy()
    {
        cubeManager.OnSnakeHeadChanged -= HandleSnakeHeadChanged;
    }
}