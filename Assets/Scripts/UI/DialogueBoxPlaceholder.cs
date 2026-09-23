using UnityEngine;
using UnityEngine.UI;

// Placeholder dialogue box. Shows a fixed line for a few seconds when an NPC
// is talked to (see InteractPrompt). Once GossipNet is wired in, callers
// should pass the line ActionDispatcher hands them instead of a
// hand-authored fixed string - this box's job (display + auto-hide) doesn't
// need to change.
//
// Every shown line is also appended to EventDialogueLog (screen-display-only,
// not fed back into GossipNet's context/prompt), which EventDialogueLogView
// renders in the right-side F3 panel alongside world events.
public class DialogueBoxPlaceholder : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private Text _text;
    [SerializeField] private float _displaySeconds = 3f;

    private float _hideAt;

    private void Awake()
    {
        if (_panel != null)
            _panel.SetActive(false);
    }

    public void ShowLine(string speakerName, string line)
    {
        EventDialogueLog.LogDialogue(speakerName, line);

        if (_panel == null || _text == null)
            return;

        _panel.SetActive(true);
        _text.text = $"{speakerName}: {line}";
        _hideAt = Time.time + _displaySeconds;
    }

    private void Update()
    {
        if (_panel != null && _panel.activeSelf && Time.time >= _hideAt)
            _panel.SetActive(false);
    }
}
