using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;

public class MorseController : MonoBehaviour
{
    [Header("UI элементы")]
    public TextMeshProUGUI terminalText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI currentSymbolsText;
    public GameObject hintPanel;
    public TextMeshProUGUI timerText;

    [Header("Hold UI (градусник)")]
    public Image holdBarFill;

    [Header("Цвета шкалы по таймингам")]
    public Color dotColor = Color.green;                    // < 0.25
    public Color dashColor = Color.yellow;                  // 0.25..1.2
    public Color deleteColor = new Color(1f, 0.55f, 0f);    // 1.2..2.5
    public Color hintColor = Color.red;                     // 2.5..4.0
    public Color clearColor = new Color(1f, 0f, 0f);        // >= 4.0

    [Header("Единственная кнопка (Signal Button)")]
    public Button signalButton;
    public Image signalButtonImage;
    public Color pressedColor = new Color(0.7f, 0.7f, 0.7f);
    private Color normalColor = Color.white;

    [Header("Пороги удержания (сек)")]
    public float dotMax = 0.25f;
    public float dashMax = 0.7f;            // оставлено (не используется)
    public float deleteHold = 1.2f;
    public float hintHold = 2.5f;
    public float clearAllHold = 4.0f;

    [Header("Паузы и печать")]
    public float letterPause = 0.6f;
    public float textPrintSpeed = 0.05f;

    [Header("Паузы между экранами (Level 5)")]
    public float level5PagePause = 3.5f;   // СДЕЛАЛ БОЛЬШЕ (огромные паузы)
    public float level5FinalPause = 1.2f;  // чуть больше перед показом вариантов

    [Header("Паузы между экранами (ENDING)")]
    public float endingPagePause = 4.0f;     // СДЕЛАЛ БОЛЬШЕ (огромные паузы)
    public float ellipsisPause = 6.0f;       // <-- 6 секунд на текст "..."

    [Header("Фон и эффекты уровней")]
    public Image backgroundImage;
    public Sprite level2BackgroundSprite;
    public GameObject level2Effects;

    [Header("Аудио")]
    public AudioSource audioSource;          // для сигнала (пиканье)
    public AudioClip level2TransitionSound;

    [Header("SFX (опционально)")]
    public AudioSource sfxSource;            // можно оставить None
    public AudioClip crackSound;             // можно оставить None
    public float preTextPause = 1f;          // пауза перед текстом 3 уровня

    [Header("Паника в 3-м уровне (кнопка иногда не слушается)")]
    [Range(0f, 1f)] public float level3DropChance = 0.18f;
    public float level3DropCooldown = 0.8f;
    public AudioClip glitchClip;             // можно None
    public GameObject glitchFlash;           // можно None

    [Header("Таймер (Финальный уровень)")]
    public float level3Time = 100f;
    private float timeLeft;
    private bool isTimerRunning = false;

    // уровни: 0=SOS(+ID сцена), 1=AIR, 2=OPEN
    private string[] levels = { "SOS", "AIR", "OPEN" };
    private int currentLevelIndex = 0;

    // ввод
    private string currentLetterCode = "";
    private string translatedWord = "";
    private float lastReleaseTime;
    private float lastTranslatedLetterTime = -999f;

    private bool isPressing = false;
    private float pressTime;

    // флаги
    private bool canType = false;
    private bool hintOpen = false;
    private bool letterProcessed = true;

    private bool actionConsumedThisHold = false;
    private bool hintPending = false;

    // УРОВЕНЬ 1
    private bool level1AwaitingID = false;
    private bool level1SequenceRunning = false;

    // Глюк 3 уровня
    private float lastGlitchTime = -999f;

    // УРОВЕНЬ 5
    private bool level5ChoiceActive = false;
    private bool level5SequenceRunning = false;

    private string level1Intro =
        "...\n... --- ...\nПРИЕМ?\n\nЕСЛИ КТО-ТО ЖИВ — ОТВЕТЬТЕ\n\n[ ВВЕДИТЕ: SOS ]";

