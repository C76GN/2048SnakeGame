using UnityEngine;

public class InputManager : MonoBehaviour
{
    public event System.Action<Vector3> OnDirectionChanged;
    public event System.Action OnSpawnCubeRequested;

    void Update()
    {
        HandleMovementInput();
        HandleSpawnInput();
    }

    void HandleMovementInput()
    {
        Vector3 newDirection = Vector3.zero;

        if (Input.GetKeyDown(KeyCode.W)) newDirection = Vector3.forward;
        if (Input.GetKeyDown(KeyCode.S)) newDirection = Vector3.back;
        if (Input.GetKeyDown(KeyCode.A)) newDirection = Vector3.left;
        if (Input.GetKeyDown(KeyCode.D)) newDirection = Vector3.right;

        if (newDirection != Vector3.zero)
        {
            OnDirectionChanged?.Invoke(newDirection);
        }
    }

    void HandleSpawnInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnSpawnCubeRequested?.Invoke();
        }
    }

    void LateUpdate()
    {
        // 防止正方体穿过地面
        float groundY = 0.5f;
        float halfHeight = transform.localScale.y / 2.0f;

        if (transform.position.y < groundY + halfHeight)
        {
            transform.position = new Vector3(
                transform.position.x,
                groundY + halfHeight,
                transform.position.z
            );
        }
    }

}