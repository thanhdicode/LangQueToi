using UnityEngine;

namespace LangQueToi
{
    /// <summary>
    /// Immutable presentation data for a single dialogue turn: who is speaking,
    /// their portrait, and what they say. Consumed by DialoguePanelUI.Open overload.
    /// </summary>
    public readonly struct DialoguePresentation
    {
        public DialoguePresentation(string speakerName, Sprite portrait, string text)
        {
            SpeakerName = speakerName ?? string.Empty;
            Portrait = portrait;
            Text = text ?? string.Empty;
        }

        public string SpeakerName { get; }
        public Sprite Portrait { get; }
        public string Text { get; }
    }
}
