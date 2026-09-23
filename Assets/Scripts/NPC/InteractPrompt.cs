using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using AINPCCoreEngine;

// Proximity-interact behaviour for a GossipNet-backed NPC. Tints the NPC
// sprite by NpcStateWatcher's conversation state (red=Unprepared,
// yellow=Updating, green=Ready - OVERVIEW.md §10.1's traffic-light colors,
// reused directly on the sprite instead of a floating orb since TownDemo
// has no SampleNpcStateLight) and brightens it further while the player is
// in range. On Space, talks to the NPC via GossipNet (cache hit → instant,
// cache miss → live LLM call) and shows the result via DialogueBoxPlaceholder.
// On _attackKey, records a "町民を攻撃した" context event via
// TownActionTriggers (OVERVIEW.md §16 additional event triggers) instead of
// talking, flashing the sprite briefly for feedback.
//
// _fixedLine is passed through as GossipNet's fallbackDialogueOverride
// (OVERVIEW.md §14.7), so it's what actually gets shown if the LLM call
// fails or no API key is configured - the "existing authored dialogue" case
// this line was originally a stand-in for.
[RequireComponent(typeof(NpcPlaceholder))]
public class InteractPrompt : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Color _unpreparedColor = new Color(0.85f, 0.3f, 0.3f);
    [SerializeField] private Color _updatingColor = new Color(0.9f, 0.85f, 0.3f);
    [SerializeField] private Color _readyColor = new Color(0.4f, 0.85f, 0.4f);
    [SerializeField] private float _inRangeBrightness = 0.4f; // 0=state color as-is, 1=white
    [SerializeField] private string _displayName = "NPC";
    [SerializeField, TextArea] private string _fixedLine = "...";
    [SerializeField] private Key _attackKey = Key.X;
    [SerializeField] private Color _hitFlashColor = Color.red;
    [SerializeField] private float _hitFlashDuration = 0.15f;

    private NpcPlaceholder _npc;
    private DialogueBoxPlaceholder _dialogueBox;
    private AsyncCachePipeline _pipeline;
    private LLMBridge _llmBridge;
    private ActionDispatcher _dispatcher;
    private NpcRegistry _npcRegistry;
    private NpcStateWatcher _stateWatcher;
    private TownActionTriggers _actionTriggers;
    private Color _baseColor;
    private bool _playerInRange;

    // Shared across all InteractPrompt instances so ContextManager.UpdateNearbyNpcs
    // (which replaces the whole list) always reflects everyone currently in range,
    // not just the last NPC the player touched.
    private static readonly HashSet<string> s_nearbyNpcIds = new HashSet<string>();

    private void Awake()
    {
        _npc = GetComponent<NpcPlaceholder>();
        _dialogueBox = FindFirstObjectByType<DialogueBoxPlaceholder>();
        _pipeline = FindFirstObjectByType<AsyncCachePipeline>();
        _llmBridge = FindFirstObjectByType<LLMBridge>();
        _dispatcher = FindFirstObjectByType<ActionDispatcher>();
        _npcRegistry = FindFirstObjectByType<NpcRegistry>();
        _actionTriggers = FindFirstObjectByType<TownActionTriggers>();
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        _stateWatcher = GetComponent<NpcStateWatcher>();
        if (_stateWatcher == null)
            _stateWatcher = gameObject.AddComponent<NpcStateWatcher>();
        _stateWatcher.SetNpcId(_npc.NpcId);

        _baseColor = _unpreparedColor;
    }

    private void OnEnable()
    {
        if (_dispatcher != null)
            _dispatcher.OnDialogueReady += HandleDialogueReady;
        if (_stateWatcher != null)
            _stateWatcher.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (_dispatcher != null)
            _dispatcher.OnDialogueReady -= HandleDialogueReady;
        if (_stateWatcher != null)
            _stateWatcher.OnStateChanged -= HandleStateChanged;
    }

    private void HandleDialogueReady(string npcId, string dialogue, string emotion)
    {
        if (npcId != _npc.NpcId || _dialogueBox == null)
            return;

        _dialogueBox.ShowLine(_displayName, dialogue);
    }

    private void HandleStateChanged(NpcConversationState state)
    {
        _baseColor = state switch
        {
            NpcConversationState.Ready => _readyColor,
            NpcConversationState.Updating => _updatingColor,
            _ => _unpreparedColor,
        };
        ApplySpriteColor();
    }

    private void ApplySpriteColor()
    {
        if (_spriteRenderer == null)
            return;

        _spriteRenderer.color = _playerInRange
            ? Color.Lerp(_baseColor, Color.white, _inRangeBrightness)
            : _baseColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _playerInRange = true;
        ApplySpriteColor();

        // 主要NPCの自動プリフェッチ（AsyncCachePipeline.HandleContextUpdated、
        // OVERVIEW.md §10.5）はNearbyNpcIdsを見て判断するため、近づいたタイミングで
        // 反映する。プレイヤーが近づいてから話しかけるまでの間にReadyへ遷移する、
        // という頭上ライトの設計通りの体験になる。
        s_nearbyNpcIds.Add(_npc.NpcId);
        ContextManager.Instance?.UpdateNearbyNpcs(new List<string>(s_nearbyNpcIds));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _playerInRange = false;
        ApplySpriteColor();

        s_nearbyNpcIds.Remove(_npc.NpcId);
        ContextManager.Instance?.UpdateNearbyNpcs(new List<string>(s_nearbyNpcIds));
    }

    private void Update()
    {
        if (!_playerInRange || Keyboard.current == null)
            return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            TalkTo();

        if (Keyboard.current[_attackKey].wasPressedThisFrame)
            Attack();
    }

    private void Attack()
    {
        if (_actionTriggers == null)
        {
            Debug.LogWarning($"[InteractPrompt:{_npc.NpcId}] TownActionTriggers not found in scene; attack ignored.");
            return;
        }

        _actionTriggers.AttackVillager(_npc.NpcId, transform.position);
        StartCoroutine(FlashHit());
    }

    private IEnumerator FlashHit()
    {
        if (_spriteRenderer == null)
            yield break;

        _spriteRenderer.color = _hitFlashColor;
        yield return new WaitForSeconds(_hitFlashDuration);
        ApplySpriteColor();
    }

    private async void TalkTo()
    {
        if (_pipeline == null || _llmBridge == null || _dispatcher == null || _npcRegistry == null)
        {
            Debug.LogWarning($"[InteractPrompt:{_npc.NpcId}] GossipNetManager not found in scene; showing fixed line only.");
            if (_dialogueBox != null)
                _dialogueBox.ShowLine(_displayName, _fixedLine);
            return;
        }

        // 会話準備状態がReady（緑）でない場合はトリガーを無視する（OVERVIEW.md §10.4）。
        if (_pipeline.GetConversationState(_npc.NpcId) != NpcConversationState.Ready)
        {
            Debug.Log($"[InteractPrompt:{_npc.NpcId}] 会話準備中のため話しかけトリガーを無視しました。");
            return;
        }

        // 先にキャッシュ消費/生成を行い、その後で「話しかけた」をコンテキストに記録する
        // （OVERVIEW.md §7.9: 逆順だとコンテキストハッシュが変わってしまう）。
        var cached = _pipeline.ConsumeCache(_npc.NpcId);
        if (cached != null)
        {
            _dispatcher.Dispatch(cached.Response);
        }
        else
        {
            var persona = _npcRegistry.Resolve(_npc.NpcId);
            if (persona == null)
            {
                Debug.LogWarning($"[InteractPrompt] Persona未登録: {_npc.NpcId}");
                return;
            }

            await _llmBridge.GenerateAndDispatchImmediateAsync(persona, ContextManager.Instance.CurrentContext, _fixedLine);
        }

        ContextManager.Instance.RecordAction(ContextManager.TalkActionType, _npc.NpcId);
    }
}
