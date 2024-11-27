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
    public IReadOnlyList<GameObject> Cubes => cubes; // ֻ������
    private List<GameObject> foodCubes = new List<GameObject>();
    private Vector3 moveDirection = Vector3.forward;

    public float foodSpawnInterval = 5f;
    private float foodSpawnTimer = 0f;

    public event System.Action<int> OnCubeMerged;
    private bool isMerging = false; // �ϲ���־λ

    private List<Vector3> pathPoints = new List<Vector3>();
    public int pathResolution = 10; // ·������ķֱ���
    private List<float> cachedDistances = new List<float>();

    public int maxFoodCount = 20; // ���ʳ����������

    public event System.Action<Transform> OnSnakeHeadChanged;

    public void SpawnCube(int number)
    {
        // ʵ�����µķ���
        GameObject newCube = Instantiate(cubePrefab);

        if (cubes.Count == 0)
        {
            // ����ǵ�һ�����飬�����ڳ�ʼ���ɵ�
            newCube.transform.position = spawnPoint.position;
        }
        else
        {
            // ʹ��ͳһ�Ķ�̬��϶�����߼�
            GameObject tail = cubes[cubes.Count - 1];
            float dynamicGap = CalculateDynamicGap(tail, newCube); // ����ͳһ�ļ�϶���㺯��

            // ���㷽����·���������
            Vector3 direction = (pathPoints.Count > 1)
                ? (pathPoints[pathPoints.Count - 1] - pathPoints[pathPoints.Count - 2]).normalized
                : Vector3.back;

            // ����̬��϶����λ��
            newCube.transform.position = tail.transform.position + direction * dynamicGap;
        }

        // ����λ�ã�ȷ�������ڵ����ϣ�
        float groundY = 0.5f; // ����߶�
        float halfHeight = newCube.transform.localScale.y / 2.0f;
        newCube.transform.position = new Vector3(
            newCube.transform.position.x,
            groundY + halfHeight,
            newCube.transform.position.z
        );

        // ��ʼ���·���Ľű���������ֵ
        DynamicCubeScript cubeScript = newCube.GetComponent<DynamicCubeScript>();
        if (cubeScript != null)
        {
            cubeScript.SetNumber(number);
            cubeScript.PlaySpawnAnimation();
        }

        // ���·�����ӵ��ߵķ����б�
        cubes.Add(newCube);

        // ���»���ľ���
        UpdateCachedDistances();

        // ��鲢����ϲ��߼�
        CheckAndMergeCubes();
    }


    private float CalculateDynamicGap(GameObject current, GameObject next)
    {
        float currentSize = current.transform.localScale.x;
        float nextSize = next.transform.localScale.x;

        // �ȶ��ļ�϶���㣬���⸡�����
        return fixedGap + Mathf.Round((currentSize + nextSize) * 0.5f * 1000f) / 1000f;
    }

    public void SetMoveDirection(Vector3 direction)
    {
        moveDirection = direction;
    }

    public void MoveSnake()
    {
        // ��ǰ���صļ��
        if (cubes == null || cubes.Count == 0) return;

        try
        {
            // ��������������ظ�����
            GameObject head = cubes[0];
            float deltaTime = Time.fixedDeltaTime;

            // ������ͷ����λ��
            Vector3 newHeadPosition = head.transform.position + moveDirection * (moveSpeed * deltaTime * 0.5f);
            newHeadPosition.y = 0.5f;

            // ����·������뵽�б�Ŀ�ͷ
            pathPoints.Insert(0, newHeadPosition);

            // ����·������������Ƴ��������Ƶ����һ����
            int dynamicResolution = Mathf.CeilToInt(moveSpeed * 3f * cubes.Count); // �����ƶ��ٶȶ�̬�����ֱ���
            int maxPoints = Mathf.Max(dynamicResolution * (cubes.Count + 1), 50); // ��֤���� 10 ����
            if (pathPoints.Count > maxPoints)
            {
                pathPoints.RemoveAt(pathPoints.Count - 1); // �Ƴ�����ĵ�
            }
            // �ƶ���ͷ
            head.GetComponent<DynamicCubeScript>().SmoothMove(newHeadPosition, deltaTime, () =>
            {
                OnSnakeHeadChanged?.Invoke(cubes[0].transform); // ȷ�����������Ŀ��
            });


            // ���岿�ָ���ͷ��·���ƶ�
            for (int i = 1; i < cubes.Count; i++)
            {
                float targetDistance = CalculateTargetDistance(i);
                Vector3 targetPosition = GetPositionAlongPath(targetDistance);

                if (Vector3.Distance(cubes[i].transform.position, targetPosition) > fixedGap * 1.2f) // �ʵ��ſ���ֵ
                {
                    cubes[i].GetComponent<DynamicCubeScript>().SmoothMove(targetPosition, deltaTime);
                }

            }

            // ��ת�Ż�
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
            targetDistance += CalculateDynamicGap(cubes[j], cubes[j + 1]); // ʹ�ö�̬��϶
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
            distance += CalculateDynamicGap(cubes[i], cubes[i + 1]); // ʹ�ö�̬��϶
        }
        // ������һ������ľ���
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

                // ��Ϊ���β�ֵ (Catmull-Rom)
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

                    UpdateCachedDistances(); // ȷ������һ����

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
                // ʹ�� CalculateDynamicGap �����϶
                if (Vector3.Distance(cube.transform.position, spawnPosition) < CalculateDynamicGap(cube, newFood))
                {
                    positionValid = false;
                    break;
                }
            }

            foreach (GameObject food in foodCubes)
            {
                if (food == null) continue;
                // ʹ�������߼�����ʳ����
                if (Vector3.Distance(food.transform.position, spawnPosition) < (food.transform.localScale.x + fixedGap) * 1.5f)
                {
                    positionValid = false;
                    break;
                }
            }
        } while (!positionValid);

        // ������ʳ�������λ��
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