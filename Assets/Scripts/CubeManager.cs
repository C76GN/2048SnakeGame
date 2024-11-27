using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeManager : MonoBehaviour
{
    public GameObject cubePrefab;
    public Transform spawnPoint;
    public float moveSpeed = 5f;
    public float stepLength = 1f;
    public float fixedGap = 0.2f;

    private List<GameObject> cubes = new List<GameObject>();
    public IReadOnlyList<GameObject> Cubes => cubes; // 只读访问
    private List<GameObject> foodCubes = new List<GameObject>();
    private Vector3 moveDirection = Vector3.forward;

    public float foodSpawnInterval = 5f;
    private float foodSpawnTimer = 0f;

    public event System.Action<int> OnCubeMerged;
    private bool isMerging = false; // 合并标志位

    private List<Vector3> pathPoints = new List<Vector3>();
    public int pathResolution = 10; // 路径点间距的分辨率
    private List<float> cachedDistances = new List<float>();

    public int maxFoodCount = 20; // 最大食物数量限制

    public event System.Action<Transform> OnSnakeHeadChanged;

    public void SpawnCube(int number)
    {
        // 实例化新的方块
        GameObject newCube = Instantiate(cubePrefab);

        if (cubes.Count == 0)
        {
            // 如果是第一个方块，放置在初始生成点
            newCube.transform.position = spawnPoint.position;
        }
        else
        {
            // 使用统一的动态间隙计算逻辑
            GameObject tail = cubes[cubes.Count - 1];
            float dynamicGap = CalculateDynamicGap(tail, newCube); // 调用统一的间隙计算函数

            // 计算方向（由路径点决定）
            Vector3 direction = (pathPoints.Count > 1)
                ? (pathPoints[pathPoints.Count - 1] - pathPoints[pathPoints.Count - 2]).normalized
                : Vector3.back;

            // 按动态间隙设置位置
            newCube.transform.position = tail.transform.position + direction * dynamicGap;
        }

        // 修正位置（确保方块在地面上）
        float groundY = 0.5f; // 地面高度
        float halfHeight = newCube.transform.localScale.y / 2.0f;
        newCube.transform.position = new Vector3(
            newCube.transform.position.x,
            groundY + halfHeight,
            newCube.transform.position.z
        );

        // 初始化新方块的脚本并设置数值
        DynamicCubeScript cubeScript = newCube.GetComponent<DynamicCubeScript>();
        if (cubeScript != null)
        {
            cubeScript.SetNumber(number);
            cubeScript.PlaySpawnAnimation();
        }

        // 将新方块添加到蛇的方块列表
        cubes.Add(newCube);

        // 更新缓存的距离
        UpdateCachedDistances();

        // 检查并处理合并逻辑
        CheckAndMergeCubes();
    }


    private float CalculateDynamicGap(GameObject current, GameObject next)
    {
        float currentSize = current.transform.localScale.x;
        float nextSize = next.transform.localScale.x;

        // 稳定的间隙计算，避免浮点误差
        return fixedGap + Mathf.Round((currentSize + nextSize) * 0.5f * 1000f) / 1000f;
    }

    public void SetMoveDirection(Vector3 direction)
    {
        moveDirection = direction;
    }

    public void MoveSnake()
    {
        // 提前返回的检查
        if (cubes == null || cubes.Count == 0) return;

        try
        {
            // 缓存变量，减少重复计算
            GameObject head = cubes[0];
            float deltaTime = Time.fixedDeltaTime;

            // 计算蛇头的新位置
            Vector3 newHeadPosition = head.transform.position + moveDirection * (moveSpeed * deltaTime * 0.5f);
            newHeadPosition.y = 0.5f;

            // 将新路径点插入到列表的开头
            pathPoints.Insert(0, newHeadPosition);

            // 控制路径点的数量，移除超出限制的最后一个点
            int dynamicResolution = Mathf.CeilToInt(moveSpeed * 3f * cubes.Count); // 根据移动速度动态调整分辨率
            int maxPoints = Mathf.Max(dynamicResolution * (cubes.Count + 1), 50); // 保证最少 10 个点
            if (pathPoints.Count > maxPoints)
            {
                pathPoints.RemoveAt(pathPoints.Count - 1); // 移除多余的点
            }
            // 移动蛇头
            head.GetComponent<DynamicCubeScript>().SmoothMove(newHeadPosition, deltaTime, () =>
            {
                OnSnakeHeadChanged?.Invoke(cubes[0].transform); // 确保更新摄像机目标
            });


            // 身体部分跟随头部路径移动
            for (int i = 1; i < cubes.Count; i++)
            {
                float targetDistance = CalculateTargetDistance(i);
                Vector3 targetPosition = GetPositionAlongPath(targetDistance);

                if (Vector3.Distance(cubes[i].transform.position, targetPosition) > fixedGap * 1.2f) // 适当放宽阈值
                {
                    cubes[i].GetComponent<DynamicCubeScript>().SmoothMove(targetPosition, deltaTime);
                }

            }

            // 旋转优化
            if (moveDirection != Vector3.zero)
            {
                head.transform.rotation = Quaternion.LookRotation(moveDirection);
            }

            CheckFoodCollision();
        }
        catch (Exception e)
        {
            Debug.LogError($"Snake movement error: {e.Message}");
        }
    }

    private float CalculateTargetDistance(int cubeIndex)
    {
        float targetDistance = 0;
        for (int j = 0; j < cubeIndex; j++)
        {
            targetDistance += CalculateDynamicGap(cubes[j], cubes[j + 1]); // 使用动态间隙
        }
        return targetDistance;
    }



    private void UpdateCachedDistances()
    {
        cachedDistances.Clear();
        float distance = 0;
        for (int i = 0; i < cubes.Count - 1; i++)
        {
            cachedDistances.Add(distance);
            distance += CalculateDynamicGap(cubes[i], cubes[i + 1]); // 使用动态间隙
        }
        // 添加最后一个方块的距离
        cachedDistances.Add(distance);
    }


    private Vector3 GetPositionAlongPath(float distance)
    {
        if (pathPoints.Count == 0)
        {
            Debug.LogWarning("Path points list is empty. Returning default position.");
            return Vector3.zero;
        }

        float coveredDistance = 0f;

        for (int i = 1; i < pathPoints.Count; i++)
        {
            float segmentDistance = Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
            coveredDistance += segmentDistance;

            if (coveredDistance >= distance)
            {
                float overshoot = coveredDistance - distance;
                float t = 1f - (overshoot / segmentDistance);

                // 改为三次插值 (Catmull-Rom)
                Vector3 p0 = i > 1 ? pathPoints[i - 2] : pathPoints[i - 1];
                Vector3 p1 = pathPoints[i - 1];
                Vector3 p2 = pathPoints[i];
                Vector3 p3 = i + 1 < pathPoints.Count ? pathPoints[i + 1] : pathPoints[i];

                return HermiteInterpolation(p0, p1, p2, p3, t);
            }
        }

        return pathPoints[pathPoints.Count - 1];
    }

    private Vector3 HermiteInterpolation(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return (2f * t3 - 3f * t2 + 1f) * p1 +
               (t3 - 2f * t2 + t) * 0.5f * (p2 - p0) +
               (-2f * t3 + 3f * t2) * p2 +
               (t3 - t2) * 0.5f * (p3 - p1);
    }


    void CheckFoodCollision()
    {
        if (cubes.Count == 0) return;

        GameObject head = cubes[0];
        Vector3 headPosition = head.transform.position;

        float collisionRadius = head.transform.localScale.x * 0.75f;

        Collider[] colliders = Physics.OverlapSphere(headPosition, collisionRadius);
        foreach (var collider in colliders)
        {
            if (foodCubes.Contains(collider.gameObject))
            {
                GameObject food = collider.gameObject;
                DynamicCubeScript foodScript = food.GetComponent<DynamicCubeScript>();
                int foodNumber = foodScript.currentNumber;
                OnSnakeHeadChanged?.Invoke(cubes[0].transform);
                if (isMerging) return;

                isMerging = true;

                foodCubes.Remove(food);

                foodScript.SmoothMove(cubes[cubes.Count - 1].transform.position, 0.5f, () =>
                {
                    SpawnCube(foodNumber);
                    UpdateCachedDistances();
                    Destroy(food);
                    isMerging = false;
                });

                break;
            }
        }
    }

    public void CheckAndMergeCubes()
    {
        if (cubes.Count < 2 || isMerging) return;

        for (int i = cubes.Count - 1; i > 0; i--)
        {
            DynamicCubeScript currentCube = cubes[i].GetComponent<DynamicCubeScript>();
            DynamicCubeScript prevCube = cubes[i - 1].GetComponent<DynamicCubeScript>();

            if (currentCube != null && prevCube != null)
            {
                if (currentCube.currentNumber == prevCube.currentNumber)
                {
                    int newNumber = currentCube.currentNumber * 2;
                    prevCube.SetNumber(newNumber);

                    float groundY = 0.5f;
                    float halfHeight = prevCube.transform.localScale.y / 2.0f;
                    prevCube.transform.position = new Vector3(
                        prevCube.transform.position.x,
                        groundY + halfHeight,
                        prevCube.transform.position.z
                    );

                    Destroy(cubes[i]);
                    cubes.RemoveAt(i);

                    prevCube.PlayMergeAnimation();
                    OnCubeMerged?.Invoke(newNumber);

                    UpdateCachedDistances(); // 确保缓存一致性

                    OnSnakeHeadChanged?.Invoke(cubes.Count > 0 ? cubes[0].transform : null);

                }
                else if (currentCube.currentNumber > prevCube.currentNumber)
                {
                    Vector3 prevPosition = prevCube.transform.position;
                    Vector3 newPosition = currentCube.transform.position;

                    GameObject temp = cubes[i - 1];
                    cubes[i - 1] = cubes[i];
                    cubes[i] = temp;

                    UpdateCachedDistances();

                    cubes[i - 1].GetComponent<DynamicCubeScript>().SmoothMove(prevPosition, 0.2f);
                    cubes[i].GetComponent<DynamicCubeScript>().SmoothMove(newPosition, 0.2f);
                }
            }
        }
    }

    public void SpawnFood()
    {
        if (foodCubes.Count >= maxFoodCount) return;

        GameObject newFood = Instantiate(cubePrefab);

        GameObject ground = GameObject.Find("Ground");
        Renderer groundRenderer = ground.GetComponent<Renderer>();

        float minX = groundRenderer.bounds.min.x;
        float maxX = groundRenderer.bounds.max.x;
        float minZ = groundRenderer.bounds.min.z;
        float maxZ = groundRenderer.bounds.max.z;

        float groundY = groundRenderer.bounds.min.y + 0.5f;

        Vector3 spawnPosition;
        bool positionValid = false;
        do
        {
            float x = UnityEngine.Random.Range(minX, maxX);
            float z = UnityEngine.Random.Range(minZ, maxZ);

            spawnPosition = new Vector3(x, groundY, z);
            positionValid = true;

            foreach (GameObject cube in cubes)
            {
                if (cube == null) continue;
                // 使用 CalculateDynamicGap 计算间隙
                if (Vector3.Distance(cube.transform.position, spawnPosition) < CalculateDynamicGap(cube, newFood))
                {
                    positionValid = false;
                    break;
                }
            }

            foreach (GameObject food in foodCubes)
            {
                if (food == null) continue;
                // 使用现有逻辑计算食物间距
                if (Vector3.Distance(food.transform.position, spawnPosition) < (food.transform.localScale.x + fixedGap) * 1.5f)
                {
                    positionValid = false;
                    break;
                }
            }
        } while (!positionValid);

        // 设置新食物的最终位置
        newFood.transform.position = spawnPosition;


        DynamicCubeScript foodScript = newFood.GetComponent<DynamicCubeScript>();
        foodScript.SetNumber((int)Mathf.Pow(2, UnityEngine.Random.Range(1, 7)));
        foodScript.PlaySpawnAnimation();
        foodCubes.Add(newFood);
    }

    void Update()
    {
        foodSpawnTimer += Time.deltaTime;
        if (foodSpawnTimer >= foodSpawnInterval)
        {
            SpawnFood();
            foodSpawnTimer = 0f;
        }
    }
}