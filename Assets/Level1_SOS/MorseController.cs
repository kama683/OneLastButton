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

    [Header("Настройки")]
    public AudioSource audioSource;
    public float dashThreshold = 0.25f;
    public float letterPause = 0.6f;
    public float textPrintSpeed = 0.05f;

    private string[] levels = { "SOS", "AIR", "H2O", "OPEN" };
    private int currentLevelIndex = 0;

    private string currentLetterCode = "";
    private string translatedWord = "";
    private float lastReleaseTime;
    private bool isPressing = false;
    private float pressTime;
    private bool letterProcessed = true;
    private bool canType = false;

    private string level1Intro = "...\n... --- ...\nПРИЕМ?\nЕСЛИ КТО-ТО ЖИВ — ОТВЕТЬТЕ\n\n[ ВВЕДИТЕ: SOS ]";
    private string level1Outro = "СИГНАЛ ПОЛУЧЕН\nИДЕНТИФИЦИРУЙТЕ СЕБЯ\n...\nВАША СТАНЦИЯ НЕ ОТВЕЧАЕТ НА АВТОЗАПРОС";

    private Dictionary<string, char> morseDict = new Dictionary<string, char>() {
        {".-", 'A'}, {"-...", 'B'}, {"-.-.", 'C'}, {"-..", 'D'}, {".", 'E'},
        {"..-.", 'F'}, {"--.", 'G'}, {"....", 'H'}, {"..", 'I'}, {".---", 'J'},
        {"-.-", 'K'}, {".-..", 'L'}, {"--", 'M'}, {"-.", 'N'}, {"---", 'O'},
        {".--.", 'P'}, {"--.-", 'Q'}, {".-.", 'R'}, {"...", 'S'}, {"-", 'T'},
        {"..-", 'U'}, {"...-", 'V'}, {".--", 'W'}, {"-..-", 'X'}, {"-.--", 'Y'},
        {"--..", 'Z'}, {"-----", '0'}, {".----", '1'}, {"..---", '2'}, {"...--", '3'},
        {"....-", '4'}, {".....", '5'}, {"-....", '6'}, {"--...", '7'}, {"---..", '8'}, {"----.", '9'}
    };

    void Start()
    {
        if (hintPanel != null) hintPanel.SetActive(false);
        resultText.text = "";
        currentSymbolsText.text = "";

        StartCoroutine(StartLevel1());
    }

    void Update()
    {
        // В Update мы теперь только проверяем паузу для перевода буквы.
        // Сами клики обрабатываются в методах OnTransmitterDown и OnTransmitterUp
        if (!isPressing && !letterProcessed && (Time.time - lastReleaseTime > letterPause))
        {
            ProcessLetter();
        }
    }

    // --- ФУНКЦИИ ДЛЯ ЦЕНТРАЛЬНОЙ КНОПКИ ВВОДА ---

    // Вызывается, когда мы НАЖАЛИ на кнопку
    public void OnTransmitterDown()
    {
        if ((hintPanel != null && hintPanel.activeSelf) || !canType) return;

        pressTime = Time.time;
        isPressing = true;
        letterProcessed = false;
        if (audioSource) audioSource.Play();
    }

    // Вызывается, когда мы ОТПУСТИЛИ кнопку
    public void OnTransmitterUp()
    {
        if (!isPressing) return; // Защита от случайных срабатываний

        isPressing = false;
        float duration = Time.time - pressTime;
        currentLetterCode += (duration < dashThreshold) ? "." : "-";
        currentSymbolsText.text = currentLetterCode;
        lastReleaseTime = Time.time;
        if (audioSource) audioSource.Stop();
    }

    // --------------------------------------------

    void ProcessLetter()
    {
        letterProcessed = true;
        if (morseDict.ContainsKey(currentLetterCode))
        {
            translatedWord += morseDict[currentLetterCode];
            resultText.text = translatedWord;
            CheckWord();
        }
        currentLetterCode = "";
        currentSymbolsText.text = "";
    }

    void CheckWord()
    {
        if (translatedWord == levels[currentLevelIndex])
        {
            if (currentLevelIndex == 0)
            {
                StartCoroutine(CompleteLevel1());
            }
            else
            {
                terminalText.text = "ПРИНЯТО!";
            }
        }
    }

    IEnumerator StartLevel1()
    {
        canType = false;
        yield return StartCoroutine(PrintText(level1Intro));
        canType = true;
    }

    IEnumerator CompleteLevel1()
    {
        canType = false;
        yield return new WaitForSeconds(1f);

        resultText.text = "";
        translatedWord = "";

        yield return StartCoroutine(PrintText(level1Outro));
        yield return new WaitForSeconds(3f);

        currentLevelIndex++;
        terminalText.text = "МЫ ПОТЕРЯЛИ С ВАМИ КОНТАКТ 18 ЧАСОВ НАЗАД...\n[ ВВЕДИТЕ: AIR ]";
        canType = true;
    }

    IEnumerator PrintText(string textToPrint)
    {
        terminalText.text = "";
        foreach (char letter in textToPrint.ToCharArray())
        {
            terminalText.text += letter;
            yield return new WaitForSeconds(textPrintSpeed);
        }
    }

    public void DeleteLastLetter()
    {
        if (!canType || hintPanel.activeSelf) return;
        if (translatedWord.Length > 0)
        {
            translatedWord = translatedWord.Substring(0, translatedWord.Length - 1);
            resultText.text = translatedWord;
        }
    }

    // --- ОБНОВЛЕННАЯ ФУНКЦИЯ ПОДСКАЗКИ ---
    public void ToggleHint()
    {
        if (hintPanel != null)
        {
            bool isShowing = !hintPanel.activeSelf;

            // Включаем/выключаем панель
            hintPanel.SetActive(isShowing);

            // Прячем/показываем тексты (если у тебя они привязаны в инспекторе)
            if (terminalText != null) terminalText.gameObject.SetActive(!isShowing);
            if (resultText != null) resultText.gameObject.SetActive(!isShowing);
            if (currentSymbolsText != null) currentSymbolsText.gameObject.SetActive(!isShowing);
        }
    }
}