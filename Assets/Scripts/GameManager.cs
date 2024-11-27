using UnityEngine;

public class GameManager : MonoBehaviour
{
    public InputManager inputManager;
    public CubeManager cubeManager;
    public ScoreManager scoreManager;

    void Start()
    {
        // �����¼�
        inputManager.OnDirectionChanged += HandleDirectionChange;
        inputManager.OnSpawnCubeRequested += HandleSpawnCube;
        cubeManager.OnCubeMerged += HandleCubeMerged;
        cubeManager.OnSnakeHeadChanged += HandleSnakeHeadChanged;

        // ��ʼ����������
        cubeManager.SpawnCube(2);

        // ��ʼ����ʳ��
        InvokeRepeating("SpawnFood", 0f, cubeManager.foodSpawnInterval);

        CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (cameraFollow != null && cubeManager.Cubes.Count > 0)
        {
            cameraFollow.SetSnakeHead(cubeManager.Cubes[0].transform); // ��ʼ����ͷ
        }
    }

    void FixedUpdate()
    {
        cubeManager.MoveSnake();
        cubeManager.CheckAndMergeCubes(); // �Զ���鲢����ʳ����ײ
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
            cameraFollow.SetSnakeHead(newHead); // ʹ�÷����������������ͷĿ��
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