    private Dictionary<string, char> morseDict = new Dictionary<string, char>() {
        {".-", 'A'}, {"-...", 'B'}, {"-.-.", 'C'}, {"-..", 'D'}, {".", 'E'},
        {"..-.", 'F'}, {"--.", 'G'}, {"....", 'H'}, {"..", 'I'}, {".---", 'J'},
        {"-.-", 'K'}, {".-..", 'L'}, {"--", 'M'}, {"-.", 'N'}, {"---", 'O'},
        {".--.", 'P'}, {"--.-", 'Q'}, {".-.", 'R'}, {"...", 'S'}, {"-", 'T'},
        {"..-", 'U'}, {"...-", 'V'}, {".--", 'W'}, {"-..-", 'X'}, {"-.--", 'Y'},
        {"--..", 'Z'},
        {"-----", '0'}, {".----", '1'}, {"..---", '2'}, {"...--", '3'},
        {"....-", '4'}, {".....", '5'}, {"-....", '6'}, {"--...", '7'}, {"---..", '8'}, {"----.", '9'}
    };

    void Start()
    {
        if (hintPanel != null) hintPanel.SetActive(false);
        hintOpen = false;

        if (signalButtonImage != null) normalColor = signalButtonImage.color;

        if (level2Effects != null) level2Effects.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);

        if (resultText != null) resultText.text = "";
        if (currentSymbolsText != null) currentSymbolsText.text = "";

        if (holdBarFill != null)
        {
            holdBarFill.fillAmount = 0f;
            holdBarFill.color = dotColor;
        }

        if (glitchFlash != null) glitchFlash.SetActive(false);

