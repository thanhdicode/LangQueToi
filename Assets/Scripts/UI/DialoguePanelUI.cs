// ──────────────────────────────────────────────
// TheSprouty | UI/DialoguePanelUI.cs
// Manages showing and hiding the dialogue panel.
// Typewriter effect with Space/LMB-to-skip support.
// Attach directly on the DialoguePanel GameObject.
// ──────────────────────────────────────────────
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class DialoguePanelUI : MonoBehaviour
{
    // ----------------------------------------------------------
    // Singleton
    // ----------------------------------------------------------
    public static DialoguePanelUI Instance { get; private set; }

    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [SerializeField] private PlayerIndicator playerIndicator;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject continueIndicator;
    [SerializeField] private GameObject choicesContainer;

    [Header("Typewriter")]
    [SerializeField] private float typingSpeed = 0.04f;
    [SerializeField] private SoundEventSO typingSound;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private DialogueAnimator _dialogueAnimator;
    private Coroutine _typingCoroutine;
    private string _currentText;
    private bool _isOpen;

    // ----------------------------------------------------------
    // Properties
    // ----------------------------------------------------------
    public bool IsTyping => _typingCoroutine != null;
    public bool IsOpen   => _isOpen;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _dialogueAnimator = GetComponent<DialogueAnimator>();
    }

    private void Update()
    {
        if (!_isOpen) return;

        bool skipPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
        if (!skipPressed) return;

        if (IsTyping)
            SkipTyping();
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    /// <summary>Opens the dialogue panel with animation, freezes player movement and hides indicator.</summary>
    public void Open()
    {
        _isOpen = true;
        Player.Instance.EnterDialogue();
        playerIndicator.Hide();
        _dialogueAnimator.Show(onComplete: () =>
            PlayDialogue("What a lovely day to stop by! The shelves are fresh. See anything you fancy?"));
    }

    /// <summary>Closes the dialogue panel with animation, restores player movement and shows indicator.
    /// Wire to Goodbye button onClick. onComplete fires after hide animation finishes.</summary>
    /// <summary>Parameterless overload for wiring to Inspector onClick buttons.</summary>
    public void Close() => Close(onComplete: null);

    public void Close(Action onComplete)
    {
        StopAllCoroutines();
        _typingCoroutine = null;
        dialogueText.text = "";
        continueIndicator.SetActive(false);
        choicesContainer.SetActive(false);
        Player.Instance.ExitDialogue();
        playerIndicator.Show();
        _dialogueAnimator.Hide(onComplete: () =>
        {
            _isOpen = false;
            onComplete?.Invoke();
        });
    }

    /// <summary>Starts typewriter effect for the given text.</summary>
    public void PlayDialogue(string text)
    {
        _currentText = text;
        continueIndicator.SetActive(false);
        choicesContainer.SetActive(false);
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(TypeRoutine());
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private IEnumerator TypeRoutine()
    {
        continueIndicator.SetActive(false);
        choicesContainer.SetActive(false);
        dialogueText.text = "";
        foreach (char c in _currentText)
        {
            dialogueText.text += c;
            if (c != ' ') typingSound?.Play();
            yield return new WaitForSeconds(typingSpeed);
        }
        _typingCoroutine = null;
        continueIndicator.SetActive(true);
        choicesContainer.SetActive(true);
    }

    private void SkipTyping()
    {
        StopCoroutine(_typingCoroutine);
        _typingCoroutine = null;
        dialogueText.text = _currentText;
        continueIndicator.SetActive(true);
        choicesContainer.SetActive(true);
    }
}
