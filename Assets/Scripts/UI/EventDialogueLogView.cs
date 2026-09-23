using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Right-side debug panel showing EventDialogueLog's unified log of world
// events interleaved with actual NPC dialogue lines, newest entry first, so
// an event's effect on the next line(s) of dialogue is visible at a glance.
// Lives inside a ScrollRect (_content is the scrollable Content
// RectTransform) since the log can exceed the panel's height - _content's
// height is resized to _contentText's preferred height every refresh so the
// scrollbar/drag range always matches the actual content. Shares the F3
// toggle with DebugViewPlaceholder (the left-side cache/context panel) -
// these are treated as one combined "debug view".
public class EventDialogueLogView : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private Key _toggleKey = Key.F3;
    [SerializeField] private float _refreshInterval = 0.2f;
    [SerializeField] private Text _contentText;
    [SerializeField] private RectTransform _content;

    private float _nextRefreshAt;
    private readonly StringBuilder _sb = new StringBuilder();

    private void Awake()
    {
        if (_panel != null)
            _panel.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current == null || _panel == null)
            return;

        if (Keyboard.current[_toggleKey].wasPressedThisFrame)
        {
            bool visible = !_panel.activeSelf;
            _panel.SetActive(visible);
            if (visible)
                Refresh();
        }

        if (_panel.activeSelf && Time.time >= _nextRefreshAt)
        {
            _nextRefreshAt = Time.time + _refreshInterval;
            Refresh();
        }
    }

    private void Refresh()
    {
        if (_contentText == null)
            return;

        _sb.Clear();
        _sb.Append("=== イベント & セリフ履歴（新しい順） ===\n");

        var entries = EventDialogueLog.Entries;
        if (entries.Count == 0)
        {
            _sb.Append("(まだ何も記録されていません)\n");
        }
        else
        {
            // 新しいものが先頭に来るよう逆順で並べる（EventDialogueLog自体は古い→新しい順で保持）。
            for (int i = entries.Count - 1; i >= 0; i--)
                _sb.Append(entries[i]).Append('\n');
        }

        _contentText.text = _sb.ToString();

        if (_content != null)
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, _contentText.preferredHeight);
    }
}
