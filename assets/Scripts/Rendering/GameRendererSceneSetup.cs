using UnityEngine;

/// <summary>
/// GameRenderer Scene 자동 설정 도구 (Editor 전용)
/// Scene에 GameRenderer에 필요한 GameObject들을 자동 생성합니다.
/// </summary>
public class GameRendererSceneSetup : MonoBehaviour
{
    [Header("Sprite Assets")]
    [SerializeField] private Sprite playerSprite;
    [SerializeField] private Sprite monsterSprite;
    [SerializeField] private Sprite backgroundSprite;

    [Header("References (자동 설정)")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private SpriteRenderer monsterRenderer;
    [SerializeField] private SpriteRenderer backgroundRenderer;

    private void Awake()
    {
        // GameRenderer 컴포넌트 추가
        var gameRenderer = GetComponent<GameRenderer>();
        if (gameRenderer == null)
        {
            gameRenderer = gameObject.AddComponent<GameRenderer>();
        }

        // Camera 설정
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        if (mainCamera == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.transform.position = new Vector3(0, 0, -10);
            mainCamera = camObj.AddComponent<Camera>();
        }

        // Player 설정
        SetupPlayer();

        // Monster 설정
        SetupMonster();

        // Background 설정
        SetupBackground();

        Debug.Log("[GameRendererSceneSetup] Scene 설정 완료");
    }

    private void SetupPlayer()
    {
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj == null)
        {
            playerObj = new GameObject("Player");
            playerObj.transform.position = new Vector3(-3f, 0, 0);
        }

        playerRenderer = playerObj.GetComponent<SpriteRenderer>();
        if (playerRenderer == null)
        {
            playerRenderer = playerObj.AddComponent<SpriteRenderer>();
        }

        if (playerSprite != null)
        {
            playerRenderer.sprite = playerSprite;
        }

        playerRenderer.sortingOrder = 1;
    }

    private void SetupMonster()
    {
        GameObject monsterObj = GameObject.Find("Monster");
        if (monsterObj == null)
        {
            monsterObj = new GameObject("Monster");
            monsterObj.transform.position = new Vector3(3f, 0, 0);
        }

        monsterRenderer = monsterObj.GetComponent<SpriteRenderer>();
        if (monsterRenderer == null)
        {
            monsterRenderer = monsterObj.AddComponent<SpriteRenderer>();
        }

        if (monsterSprite != null)
        {
            monsterRenderer.sprite = monsterSprite;
        }

        monsterRenderer.sortingOrder = 1;
    }

    private void SetupBackground()
    {
        GameObject bgObj = GameObject.Find("Background");
        if (bgObj == null)
        {
            bgObj = new GameObject("Background");
            bgObj.transform.position = new Vector3(0, 0, 5);
        }

        backgroundRenderer = bgObj.GetComponent<SpriteRenderer>();
        if (backgroundRenderer == null)
        {
            backgroundRenderer = bgObj.AddComponent<SpriteRenderer>();
        }

        if (backgroundSprite != null)
        {
            backgroundRenderer.sprite = backgroundSprite;
            backgroundRenderer.size = new Vector2(20, 15);
        }

        backgroundRenderer.sortingOrder = 0;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor에서만 사용: 스프라이트 자동 찾기
    /// </summary>
    [UnityEditor.MenuItem("Tools/AFK RPG/Setup GameRenderer Scene")]
    public static void AutoSetup()
    {
        Debug.Log("[GameRendererSceneSetup] 에디터 메뉴는 아직 구현되지 않았습니다.");
    }
#endif
}
