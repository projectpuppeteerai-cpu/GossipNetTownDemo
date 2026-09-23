using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using AINPCCoreEngine;

// Real-time cache/context debug view (OVERVIEW.md §7.1 GossipNetDebugUI).
// Toggles a text panel with F3, showing live AsyncCachePipeline cache state
// per registered NPC and ContextManager's current player context - a
// simplified, single-Text version of the original demo's GossipNetDebugUI.
//
// Also shows each major NPC's latest JEV state (OVERVIEW.md §16.3 line
// format) and, while the panel is open, lets _clearJevKey clear JEV state
// for every major NPC - JEV has no auto-expiry by design (§16.5), so this
// is the debug-only manual escape hatch decided for TownDemo (§16.7).
//
// _resetAllKey does a full demo reset (JEV + ContextManager history +
// EventDialogueLog + every one-shot world trigger's _taken/_broken/etc
// flag and visuals), so the whole scenario can be replayed without
// restarting Play mode.
public class DebugViewPlaceholder : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private Key _toggleKey = Key.F3;
    [SerializeField] private Key _clearJevKey = Key.C;
    [SerializeField] private Key _resetAllKey = Key.R;
    [SerializeField] private float _refreshInterval = 0.2f;
    [SerializeField] private Text _contentText;
    [SerializeField] private RectTransform _content;

    private AsyncCachePipeline _pipeline;
    private float _nextRefreshAt;
    private readonly StringBuilder _sb = new StringBuilder();

    private void Awake()
    {
        if (_panel != null)
            _panel.SetActive(false);
        _pipeline = FindFirstObjectByType<AsyncCachePipeline>();
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

        if (_panel.activeSelf && Keyboard.current[_clearJevKey].wasPressedThisFrame)
        {
            ClearAllMajorJevState();
            Refresh();
        }

        if (_panel.activeSelf && Keyboard.current[_resetAllKey].wasPressedThisFrame)
        {
            ResetEverything();
            Refresh();
        }

        if (_panel.activeSelf && Time.time >= _nextRefreshAt)
        {
            _nextRefreshAt = Time.time + _refreshInterval;
            Refresh();
        }
    }

    private void ClearAllMajorJevState()
    {
        if (_pipeline == null || ContextManager.Instance == null)
            return;

        foreach (var entry in _pipeline.GetDebugSnapshot())
        {
            if (entry.Tier == NpcTier.Major)
                ContextManager.Instance.ClearNpcJevState(entry.NpcId);
        }
    }

    // デモ全体を初期状態に戻す。JEV・行動履歴/所持品/WorldFlags・イベント&セリフ履歴表示に加え、
    // 壺/カゴ/装備/強盗/救助対象など一度きりのワールドトリガーも再発火できる状態に戻し、
    // Playモードを再起動しなくてもシナリオを繰り返し試せるようにする。
    private void ResetEverything()
    {
        if (ContextManager.Instance != null)
        {
            ContextManager.Instance.ClearAllJevState();
            ContextManager.Instance.ResetHistory();
        }

        EventDialogueLog.Clear();

        foreach (var pot in FindObjectsByType<BreakablePot>(FindObjectsSortMode.None))
            pot.ResetTrigger();
        foreach (var crate in FindObjectsByType<StealableCrate>(FindObjectsSortMode.None))
            crate.ResetTrigger();
        foreach (var villager in FindObjectsByType<HelpableVillager>(FindObjectsSortMode.None))
            villager.ResetTrigger();
        foreach (var equipment in FindObjectsByType<EquipmentPickup>(FindObjectsSortMode.None))
            equipment.ResetTrigger();
        foreach (var robbery in FindObjectsByType<RobberyIncidentTrigger>(FindObjectsSortMode.None))
            robbery.ResetTrigger();

        if (_pipeline != null)
            _ = _pipeline.RefreshAllCachesManuallyAsync();
    }

    private void Refresh()
    {
        if (_contentText == null)
            return;

        _sb.Clear();
        _sb.Append("GossipNet v").Append(GossipNetVersion.Version).Append('\n');
        _sb.Append("=== NPCキャッシュテーブル ===\n");

        if (_pipeline == null)
        {
            _sb.Append("(AsyncCachePipelineが見つかりません)\n");
        }
        else
        {
            foreach (var entry in _pipeline.GetDebugSnapshot())
            {
                string badge = entry.IsInFlight ? "[生成中]" : entry.CachedCount > 0 ? "[OK]" : "[MISS]";
                _sb.Append(badge).Append(' ').Append(entry.NpcId)
                    .Append(" (").Append(entry.Tier).Append(") 件数:")
                    .Append(entry.CachedCount).Append('/').Append(entry.VariationCacheSize)
                    .Append(" Hit:").Append(entry.HitCount).Append(" Miss:").Append(entry.MissCount)
                    .Append('\n');

                if (entry.LatestJevState != null && entry.LatestJevState.event_perceived)
                {
                    var jev = entry.LatestJevState;
                    _sb.Append("    JEV: ").Append(jev.emotion_state)
                        .Append(" / threat=").Append(jev.threat_level.ToString("0.00"));
                    if (!string.IsNullOrEmpty(jev.perceived_intent))
                        _sb.Append(" / intent=").Append(jev.perceived_intent);
                    if (!string.IsNullOrEmpty(jev.reaction_category))
                        _sb.Append(" / reaction=").Append(jev.reaction_category);
                    if (jev.is_uncertain)
                        _sb.Append(" / uncertain");
                    _sb.Append('\n');
                }

                // 直近生成バッチが実際にLLMへ送ったプロンプト。JEVの反映有無をその場で目視確認できる
                // ようにする（このセリフがどのJEV状態を踏まえて生成されたか、後追いで分かるように）。
                if (!string.IsNullOrEmpty(entry.LatestPrompt))
                {
                    _sb.Append("    [生成日時: ")
                        .Append(System.DateTimeOffset.FromUnixTimeMilliseconds(entry.LatestGeneratedAtUnixMs).ToLocalTime().ToString("HH:mm:ss"))
                        .Append("]\n");
                    _sb.Append("    Prompt: ").Append(entry.LatestPrompt).Append('\n');
                }
            }
        }

        _sb.Append("\n=== プレイヤーコンテキスト ===\n");
        if (ContextManager.Instance == null)
        {
            _sb.Append("(ContextManagerが見つかりません)\n");
        }
        else
        {
            var ctx = ContextManager.Instance.CurrentContext;
            _sb.Append("Location: ")
                .Append(string.IsNullOrEmpty(ctx.CurrentLocationTag) ? "(未設定)" : ctx.CurrentLocationTag)
                .Append('\n');

            _sb.Append("Notable Actions(").Append(ctx.RecentActions.Count).Append("): ");
            for (int i = 0; i < ctx.RecentActions.Count; i++)
            {
                if (i > 0) _sb.Append(" / ");
                _sb.Append(ctx.RecentActions[i].ActionType).Append('→').Append(ctx.RecentActions[i].Target);
            }

            _sb.Append("\nTalk History(").Append(ctx.RecentTalks.Count).Append("): ");
            for (int i = 0; i < ctx.RecentTalks.Count; i++)
            {
                if (i > 0) _sb.Append(" / ");
                _sb.Append(ctx.RecentTalks[i].Target);
            }
        }

        _sb.Append("\n\nF3で表示/非表示 / Cで主要NPC全員のJEVをクリア / Rで全部リセット / 右側パネルにイベント&セリフ履歴");
        _contentText.text = _sb.ToString();

        if (_content != null)
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, _contentText.preferredHeight);
    }
}
