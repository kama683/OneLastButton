using UnityEngine;
using UnityEngine.UI;

public class FlickerEffect : MonoBehaviour
{
    [Header("Настройки мигания")]
    public Image imageToFlicker; // Картинка, которая будет мигать
    public float minAlpha = 0.0f; // Минимальная прозрачность (0 - полностью невидимо)
    public float maxAlpha = 0.8f; // Максимальная прозрачность (1 - полностью видно)
    public float baseFlickerSpeed = 0.1f; // Базовая скорость мигания

    private float timer;

    void Start()
    {
        // Если картинка не назначена, скрипт берет ту, на которой висит
        if (imageToFlicker == null)
        {
            imageToFlicker = GetComponent<Image>();
        }
    }

    void Update()
    {
        if (imageToFlicker == null) return;

        timer += Time.deltaTime;

        // Когда таймер достигает цели, меняем прозрачность
        if (timer >= baseFlickerSpeed)
        {
            Color currentColor = imageToFlicker.color;
            currentColor.a = Random.Range(minAlpha, maxAlpha); // Случайная прозрачность
            imageToFlicker.color = currentColor;

            // Сбрасываем таймер со случайной задержкой, чтобы мигание было "рваным" и пугающим
            timer = Random.Range(0f, baseFlickerSpeed * 0.5f);
        }
    }
}