using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameOverUI : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private GameObject panel;
    [SerializeField] private Button retryButton;
    [SerializeField, Min(0f)] private float showDelay = 0.6f; // 사망 모션을 보여 준 뒤 UI를 표시할 시간(초)

    private float deathElapsedTime;
    private bool isShowing;

    private void Awake()
    {
        panel.SetActive(false);
        retryButton.onClick.AddListener(Restart);
    }

    private void Update()
    {
        if (!player.IsDead || isShowing)
            return;

        deathElapsedTime += Time.unscaledDeltaTime;
        if (deathElapsedTime < showDelay)
            return;

        isShowing = true;
        panel.SetActive(true);
        Time.timeScale = 0f;
        retryButton.Select();
    }

    private void Restart()
    {
        if (!isShowing)
            return;

        retryButton.interactable = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
