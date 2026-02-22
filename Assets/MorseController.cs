using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems; 

public class MorseController : MonoBehaviour
{
    [Header("UI элементы")]
    public TextMeshProUGUI terminalText; 
    public TextMeshProUGUI resultText;   
    public TextMeshProUGUI currentSymbolsText; 
    public GameObject hintPanel;
    public TextMeshProUGUI timerText; // НОВОЕ: Текст обратного отсчета
    
    [Header("Кнопка Ввода (для визуала)")]
    public Image transmitterButtonImage; 
    public Color pressedColor = new Color(0.7f, 0.7f, 0.7f); 
    private Color normalColor = Color.white;

    [Header("Фон и Эффекты Уровней")]
    public Image backgroundImage;          
    public Sprite level2BackgroundSprite;  
    public GameObject level2Effects;       

    [Header("Настройки Аудио и Ввода")]
    public AudioSource audioSource;
    public AudioClip level2TransitionSound; 
    public float dashThreshold = 0.25f; 
    public float letterPause = 0.6f;    
    public float textPrintSpeed = 0.05f; 

    [Header("Таймер (Финальный уровень)")]
    public float level3Time = 30f; // Сколько секунд даем на ввод слова OPEN
    private float timeLeft;
    private bool isTimerRunning = false;

    // ОБНОВЛЕНО: Оставили 3 финальных уровня
    private string[] levels = { "SOS", "AIR", "OPEN" };
    private int currentLevelIndex = 0;

    private string currentLetterCode = ""; 
    private string translatedWord = "";   
    private float lastReleaseTime;
    private bool isPressing = false;
    private float pressTime;
    private bool letterProcessed = true;
    private bool canType = false; 

    // --- СЮЖЕТНЫЕ ТЕКСТЫ ---
    private string level1Intro = "...\n... --- ...\nПРИЕМ?\nЕСЛИ КТО-ТО ЖИВ — ОТВЕТЬТЕ\n\n[ ВВЕДИТЕ: SOS ]";
    private string level1Outro = "СИГНАЛ ПОЛУЧЕН\nИДЕНТИФИЦИРУЙТЕ СЕБЯ\n...\nВАША СТАНЦИЯ НЕ ОТВЕЧАЕТ НА АВТОЗАПРОС";
    private string level2Outro = "ДАТЧИКИ ПОКАЗЫВАЮТ РАЗГЕРМЕТИЗАЦИЮ\nВ ЭКИПАЖЕ 4 ЧЕЛОВЕКА\nПОДТВЕРДИТЕ СОСТОЯНИЕ ЭКИПАЖА\n...\nПРИНЯТО";
    
    // Финальный текст после прохождения
    private string level3Outro = "...\nШЛЮЗ ОТКРЫТ. МЫ ВИДИМ ВАС.\nВЫ МОЛЧАЛИ ВСЕ 18 ЧАСОВ\nНО СИГНАЛ ПРОДОЛЖАЛ ПЕРЕДАВАТЬСЯ...\n\nКОНЕЦ СВЯЗИ.";

    private Dictionary<string, char> morseDict = new Dictionary<string, char>() {
        {".-", 'A'}, {"-...", 'B'}, {"-.-.", 'C'}, {"-..", 'D'}, {".", 'E'},
        {"..-.", 'F'}, {"--.", 'G'}, {"....", 'H'}, {"..", 'I'}, {".---", 'J'},
        {"-.-", 'K'}, {".-..", 'L'}, {"--", 'M'}, {"-.", 'N'}, {"---", 'O'},
        {".--.", 'P'}, {"--.-", 'Q'}, {".-.", 'R'}, {"...", 'S'}, {"-", 'T'},
        {"..-", 'U'}, {"...-", 'V'}, {".--", 'W'}, {"-..-", 'X'}, {"-.--", 'Y'},
        {"--..", 'Z'}, {"-----", '0'}, {".----", '1'}, {"..---", '2'}, {"...--", '3'},
        {"....-", '4'}, {".....", '5'}, {"-....", '6'}, {"--...", '7'}, {"---..", '8'}, {"----.", '9'}
    };

    void Start() {
        if(hintPanel != null) hintPanel.SetActive(false);
        if(transmitterButtonImage != null) normalColor = transmitterButtonImage.color;
        if(level2Effects != null) level2Effects.SetActive(false); 
        if(timerText != null) timerText.gameObject.SetActive(false); // Прячем таймер в начале
        
        resultText.text = "";
        currentSymbolsText.text = "";
        
        StartCoroutine(StartLevel1());
    }

    void Update() {
        // --- ЛОГИКА ТАЙМЕРА ---
        if (isTimerRunning) {
            timeLeft -= Time.deltaTime; // Отнимаем время
            
            if (timerText != null) {
                timerText.text = Mathf.CeilToInt(timeLeft).ToString() ;
                // Если остается меньше 10 секунд, можно сделать текст красным (по желанию)
                if (timeLeft <= 10f) timerText.color = Color.red;
            }

            // Если время вышло — конец игры!
            if (timeLeft <= 0) {
                GameOver();
            }
        }

        if (!isPressing && !letterProcessed && (Time.time - lastReleaseTime > letterPause)) {
            ProcessLetter();
        }
    }

