using System.Collections.Generic;
using System.Reflection;
using AINPCCoreEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-click recreation of the "GossipNetManager" GameObject + cross-wiring
// (OVERVIEW.md §6.1 pattern) for GossipNetTownDemo. Written after the manual
// Inspector setup was lost to Unity's "changes made in Play Mode don't
// persist" behavior. Sets private [SerializeField] fields via reflection so
// it works the same way SerializedObject-based Editor scripts (see
// TownDemoSceneSetup.cs) do, without needing to guess enum SerializedProperty
// indices.
public static class GossipNetManagerSetup
{
    [MenuItem("GossipNetTownDemo/Setup GossipNet Manager")]
    public static void Setup()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("[GossipNetManagerSetup] Playモード中は実行しないでください。Stopしてから実行してください（Play中の変更は保存されません）。");
            return;
        }

        var managerGo = GameObject.Find("GossipNetManager") ?? new GameObject("GossipNetManager");

        var contextManager = managerGo.GetComponent<ContextManager>() ?? managerGo.AddComponent<ContextManager>();
        var npcRegistry = managerGo.GetComponent<NpcRegistry>() ?? managerGo.AddComponent<NpcRegistry>();
        var llmBridge = managerGo.GetComponent<LLMBridge>() ?? managerGo.AddComponent<LLMBridge>();
        var dispatcher = managerGo.GetComponent<ActionDispatcher>() ?? managerGo.AddComponent<ActionDispatcher>();
        var pipeline = managerGo.GetComponent<AsyncCachePipeline>() ?? managerGo.AddComponent<AsyncCachePipeline>();
        var jevEvaluator = managerGo.GetComponent<JevEvaluator>() ?? managerGo.AddComponent<JevEvaluator>();
        if (managerGo.GetComponent<GossipNetBootstrap>() == null)
            managerGo.AddComponent<GossipNetBootstrap>();

        SetField(llmBridge, "_dispatcher", dispatcher);
        SetField(llmBridge, "_provider", LLMBridge.ApiProvider.Anthropic);
        SetField(llmBridge, "_model", "claude-haiku-4-5");

        SetField(pipeline, "_llmBridge", llmBridge);
        SetField(pipeline, "_npcRegistry", npcRegistry);

        SetField(jevEvaluator, "_llmBridge", llmBridge);
        SetField(jevEvaluator, "_npcRegistry", npcRegistry);
        SetField(pipeline, "_sharedMobPersona", new NpcPersona
        {
            NpcId = "mob_shared",
            Tier = NpcTier.Mob,
            PersonaPromptFragment = "名前を持たない町のモブ住人。誰であるかを前提にしない汎用的な口調で話す。",
            DefaultDialogue = "（今は特に話すことはないな。）",
        });

        SetField(npcRegistry, "_personas", new List<NpcPersona>
        {
            new NpcPersona
            {
                NpcId = "mob_a", Tier = NpcTier.Mob,
                PersonaPromptFragment = "村の八百屋の村人A。天気の話が好きな気さくな性格。",
                DefaultDialogue = "いらっしゃい、今日はいい天気だね。",
            },
            new NpcPersona
            {
                NpcId = "mob_b", Tier = NpcTier.Mob,
                PersonaPromptFragment = "村人B。噂話を耳にしがちな少し不安げな性格。",
                DefaultDialogue = "最近このあたりで物騒な噂を耳にするよ。",
            },
            new NpcPersona
            {
                NpcId = "major_guard", Tier = NpcTier.Major,
                PersonaPromptFragment = "町の見張り場に立つ衛兵。真面目で警戒心が強い。",
                DefaultDialogue = "ここは町の見張り場だ。何か困りごとか?",
            },
        });

        EditorUtility.SetDirty(contextManager);
        EditorUtility.SetDirty(npcRegistry);
        EditorUtility.SetDirty(llmBridge);
        EditorUtility.SetDirty(dispatcher);
        EditorUtility.SetDirty(pipeline);
        EditorUtility.SetDirty(jevEvaluator);
        EditorSceneManager.MarkSceneDirty(managerGo.scene);
        EditorSceneManager.SaveScene(managerGo.scene);

        Debug.Log("[GossipNetManagerSetup] GossipNetManager のセットアップ完了・シーン保存済み。" +
            "Api Key は引き続き gossipnet_config.json から読み込まれます（このスクリプトは触れません）。");
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            Debug.LogError($"[GossipNetManagerSetup] Field not found: {target.GetType().Name}.{fieldName}");
            return;
        }
        field.SetValue(target, value);
    }
}