        StartCoroutine(StartLevel1());
    }

    void Update()
    {
        if (isTimerRunning)
        {
            timeLeft -= Time.deltaTime;

            if (timerText != null)
            {
                timerText.text = Mathf.CeilToInt(timeLeft).ToString();
                if (timeLeft <= 10f) timerText.color = Color.red;
            }

            if (timeLeft <= 0f) GameOver();
        }

        if (!isPressing && !letterProcessed && (Time.time - lastReleaseTime > letterPause))
        {
            ProcessLetter();
        }

        if (currentLevelIndex == 0 && level1AwaitingID && !level1SequenceRunning)
        {
            if (translatedWord.Length > 0 && (Time.time - lastTranslatedLetterTime) > 13.0f)
            {
                StartCoroutine(Level1_AfterAnyIDAttempt_ShowError_AndGoLevel2(translatedWord));
            }
        }

        // Level5: только ME или CORE
        if (level5ChoiceActive && !level5SequenceRunning)
        {
            string choice = translatedWord.Trim().ToUpper();

            if (choice.Length == 2 && choice == "ME")
            {
                StartCoroutine(Level5_PlayEnding("ME"));
            }
            else if (choice.Length == 4 && choice == "CORE")
            {
                StartCoroutine(Level5_PlayEnding("CORE"));
            }
            else if (choice.Length > 4)
            {
                ResetInputText();
            }
        }

        if (isPressing && !actionConsumedThisHold && !hintOpen)
        {
            float held = Time.time - pressTime;

            if (held >= clearAllHold)
            {
                ClearAllInput();
                actionConsumedThisHold = true;
                hintPending = false;

                if (holdBarFill != null) holdBarFill.fillAmount = 0f;
                return;
            }

            if (held >= hintHold)
            {
                hintPending = true;
            }
        }

        UpdateHoldBar();
    }

    public void OnSignalDown()
    {
        if (!canType) return;

        isPressing = true;
        pressTime = Time.time;
        letterProcessed = false;

        actionConsumedThisHold = false;
        hintPending = false;

        if (signalButtonImage != null) signalButtonImage.color = pressedColor;
        if (audioSource != null) audioSource.Play();
    }

    public void OnSignalUp()
    {
        if (!isPressing) return;

        isPressing = false;
        float duration = Time.time - pressTime;

        if (signalButtonImage != null) signalButtonImage.color = normalColor;
        if (audioSource != null) audioSource.Stop();

        if (hintOpen)
        {
            CloseHint();
            return;
        }

        if (actionConsumedThisHold) return;

        if (hintPending)
        {
            OpenHint();
            return;
        }

        if (duration >= deleteHold)
        {
            DeleteLastLetter();
            return;
        }

        if (!canType || hintOpen) return;

        if (duration < dotMax) AddSymbol(".");
        else if (duration < deleteHold) AddSymbol("-");
    }

    private void AddSymbol(string symbol)
    {
        if (!canType || hintOpen) return;

        if (currentLevelIndex == 2 && ShouldGlitchNow())
        {
            TriggerGlitchFeedback();
            lastReleaseTime = Time.time;
            return;
        }

        currentLetterCode += symbol;
        if (currentSymbolsText != null) currentSymbolsText.text = currentLetterCode;
        lastReleaseTime = Time.time;
    }

    private bool ShouldGlitchNow()
    {
        if (Time.time - lastGlitchTime < level3DropCooldown) return false;
        if (Random.value < level3DropChance)
        {
            lastGlitchTime = Time.time;
            return true;
        }
        return false;
    }

    private void TriggerGlitchFeedback()
    {
        if (sfxSource != null && glitchClip != null) sfxSource.PlayOneShot(glitchClip);
        else if (audioSource != null && glitchClip != null) audioSource.PlayOneShot(glitchClip);

        if (glitchFlash != null) StartCoroutine(GlitchFlashRoutine());
    }

    IEnumerator GlitchFlashRoutine()
    {
        glitchFlash.SetActive(true);
        yield return new WaitForSeconds(0.08f);
        glitchFlash.SetActive(false);
    }

    void ProcessLetter()
    {
        letterProcessed = true;
        if (string.IsNullOrEmpty(currentLetterCode)) return;

        if (morseDict.TryGetValue(currentLetterCode, out char letter))
        {
            translatedWord += letter;
            lastTranslatedLetterTime = Time.time;
            if (resultText != null) resultText.text = translatedWord;
            CheckWord();
        }

        currentLetterCode = "";
        if (currentSymbolsText != null) currentSymbolsText.text = "";
    }

    void CheckWord()
    {
        if (level1SequenceRunning) return;
        if (level5SequenceRunning) return;
        if (level5ChoiceActive) return;

        if (currentLevelIndex == 0 && !level1AwaitingID)
        {
            if (translatedWord == "SOS") StartCoroutine(Level1_AfterSOS_RequestID());
            return;
        }

        if (currentLevelIndex > 0 && currentLevelIndex <= 2 && translatedWord == levels[currentLevelIndex])
        {
            if (currentLevelIndex == 1) StartCoroutine(CompleteLevel2());
            else if (currentLevelIndex == 2) StartCoroutine(CompleteLevel3());
        }
    }

    IEnumerator StartLevel1()
    {
        canType = false;
        level1AwaitingID = false;
        level1SequenceRunning = true;

        ResetInputText();
        yield return StartCoroutine(PrintText(level1Intro));

        level1SequenceRunning = false;
        canType = true;
    }

    IEnumerator Level1_AfterSOS_RequestID()
    {
        level1SequenceRunning = true;
        canType = false;

        yield return new WaitForSeconds(0.6f);
        ResetInputText();

        string text =
            "СИГНАЛ ПОЛУЧЕН\n" +
            "КАНАЛ СВЯЗИ ВОССТАНОВЛЕН\n\n" +
            "ИДЕНТИФИЦИРУЙТЕ СЕБЯ\n\n" +
            "[ ВВЕДИТЕ: ID ]";

        yield return StartCoroutine(PrintText(text));

        level1AwaitingID = true;
        level1SequenceRunning = false;
        canType = true;
    }

    IEnumerator Level1_AfterAnyIDAttempt_ShowError_AndGoLevel2(string attempt)
    {
        level1SequenceRunning = true;
        canType = false;
        level1AwaitingID = false;

        ResetInputText();
        yield return StartCoroutine(PrintText($"ПРИНЯТО: {attempt}\nОЖИДАНИЕ ПОДТВЕРЖДЕНИЯ..."));
        yield return new WaitForSeconds(2.5f);

        ResetInputText();
        yield return StartCoroutine(PrintText("ОШИБКА: ПОДТВЕРЖДЕНИЕ НЕ ПОЛУЧЕНО"));
        yield return new WaitForSeconds(1.0f);

        ResetInputText();
        yield return StartCoroutine(PrintText("ВАША СТАНЦИЯ НЕ ОТВЕЧАЕТ НА АВТОЗАПРОС"));
        yield return new WaitForSeconds(3.0f);

        if (level2TransitionSound != null && audioSource != null)
            audioSource.PlayOneShot(level2TransitionSound);

        if (backgroundImage != null && level2BackgroundSprite != null)
            backgroundImage.sprite = level2BackgroundSprite;

        if (level2Effects != null)
            level2Effects.SetActive(true);

        currentLevelIndex = 1;
        ResetInputText();

        if (terminalText != null)
            terminalText.text =
                "МЫ ПОТЕРЯЛИ С ВАМИ КОНТАКТ 18 ЧАСОВ НАЗАД...\n" +
                "СООБЩИТЕ СОСТОЯНИЕ СИСТЕМ\n\n" +
                "[ ВВЕДИТЕ: AIR ]";

        level1SequenceRunning = false;
        canType = true;
    }

    IEnumerator CompleteLevel2()
    {
        canType = false;
        yield return new WaitForSeconds(1f);
        ResetInputText();

        yield return StartCoroutine(PrintText(
            "ДАТЧИКИ ПОКАЗЫВАЮТ РАЗГЕРМЕТИЗАЦИЮ\n" +
            "В ЭКИПАЖЕ 4 ЧЕЛОВЕКА\n" +
            "ПОДТВЕРДИТЕ СОСТОЯНИЕ ЭКИПАЖА\n...\nПРИНЯТО"
        ));
        yield return new WaitForSeconds(3f);

        currentLevelIndex = 2;

        if (crackSound != null)
        {
            if (sfxSource != null) sfxSource.PlayOneShot(crackSound);
            else if (audioSource != null) audioSource.PlayOneShot(crackSound);
        }

        if (preTextPause > 0f) yield return new WaitForSeconds(preTextPause);

        if (terminalText != null)
            terminalText.text =
                "КРИТИЧЕСКАЯ НЕХВАТКА КИСЛОРОДА!\n" +
                "ОТКРОЙТЕ АВАРИЙНЫЙ ШЛЮЗ ВРУЧНУЮ!\n\n" +
                "[ КОД: OPEN ]";

        timeLeft = level3Time;
        isTimerRunning = true;

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.color = Color.white;
        }

        lastGlitchTime = -999f;
        canType = true;
    }

    IEnumerator CompleteLevel3()
    {
        canType = false;
        isTimerRunning = false;

        if (timerText != null) timerText.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.6f);
        ResetInputText();

        yield return StartCoroutine(PrintText(
            "ШЛЮЗ ОТКРЫТ\n" +
            "ДАВЛЕНИЕ ВЫРАВНИВАЕТСЯ...\n\n" +
            "НО...\n\n" +
            "ЯДРО СТАНЦИИ НА ГРАНИ СРЫВА"
        ));
        yield return new WaitForSeconds(1.2f);

        StartCoroutine(StartLevel5Choice());
    }

    // ====== постраничный вывод (с особой паузой на "...") ======
    IEnumerator PrintPages(string[] pages, float defaultPauseBetween)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            ResetInputText();
            yield return StartCoroutine(PrintText(pages[i]));

            float pause = defaultPauseBetween;

            // если страница ровно "..." -> делаем 6 секунд
            if (pages[i] != null && pages[i].Trim() == "...")
                pause = ellipsisPause;

            yield return new WaitForSeconds(pause);
        }
    }

    // ====== Level 5 Choice (без AUTO) ======
    IEnumerator StartLevel5Choice()
    {
        level5SequenceRunning = true;
        canType = false;
        level5ChoiceActive = false;

        ResetInputText();

        string[] pages =
        {
            "СТЫКОВКА ВОЗМОЖНА.\nНО МЫ МОЖЕМ СПАСТИ ТОЛЬКО ОДНО.",
            "ЕСЛИ МЫ ЗАБЕРЕМ ВАС —\nЯДРО МОЖЕТ СОРВАТЬСЯ.",
            "ЕСЛИ МЫ СТАБИЛИЗИРУЕМ ЯДРО —\nВЫ МОЖЕТЕ НЕ ДОЖИТЬ ДО ЭВАКУАЦИИ.",
            "ПЕРЕДАЙТЕ ПРИОРИТЕТ:"
        };

        yield return StartCoroutine(PrintPages(pages, level5PagePause));
        yield return new WaitForSeconds(level5FinalPause);

        ResetInputText();
        yield return StartCoroutine(PrintText(
            "ВАРИАНТЫ:\n\n" +
            "ME   — ЭВАКУАЦИЯ ЧЕЛОВЕКА\n" +
            "CORE — СТАБИЛИЗАЦИЯ ЯДРА"
        ));

        level5ChoiceActive = true;
        level5SequenceRunning = false;
        canType = true;
    }

    // ====== Ending: тоже по страницам с БОЛЬШИМИ паузами ======
    IEnumerator Level5_PlayEnding(string choice)
{
    level5SequenceRunning = true;
    canType = false;
    level5ChoiceActive = false;

    ResetInputText();

    // ===================== ME =====================
    if (choice == "ME")
    {
        string[] endPages =
        {
            "ПРИНЯТО: ME\nПРИОРИТЕТ: ЭВАКУАЦИЯ",
            "ЗАХВАТ ЦЕЛИ...",
            "СТЫКОВКА...",
            "ВЫ НА БОРТУ.",
            "...",
            "ЧЕРЕЗ 3 МЕСЯЦА:\nЯДРО СТАНЦИИ СОРВАЛОСЬ.",
            "СТАНЦИЯ БЫЛА ПОЛНОСТЬЮ УНИЧТОЖЕНА.",
            "РАССЛЕДОВАНИЕ ПРОДЛИЛОСЬ 2 ГОДА.",
            "ВАШИ ДЕЙСТВИЯ ПРИЗНАНЫ ЕДИНСТВЕННО ВОЗМОЖНЫМИ.",

            // ЭПИЛОГ
            "...",
            "ЧЕРЕЗ НЕСКОЛЬКО ЛЕТ:",
            "ВЫ РАБОТАЕТЕ В ЦЕНТРЕ ПОДГОТОВКИ ПИЛОТОВ.",
            "КАЖДЫЙ КУРСАНТ ИЗУЧАЕТ ТУ АВАРИЮ.",
            "НО НИКТО НЕ СПРАШИВАЕТ,\nКАКОВО ЭТО — ОСТАТЬСЯ ПОСЛЕДНИМ."
        };

        yield return StartCoroutine(PrintPages(endPages, endingPagePause));
    }

    // ===================== CORE =====================
    else
    {
        string[] endPages =
        {
            "ПРИНЯТО: CORE\nПРИОРИТЕТ: ЯДРО",
            "ПОДКЛЮЧЕНИЕ К СЕРДЦУ СТАНЦИИ...",
            "НАЧИНАЕМ СТАБИЛИЗАЦИЮ...",
            "ПОТОКИ ВЫРАВНИВАЮТСЯ...",
            "...",
            "ЯДРО УДЕРЖАНО.",
            "СТАНЦИЯ СПАСЕНА.",
            "НО СВЯЗЬ С ВАМИ ПРЕРВАЛАСЬ.",

            // ЭПИЛОГ
            "...",
            "ЧЕРЕЗ НЕСКОЛЬКО ЛЕТ:",
            "СТАНЦИЯ СТАЛА УЗЛОМ НОВОЙ СЕТИ КОЛОНИЙ.",
            "МИЛЛИОНЫ ЖИЗНЕЙ ЗАВИСЯТ ОТ НЕЕ.",
            "ВАШЕ ИМЯ НИГДЕ НЕ УКАЗАНО.",
            "НО СИГНАЛ SOS ВСЕ ЕЩЕ ХРАНИТСЯ В АРХИВЕ."
        };

        yield return StartCoroutine(PrintPages(endPages, endingPagePause));
    }
}

    void GameOver()
    {
        isTimerRunning = false;
        canType = false;

        if (terminalText != null)
            terminalText.text = "СИСТЕМА ЖИЗНЕОБЕСПЕЧЕНИЯ ОТКЛЮЧЕНА...\n\nСВЯЗЬ ПОТЕРЯНА.";

        if (resultText != null) resultText.text = "";
        if (currentSymbolsText != null) currentSymbolsText.text = "";
    }

    IEnumerator PrintText(string textToPrint)
    {
        if (terminalText == null) yield break;

        terminalText.text = "";
        foreach (char letter in textToPrint)
        {
            terminalText.text += letter;
            yield return new WaitForSeconds(textPrintSpeed);
        }
    }

    private void ResetInputText()
    {
        translatedWord = "";
        currentLetterCode = "";
        lastTranslatedLetterTime = -999f;

        if (resultText != null) resultText.text = "";
        if (currentSymbolsText != null) currentSymbolsText.text = "";
    }

    public void DeleteLastLetter()
    {
        if (!canType) return;
        if (hintOpen) return;

        if (translatedWord.Length > 0)
        {
            translatedWord = translatedWord.Substring(0, translatedWord.Length - 1);
            if (resultText != null) resultText.text = translatedWord;
        }
    }

    public void ClearAllInput()
    {
        if (hintOpen) return;

        translatedWord = "";
        currentLetterCode = "";

        if (resultText != null) resultText.text = "";
        if (currentSymbolsText != null) currentSymbolsText.text = "";

        letterProcessed = true;
        lastTranslatedLetterTime = -999f;
    }

    private void OpenHint()
    {
        if (hintPanel == null) return;
        if (hintOpen) return;

        hintOpen = true;
        hintPanel.SetActive(true);

        if (terminalText != null) terminalText.gameObject.SetActive(false);
        if (resultText != null) resultText.gameObject.SetActive(false);
        if (currentSymbolsText != null) currentSymbolsText.gameObject.SetActive(false);

        currentLetterCode = "";
        if (currentSymbolsText != null) currentSymbolsText.text = "";
        letterProcessed = true;
    }

    private void CloseHint()
    {
        if (hintPanel == null) return;
        if (!hintOpen) return;

        hintOpen = false;
        hintPanel.SetActive(false);

        if (terminalText != null) terminalText.gameObject.SetActive(true);
        if (resultText != null) resultText.gameObject.SetActive(true);
        if (currentSymbolsText != null) currentSymbolsText.gameObject.SetActive(true);
    }

    private void UpdateHoldBar()
    {
        if (holdBarFill == null) return;

        if (!isPressing || hintOpen)
        {
            holdBarFill.fillAmount = 0f;
            return;
        }

        float held = Time.time - pressTime;
        float progress = Mathf.Clamp01(held / clearAllHold);
        holdBarFill.fillAmount = progress;

        if (held < dotMax) holdBarFill.color = dotColor;
        else if (held < deleteHold) holdBarFill.color = dashColor;
        else if (held < hintHold) holdBarFill.color = deleteColor;
        else if (held < clearAllHold) holdBarFill.color = hintColor;
        else holdBarFill.color = clearColor;
    }
}