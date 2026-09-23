using UnityEngine;
using AINPCCoreEngine;

// Warms up the mob shared cache pool once when the scene starts. Mobs are
// intentionally never auto-prefetched by context updates alone (OVERVIEW.md
// §7.6/§10.5) - without this, mob_a/mob_b stay Unprepared until something
// else triggers a manual refresh (e.g. breaking the pot) or a first
// consumption happens. GossipNet itself never decides when to refresh
// (§1.1); this script is the game-side call for "warm the pool at boot".
//
// Optionally sets a fixed CurrentLocationTag once at boot - this demo never
// calls ContextManager.UpdatePosition otherwise, so the tag stays unset
// unless _locationTag is filled in via the Inspector. Left blank by
// default (no location set), since this is opt-in test/flavor data, not a
// baked-in default. Location is player-side shared context, not a per-NPC
// attribute - it just becomes one extra line in the dialogue-generation
// prompt (the position half of UpdatePosition has no effect on prompts at
// all, see DataModels.cs GameContext.PlayerPosition).
public class GossipNetBootstrap : MonoBehaviour
{
    [SerializeField] private string _locationTag = "始まりの町";

    private void Start()
    {
        var pipeline = FindFirstObjectByType<AsyncCachePipeline>();
        if (pipeline == null)
        {
            Debug.LogWarning("[GossipNetBootstrap] AsyncCachePipeline not found; mob cache not warmed up.");
            return;
        }

        _ = pipeline.RefreshMobCacheManuallyAsync(forceFullRefresh: true);

        if (!string.IsNullOrEmpty(_locationTag) && ContextManager.Instance != null)
            ContextManager.Instance.UpdatePosition(default, _locationTag);
    }
}
