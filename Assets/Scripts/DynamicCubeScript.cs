using System.Collections;
using UnityEngine;

public class DynamicCubeScript : MonoBehaviour
{
    public Renderer cubeRenderer; // ���������Ⱦ��
    public Texture2D[] numberTextures; // ������ͼ����
    public int currentNumber = 2; // ��ǰ��ʾ������

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("δ�ҵ� Rigidbody �����");
            enabled = false;
            return;
        }
        rb.useGravity = false;
        rb.isKinematic = true;
        UpdateCubeAppearance();
    }
    void LateUpdate()
    {
        // ��ֹ�����崩������
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

    public event System.Action OnDestroyEvent;

    private void OnDestroy()
    {
        OnDestroyEvent?.Invoke();
    }


    void UpdateCubeAppearance()
    {
        // ��֤��ǰ�����Ƿ�Ϊ 2 ����
        if (Mathf.Log(currentNumber, 2) % 1 != 0)
        {
            Debug.LogWarning("Invalid number for cube appearance: " + currentNumber);
            return;
        }

        // ��֤�����Ƿ���ȷ����
        if (numberTextures == null || numberTextures.Length == 0)
        {
            Debug.LogWarning("Number textures array is not properly assigned!");
            return;
        }

        int textureIndex = (int)(Mathf.Log(currentNumber, 2) - 1);
        if (textureIndex >= 0 && textureIndex < numberTextures.Length)
        {
            cubeRenderer.material.mainTexture = numberTextures[textureIndex];
        }

        // �����µ����ű���
        float scaleFactor = Mathf.Log(currentNumber, 2) / 2.0f + 0.5f;
        transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);

        // ȷ��������ײ����ŵ���
        float groundY = 0.5f; // ����߶ȣ����ݳ�����ʵ�����ã�
        float halfHeight = transform.localScale.y / 2.0f;
        transform.position = new Vector3(transform.position.x, groundY + halfHeight, transform.position.z);
        transform.position = new Vector3(
    transform.position.x,
    0.5f + transform.localScale.y / 2.0f,
    transform.position.z
);

    }




    public void SetNumber(int newNumber)
    {
        currentNumber = newNumber;
        UpdateCubeAppearance();
    }

    public void PlayMergeAnimation()
    {
        StartCoroutine(MergeAnimation());
    }

    private IEnumerator MergeAnimation()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 expandedScale = originalScale * 1.2f;
        float animationTime = 0.2f;

        float timer = 0;
        while (timer < animationTime)
        {
            transform.localScale = Vector3.Lerp(originalScale, expandedScale, timer / animationTime);
            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0;
        while (timer < animationTime)
        {
            transform.localScale = Vector3.Lerp(expandedScale, originalScale, timer / animationTime);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    public void SmoothMove(Vector3 targetPosition, float duration, System.Action onComplete = null)
    {
        StartCoroutine(SmoothMoveCoroutine(targetPosition, duration, onComplete));
    }

    private Vector3 HermiteInterpolation(Vector3 start, Vector3 end, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return (2f * t3 - 3f * t2 + 1f) * start + (-2f * t3 + 3f * t2) * end;
    }

    private IEnumerator SmoothMoveCoroutine(Vector3 targetPosition, float duration, System.Action onComplete)
    {
        float elapsed = 0f;
        Vector3 startPosition = transform.position;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = HermiteInterpolation(startPosition, targetPosition, t); // ʹ�� Hermite ��ֵ
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        onComplete?.Invoke();
    }



    private IEnumerator SpawnAnimation()
    {
        Vector3 originalScale = transform.localScale;
        transform.localScale = Vector3.zero;

        float timer = 0;
        float duration = 0.3f;

        while (timer < duration)
        {
            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    public void PlaySpawnAnimation()
    {
        StartCoroutine(SpawnAnimation());
    }
}