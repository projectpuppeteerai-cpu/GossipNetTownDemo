using System.Collections.Generic;

// A single chronological, screen-display-only log combining world events
// (TownActionTriggers.RecordWorldEvent) and NPC dialogue lines
// (DialogueBoxPlaceholder.ShowLine), so it's easy to see which line(s)
// followed which event. Not fed back into GossipNet's context/prompt -
// shown via EventDialogueLogView's right-side F3 panel. Static, mirroring
// the shared-state pattern already used by InteractPrompt.s_nearbyNpcIds.
public static class EventDialogueLog
{
    private const int MaxEntries = 20;
    private static readonly List<string> s_entries = new List<string>();

    public static IReadOnlyList<string> Entries => s_entries;

    public static void LogEvent(string description)
    {
        Add($"[EVENT] {description}");
    }

    public static void LogDialogue(string speakerName, string line)
    {
        Add($"[SAY] {speakerName}: {line}");
    }

    public static void Clear()
    {
        s_entries.Clear();
    }

    private static void Add(string entry)
    {
        s_entries.Add(entry);
        if (s_entries.Count > MaxEntries)
            s_entries.RemoveAt(0);
    }
}