    public void OnTransmitterDown() {
        if ((hintPanel != null && hintPanel.activeSelf) || !canType) return;
        pressTime = Time.time;
        isPressing = true;
        letterProcessed = false;
        
        if (transmitterButtonImage != null) transmitterButtonImage.color = pressedColor;
        if (audioSource) audioSource.Play();
    }

    public void OnTransmitterUp() {
        if (!isPressing) return; 
        isPressing = false;
        float duration = Time.time - pressTime;
        currentLetterCode += (duration < dashThreshold) ? "." : "-";
        currentSymbolsText.text = currentLetterCode;
        lastReleaseTime = Time.time;
        
        if (transmitterButtonImage != null) transmitterButtonImage.color = normalColor;
        if (audioSource) audioSource.Stop();
    }

    void ProcessLetter() {
        letterProcessed = true;
        if (morseDict.ContainsKey(currentLetterCode)) {
            translatedWord += morseDict[currentLetterCode];
            resultText.text = translatedWord;
            CheckWord();
        }
        currentLetterCode = ""; 
        currentSymbolsText.text = "";
    }

    void CheckWord() {
        if (translatedWord == levels[currentLevelIndex]) {
            if (currentLevelIndex == 0) StartCoroutine(CompleteLevel1());
            else if (currentLevelIndex == 1) StartCoroutine(CompleteLevel2());
            else if (currentLevelIndex == 2) StartCoroutine(CompleteLevel3());
        }
    }

    IEnumerator StartLevel1() {
        canType = false; 
        yield return StartCoroutine(PrintText(level1Intro));
        canType = true;  
    }

    IEnumerator CompleteLevel1() {
        canType = false; 
        yield return new WaitForSeconds(1f); 
        resultText.text = ""; 
        translatedWord = "";
        
        yield return StartCoroutine(PrintText(level1Outro));
        yield return new WaitForSeconds(3f); 
        
        if (level2TransitionSound != null && audioSource != null) {
            audioSource.PlayOneShot(level2TransitionSound); 
        }
        
        if (backgroundImage != null && level2BackgroundSprite != null) {
            backgroundImage.sprite = level2BackgroundSprite; 
        }
        
        if(level2Effects != null) {
            level2Effects.SetActive(true); 
        }
        
        currentLevelIndex++;
        terminalText.text = "МЫ ПОТЕРЯЛИ С ВАМИ КОНТАКТ 18 ЧАСОВ НАЗАД...\nСООБЩИТЕ СОСТОЯНИЕ СИСТЕМ\n\n[ ВВЕДИТЕ: AIR ]";
        canType = true;
    }

    IEnumerator CompleteLevel2() {
        canType = false; 
        yield return new WaitForSeconds(1f); 
        resultText.text = ""; 
        translatedWord = "";
        
        yield return StartCoroutine(PrintText(level2Outro));
        yield return new WaitForSeconds(3f); 
        
        currentLevelIndex++;
        
        // --- СТАРТ 3 УРОВНЯ (ФИНАЛ И ПАНИКА) ---
        terminalText.text = "КРИТИЧЕСКАЯ НЕХВАТКА КИСЛОРОДА!\nОТКРОЙТЕ АВАРИЙНЫЙ ШЛЮЗ ВРУЧНУЮ!\n\n[ КОД: OPEN ]";
        
        timeLeft = level3Time; // Устанавливаем таймер
        isTimerRunning = true; // Запускаем обратный отсчет
        if(timerText != null) {
            timerText.gameObject.SetActive(true);
            timerText.color = Color.white; // Возвращаем белый цвет на случай перезапуска
        }
        
        canType = true;
    }

    IEnumerator CompleteLevel3() {
        canType = false; 
        isTimerRunning = false; // Победа! Останавливаем таймер
        if(timerText != null) timerText.gameObject.SetActive(false); // Прячем таймер

        yield return new WaitForSeconds(1f); 
        resultText.text = ""; 
        translatedWord = "";
        currentSymbolsText.text = "";
        
        yield return StartCoroutine(PrintText(level3Outro));
    }

    // --- ФУНКЦИЯ ПРОИГРЫША ---
    void GameOver() {
        isTimerRunning = false;
        canType = false; // Блокируем ввод
        
        terminalText.text = "СИСТЕМА ЖИЗНЕОБЕСПЕЧЕНИЯ ОТКЛЮЧЕНА...\n\nСВЯЗЬ ПОТЕРЯНА.";
        resultText.text = "";
        currentSymbolsText.text = "";
        
        if (timerText != null) timerText.text = "КИСЛОРОД: 0 сек";
    }

    IEnumerator PrintText(string textToPrint) {
        terminalText.text = "";
        foreach (char letter in textToPrint.ToCharArray()) {
            terminalText.text += letter;
            yield return new WaitForSeconds(textPrintSpeed);
        }
    }

    public void DeleteLastLetter() {
        if (!canType || hintPanel.activeSelf) return; 
        if (translatedWord.Length > 0) {
            translatedWord = translatedWord.Substring(0, translatedWord.Length - 1);
            resultText.text = translatedWord;
        }
    }

    public void ToggleHint() {
        if (hintPanel != null) {
            bool isShowing = !hintPanel.activeSelf;
            hintPanel.SetActive(isShowing);
            
            if (terminalText != null) terminalText.gameObject.SetActive(!isShowing);
            if (resultText != null) resultText.gameObject.SetActive(!isShowing);
            if (currentSymbolsText != null) currentSymbolsText.gameObject.SetActive(!isShowing);
        }
    }
}