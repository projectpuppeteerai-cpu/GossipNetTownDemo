using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using AINPCCoreEngine;

// Context-event trigger entry point (OVERVIEW.md §14.1 "追加のコンテキスト
// トリガー"). Every world-object event (pot, crate, attack, help, trespass)
// routes through RecordWorldEventAsync, which: records the event to
// ContextManager, awaits JEV (OVERVIEW.md §16) for every major-tier NPC that
// perceives the event, and only then kicks off a manual cache refresh for
// every registered NPC (fire-and-forget, matching the "手動更新は呼び出す側の
// 裁量" convention - OVERVIEW.md §7.7; AsyncCachePipeline replays a refresh
// that arrives while one is already in flight per §15.6, so no game-side
// retry logic is needed for that part).
//
// JEV must be awaited BEFORE the cache refresh, not fired alongside it:
// PrefetchForNpcAsync reads ContextManager.GetNpcJevState(npcId) at the
// moment it runs, so if the refresh started before this event's JEV write
// landed, dialogue got generated against the previous (stale) JEV state -
// this was an actual observed bug (dialogue not reflecting the "heard only,
// uncertain" reaction JEV had just computed for the same event).
//
// Perception has two independent channels per major-tier NPC, each of which
// can make the NPC eligible for JEV on its own (NpcPlaceholder.FacingDirection
// is the fixed, Inspector-set "which way this NPC is facing" - not through
// Samples~/JevPerceptionPreFilter, since that Samples folder isn't imported
// into this project; TownDemo calls GossipNet.Runtime directly instead):
//   - Sight: within _visionRadius, within _visionConeAngleDegrees of the
//     NPC's facing direction, and not blocked by the "Obstacles" layer
//     (Physics2D.Linecast against the tree tilemap's TilemapCollider2D).
//   - Sound: within _soundRadius, omnidirectional, ignores obstacles and
//     facing (sound isn't modeled to be blocked by trees here).
// An NPC outside both radii isn't evaluated at all (JevEvaluator.EvaluateAsync
// is simply not called for it) - this is the "who does the event even reach"
// selection the perception system is meant to do. If only sound detects the
// event, JevEventContext.heard_only is set so the middleware's prompt can
// treat "heard but didn't see" differently from full visual confirmation.
public class TownActionTriggers : MonoBehaviour
{
    [SerializeField] private float _visionRadius = 10f;
    [SerializeField, Range(0f, 360f)] private float _visionConeAngleDegrees = 90f;
    [SerializeField] private float _soundRadius = 16f;
    [SerializeField] private LayerMask _obstacleLayerMask;

    private AsyncCachePipeline _pipeline;
    private JevEvaluator _jevEvaluator;

    private void Awake()
    {
        _pipeline = FindFirstObjectByType<AsyncCachePipeline>();
        _jevEvaluator = FindFirstObjectByType<JevEvaluator>();
        if (_obstacleLayerMask.value == 0)
            _obstacleLayerMask = LayerMask.GetMask("Obstacles");
    }

    public void BreakPot(Vector3 eventPosition) =>
        _ = RecordWorldEventAsync("壺をこわした", "町の壺", "pot_broken", eventPosition);

    public void StealItem(Vector3 eventPosition) =>
        _ = RecordWorldEventAsync("盗みを働いた", "商品の荷物カゴ", "theft", eventPosition);

    public void AttackVillager(string targetNpcId, Vector3 eventPosition) =>
        _ = RecordWorldEventAsync("町民を攻撃した", targetNpcId, "villager_attacked", eventPosition);

    public void HelpVillager(string targetDisplayName, Vector3 eventPosition) =>
        _ = RecordWorldEventAsync("困っている住民を助けた", targetDisplayName, "villager_helped", eventPosition);

    public void TrespassRestrictedArea(Vector3 eventPosition) =>
        _ = RecordWorldEventAsync("立入禁止区域に侵入した", "見張り場の奥", "trespassing", eventPosition);

    public void PickUpEquipment(string equipmentTag, Vector3 eventPosition)
    {
        if (ContextManager.Instance != null)
            ContextManager.Instance.AddInventoryTag(equipmentTag);

        _ = RecordWorldEventAsync("鋼の装備一式を手に入れた", equipmentTag, "equipment_picked_up", eventPosition);
    }

    // 他のイベントと違い、プレイヤー自身の行動ではなく「近くで起きている出来事を目撃する」
    // タイプのイベント。JEVの複数イベント合成（脅威の目撃→装備品の入手、で反応がどう
    // 変化するか）を試すために追加した。
    public void WitnessRobbery(Vector3 eventPosition) =>
        _ = RecordWorldEventAsync("強盗事件を目撃した", "路地裏の強盗", "robbery_witnessed", eventPosition);

    private async Task RecordWorldEventAsync(string actionType, string target, string jevEventType, Vector3 eventPosition)
    {
        EventDialogueLog.LogEvent($"{actionType}（対象: {target}）");

        if (ContextManager.Instance != null)
            ContextManager.Instance.RecordAction(actionType, target);

        // ContextManager.SetNpcJevStateへの書き込みがすべて完了してから次のキャッシュ再生成を
        // 呼ぶ（クラス冒頭のコメント参照。順序を保証しないとセリフが古いJEV状態を元に生成される）。
        await EvaluateJevForNearbyMajorsAsync(jevEventType, eventPosition);

        if (_pipeline != null)
            _ = _pipeline.RefreshAllCachesManuallyAsync();
        else
            Debug.LogWarning($"[TownActionTriggers] AsyncCachePipeline not found in scene; cache not refreshed. (event: {actionType})");
    }

    private async Task EvaluateJevForNearbyMajorsAsync(string eventType, Vector3 eventPosition)
    {
        if (_jevEvaluator == null)
            return;

        var pendingEvaluations = new List<Task<bool>>();

        foreach (var npc in FindObjectsByType<NpcPlaceholder>(FindObjectsSortMode.None))
        {
            if (npc.Tier != NpcTierPlaceholder.Major)
                continue;

            float distance = Vector3.Distance(npc.transform.position, eventPosition);

            bool sawIt = false;
            if (distance <= _visionRadius)
            {
                Vector2 toEvent = ((Vector2)eventPosition - (Vector2)npc.transform.position).normalized;
                float angle = Vector2.Angle(npc.FacingDirection, toEvent);
                bool inCone = angle <= _visionConeAngleDegrees * 0.5f;
                bool obstructed = Physics2D.Linecast(npc.transform.position, eventPosition, _obstacleLayerMask);
                sawIt = inCone && !obstructed;
            }

            bool heardIt = !sawIt && distance <= _soundRadius;

            if (!sawIt && !heardIt)
                continue; // 視覚・聴覚どちらの範囲にも入らないため、このNPCはそもそも知覚し得ない

            var eventContext = new JevEventContext
            {
                event_type = eventType,
                relative_distance_meters = distance,
                has_line_of_sight = sawIt,
                heard_only = heardIt,
            };
            pendingEvaluations.Add(_jevEvaluator.EvaluateAsync(npc.NpcId, eventContext));
        }

        if (pendingEvaluations.Count > 0)
            await Task.WhenAll(pendingEvaluations);
    }
}
