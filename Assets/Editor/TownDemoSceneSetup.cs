using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// Builds a minimal walkable town scene (ground + trees + player) from the
// Kenney sprites under Assets/Art/Kenney, the same "Editor menu builds the
// scene" pattern used by GossipNet's own demo scene setup.
public static class TownDemoSceneSetup
{
    private const string ScenePath = "Assets/Scenes/TownDemo.unity";
    private const string TilesFolder = "Assets/Art/Kenney/Tiles";
    private const string SpriteFolder = "Assets/Art/Kenney/TinyTown";
    private const string CharacterSpritePath = "Assets/Art/Kenney/Characters/hero_idle.png";
    private const string GeneratedFolder = "Assets/Art/Generated";
    private const string SolidSpritePath = GeneratedFolder + "/solid_white.png";

    private const int GroundWidth = 24;
    private const int GroundHeight = 16;
    private const string ObstaclesLayerName = "Obstacles";

    [MenuItem("GossipNetTownDemo/Build Demo Scene")]
    public static void BuildScene()
    {
        EnsureObstaclesLayerExists();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var groundTile = LoadOrCreateTile("tile_grass");
        var dirtTile = LoadOrCreateTile("tile_dirt");
        var treeTile = LoadOrCreateTile("tile_tree");

        var gridGo = new GameObject("Grid", typeof(Grid));

        var groundGo = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
        groundGo.transform.SetParent(gridGo.transform);
        var groundTilemap = groundGo.GetComponent<Tilemap>();
        groundGo.GetComponent<TilemapRenderer>().sortingOrder = 0;

        var obstaclesGo = new GameObject("Obstacles", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        obstaclesGo.transform.SetParent(gridGo.transform);
        obstaclesGo.layer = LayerMask.NameToLayer("Obstacles"); // TownActionTriggersのJEV視界判定（Physics2D.Linecast）が参照する
        var obstaclesTilemap = obstaclesGo.GetComponent<Tilemap>();
        obstaclesGo.GetComponent<TilemapRenderer>().sortingOrder = 1;

        int minX = -GroundWidth / 2;
        int minY = -GroundHeight / 2;
        for (int x = minX; x < minX + GroundWidth; x++)
        {
            for (int y = minY; y < minY + GroundHeight; y++)
            {
                bool isPath = y == 0 && x >= minX + 2 && x < minX + GroundWidth - 2;
                groundTilemap.SetTile(new Vector3Int(x, y, 0), isPath ? dirtTile : groundTile);
            }
        }

        int[,] treePositions =
        {
            { minX + 2, minY + 2 }, { minX + 3, minY + 2 }, { minX + 2, minY + 3 },
            { minX + GroundWidth - 3, minY + 2 }, { minX + GroundWidth - 4, minY + 3 },
            { minX + 5, minY + GroundHeight - 3 }, { minX + 6, minY + GroundHeight - 3 },
            { minX + GroundWidth - 6, minY + GroundHeight - 4 }, { minX + GroundWidth - 7, minY + GroundHeight - 3 },
        };
        for (int i = 0; i < treePositions.GetLength(0); i++)
        {
            int tx = treePositions[i, 0];
            int ty = treePositions[i, 1];
            if (ty == 0) continue; // keep the path clear
            obstaclesTilemap.SetTile(new Vector3Int(tx, ty, 0), treeTile);
        }

        var heroSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CharacterSpritePath);
        var playerGo = new GameObject("Player", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(PlayerController));
        playerGo.tag = "Player";
        playerGo.transform.position = new Vector3(0f, 0f, 0f);
        var spriteRenderer = playerGo.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = heroSprite;
        spriteRenderer.sortingOrder = 2;

        var body = playerGo.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        var collider = playerGo.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(0.7f, 0.7f);

        var playerController = playerGo.GetComponent<PlayerController>();
        var soPlayer = new SerializedObject(playerController);
        soPlayer.FindProperty("_spriteRenderer").objectReferenceValue = spriteRenderer;
        soPlayer.ApplyModifiedPropertiesWithoutUndo();

        CreateMobPlaceholder("mob_a_idle", "mob_a", NpcTierPlaceholder.Mob, new Vector3(-4f, 2f, 0f),
            "村人A", "いらっしゃい、今日はいい天気だね。");
        CreateMobPlaceholder("mob_b_idle", "mob_b", NpcTierPlaceholder.Mob, new Vector3(4f, 2f, 0f),
            "村人B", "最近このあたりで物騒な噂を耳にするよ。");
        CreateMobPlaceholder("major_guard_idle", "major_guard", NpcTierPlaceholder.Major, new Vector3(0f, -3f, 0f),
            "衛兵", "ここは町の見張り場だ。何か困りごとか?");

        CreateWorldLabel("村人A\n(接触してSpace:会話 / X:攻撃)", new Vector3(-4f, 2f + 0.8f, 0f));
        CreateWorldLabel("村人B\n(接触してSpace:会話 / X:攻撃)", new Vector3(4f, 2f + 0.8f, 0f));
        CreateWorldLabel("衛兵\n(接触してSpace:会話 / X:攻撃)", new Vector3(0f, -3f + 0.8f, 0f));

        CreatePot(new Vector3(6f, -1f, 0f));
        CreateWorldLabel("町の壺\n(接触で破壊 → 目撃したNPCが反応)", new Vector3(6f, -1f + 0.45f, 0f));

        CreateCrate(new Vector3(-6f, -1f, 0f));
        CreateWorldLabel("商品の荷物カゴ\n(接触で盗む → 目撃したNPCが反応)", new Vector3(-6f, -1f + 0.45f, 0f));

        CreateHelpableVillager(new Vector3(-5f, -3f, 0f));
        CreateWorldLabel("困っている住民\n(接触してH:助ける)", new Vector3(-5f, -3f + 0.8f, 0f));

        CreateRestrictedZone(new Vector3(0f, -6f, 0f), new Vector2(4f, 2f));
        CreateWorldLabel("立入禁止区域\n(侵入で警戒される)", new Vector3(0f, -6f + 0.8f, 0f));

        CreateEquipmentPickup(new Vector3(5f, -3f, 0f));
        CreateWorldLabel("鋼の装備一式\n(接触で入手)", new Vector3(5f, -3f + 0.45f, 0f));

        CreateRobberyIncident(new Vector3(3f, 0f, 0f));
        CreateWorldLabel("強盗事件\n(接触で目撃 → 周囲のNPCが反応)", new Vector3(3f, 0f + 0.8f, 0f));

        var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(CameraFollow));
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        cameraGo.transform.position = new Vector3(0f, 0f, -10f);
        var cameraFollow = cameraGo.GetComponent<CameraFollow>();
        var soCamera = new SerializedObject(cameraFollow);
        soCamera.FindProperty("_target").objectReferenceValue = playerGo.transform;
        soCamera.ApplyModifiedPropertiesWithoutUndo();

        BuildUi();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"GossipNetTownDemo: built {ScenePath}");
    }

    /// <summary>
    /// "Obstacles"レイヤー（TownActionTriggersのJEV視界判定が参照するPhysics2D.Linecast用）が
    /// 無ければユーザーレイヤー（8〜31）の空きスロットに作成する。TagManager.assetを直接編集しても
    /// 起動中のEditorのメモリ上には反映されない（再起動が必要になる）ため、SerializedObject経由で
    /// Unity側のAPIを通して変更する。
    /// </summary>
    private static void EnsureObstaclesLayerExists()
    {
        if (LayerMask.NameToLayer(ObstaclesLayerName) != -1)
            return;

        var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagManagerAssets.Length == 0)
        {
            Debug.LogError("[TownDemoSceneSetup] ProjectSettings/TagManager.assetが読み込めませんでした。");
            return;
        }

        var tagManager = new SerializedObject(tagManagerAssets[0]);
        var layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++)
        {
            var layerProp = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(layerProp.stringValue))
                continue;

            layerProp.stringValue = ObstaclesLayerName;
            tagManager.ApplyModifiedProperties();
            return;
        }

        Debug.LogError("[TownDemoSceneSetup] 空きユーザーレイヤーが見つからず、Obstaclesレイヤーを作成できませんでした。");
    }

    private static void CreateMobPlaceholder(string spriteName, string npcId, NpcTierPlaceholder tier, Vector3 position,
        string displayName, string fixedLine)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Kenney/Characters/{spriteName}.png");
        var go = new GameObject(npcId, typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(NpcPlaceholder), typeof(InteractPrompt));
        go.transform.position = position;

        var spriteRenderer = go.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 2;

        var trigger = go.GetComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 1.1f;

        var npc = go.GetComponent<NpcPlaceholder>();
        var soNpc = new SerializedObject(npc);
        soNpc.FindProperty("_npcId").stringValue = npcId;
        soNpc.FindProperty("_tier").enumValueIndex = (int)tier;
        soNpc.ApplyModifiedPropertiesWithoutUndo();

        var prompt = go.GetComponent<InteractPrompt>();
        var soPrompt = new SerializedObject(prompt);
        soPrompt.FindProperty("_spriteRenderer").objectReferenceValue = spriteRenderer;
        soPrompt.FindProperty("_displayName").stringValue = displayName;
        soPrompt.FindProperty("_fixedLine").stringValue = fixedLine;
        soPrompt.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreatePot(Vector3 position)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/tile_pot.png");
        var go = new GameObject("pot", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(BreakablePot));
        go.transform.position = position;

        var spriteRenderer = go.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 2;

        var trigger = go.GetComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.45f;
    }

    private static void CreateCrate(Vector3 position)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/tile_pot.png");
        var go = new GameObject("goods_crate", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(StealableCrate));
        go.transform.position = position;

        var spriteRenderer = go.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 2;
        spriteRenderer.color = new Color(0.85f, 0.65f, 0.35f); // 壺と見分けるためのティント（専用素材は未切り出し）

        var trigger = go.GetComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.45f;
    }

    private static void CreateHelpableVillager(Vector3 position)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Kenney/Characters/mob_a_idle.png");
        var go = new GameObject("troubled_villager", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(HelpableVillager));
        go.transform.position = position;

        var spriteRenderer = go.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 2;
        spriteRenderer.color = new Color(0.75f, 0.85f, 1f); // mob_aと見分けるための青みティント（専用素材は未切り出し）

        var trigger = go.GetComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 1.1f;
    }

    private static void CreateRestrictedZone(Vector3 position, Vector2 size)
    {
        var go = new GameObject("restricted_zone", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(RestrictedZoneTrigger));
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var spriteRenderer = go.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = LoadOrCreateSolidSprite();
        spriteRenderer.color = new Color(0.9f, 0.2f, 0.2f, 0.25f);
        spriteRenderer.sortingOrder = 1;

        var trigger = go.GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = Vector2.one;
    }

    private static void CreateEquipmentPickup(Vector3 position)
    {
        var go = new GameObject("equipment_pickup", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(EquipmentPickup));
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        var spriteRenderer = go.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = LoadOrCreateSolidSprite();
        spriteRenderer.color = new Color(0.75f, 0.78f, 0.85f); // 鋼装備を示す銀色（専用素材は未切り出し）
        spriteRenderer.sortingOrder = 2;

        var trigger = go.GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = Vector2.one;
    }

    private static void CreateRobberyIncident(Vector3 position)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Kenney/Characters/mob_b_idle.png");
        var go = new GameObject("robbery_incident", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(RobberyIncidentTrigger));
        go.transform.position = position;

        var spriteRenderer = go.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 2;
        spriteRenderer.color = new Color(0.35f, 0.3f, 0.35f); // 強盗を示す暗いティント（専用素材は未切り出し）

        var trigger = go.GetComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 1.1f;
    }

    /// <summary>
    /// 「これは何か・どう発火するか」を示すワールド空間の浮遊テキストラベル。初見のプレイヤーが
    /// 各オブジェクトの意味を理解できるようにするため追加（公開デモ向け）。TextMeshはワールド単位
    /// でスケールされるため、非等倍スケールの親（CreateRestrictedZoneの箱等）には絶対にぶら下げず、
    /// 常にワールド座標を直接指定する（歪み・意図しない拡大を避けるため）。
    /// </summary>
    private static void CreateWorldLabel(string text, Vector3 worldPosition)
    {
        var go = new GameObject("Label", typeof(TextMesh));
        go.transform.position = worldPosition;

        var textMesh = go.GetComponent<TextMesh>();
        textMesh.text = text;
        textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textMesh.fontSize = 48;
        textMesh.characterSize = 0.05f;
        textMesh.anchor = TextAnchor.LowerCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;

        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = textMesh.font.material;
        renderer.sortingOrder = 10;
    }

    private static void BuildUi()
    {
        var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        var triggersGo = new GameObject("TownActionTriggers", typeof(TownActionTriggers));

        BuildControlsLegend(canvasGo.transform);
        BuildDialogueBox(canvasGo.transform);

        var debugPanelGo = BuildScrollablePanel(canvasGo.transform, new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, 0.5f), out var debugContentText, out var debugContentRect);
        debugContentText.text =
            "=== プレイヤーコンテキスト (未接続) ===\n" +
            "GossipNet.ContextManager 未導入\n" +
            "RecordAction / SetWorldFlag の結果はここに表示予定\n\n" +
            "=== NPCキャッシュテーブル (未接続) ===\n" +
            "GossipNet.AsyncCachePipeline 未導入\n" +
            "mob_a / mob_b / major_guard のキャッシュ状態はここに表示予定\n\n" +
            "F3で表示/非表示";

        var debugViewGo = new GameObject("DebugView", typeof(DebugViewPlaceholder));
        var debugView = debugViewGo.GetComponent<DebugViewPlaceholder>();
        var soDebugView = new SerializedObject(debugView);
        soDebugView.FindProperty("_panel").objectReferenceValue = debugPanelGo;
        soDebugView.FindProperty("_contentText").objectReferenceValue = debugContentText;
        soDebugView.FindProperty("_content").objectReferenceValue = debugContentRect;
        soDebugView.ApplyModifiedPropertiesWithoutUndo();

        var eventLogPanelGo = BuildScrollablePanel(canvasGo.transform, new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(1f, 0.5f), out var eventLogText, out var eventLogContentRect);
        eventLogText.text = "=== イベント & セリフ履歴 (未接続) ===";

        var eventLogViewGo = new GameObject("EventDialogueLogView", typeof(EventDialogueLogView));
        var eventLogView = eventLogViewGo.GetComponent<EventDialogueLogView>();
        var soEventLogView = new SerializedObject(eventLogView);
        soEventLogView.FindProperty("_panel").objectReferenceValue = eventLogPanelGo;
        soEventLogView.FindProperty("_contentText").objectReferenceValue = eventLogText;
        soEventLogView.FindProperty("_content").objectReferenceValue = eventLogContentRect;
        soEventLogView.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// F3デバッグパネル用の共通レイアウト（背景+ScrollRect+Viewport(RectMask2D)+Content(Text)）を
    /// 構築する。DebugViewPlaceholder（左）とEventDialogueLogView（右）で共用。
    /// </summary>
    private static GameObject BuildScrollablePanel(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, out Text contentText, out RectTransform contentRect)
    {
        var panelGo = new GameObject("Panel", typeof(Image));
        panelGo.transform.SetParent(parent, false);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = anchorMin;
        panelRect.anchorMax = anchorMax;
        panelRect.pivot = pivot;
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(360f, 0f);
        panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        var scrollGo = new GameObject("ScrollView", typeof(ScrollRect));
        scrollGo.transform.SetParent(panelGo.transform, false);
        var scrollRect = scrollGo.GetComponent<ScrollRect>();
        var scrollRectTransform = scrollGo.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(12f, 12f);
        scrollRectTransform.offsetMax = new Vector2(-12f, -12f);

        var viewportGo = new GameObject("Viewport", typeof(RectMask2D));
        viewportGo.transform.SetParent(scrollGo.transform, false);
        var viewportRect = viewportGo.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        var contentGo = new GameObject("Content", typeof(Text));
        contentGo.transform.SetParent(viewportGo.transform, false);
        contentRect = contentGo.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        contentText = contentGo.GetComponent<Text>();
        contentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        contentText.fontSize = 16;
        contentText.color = Color.white;
        contentText.alignment = TextAnchor.UpperLeft;
        contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
        contentText.verticalOverflow = VerticalWrapMode.Overflow;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        return panelGo;
    }

    /// <summary>
    /// 常時表示の操作方法パネル（画面上部中央）。初見のプレイヤー向けに、キー操作の一覧を
    /// 常に見えるようにしておく（公開デモ向け。トグル無し、F3デバッグパネルとは別物）。
    /// </summary>
    private static void BuildControlsLegend(Transform parent)
    {
        var panelGo = new GameObject("ControlsLegend", typeof(Image));
        panelGo.transform.SetParent(parent, false);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -12f);
        panelRect.sizeDelta = new Vector2(760f, 40f);
        panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        var textGo = new GameObject("Text", typeof(Text));
        textGo.transform.SetParent(panelGo.transform, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var text = textGo.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 16;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = "移動: 矢印/WASD　会話: Space　攻撃: X　助ける: H　デバッグ表示: F3　(F3中) JEVクリア: C　全部リセット: R";
    }

    private static void BuildDialogueBox(Transform parent)
    {
        var panelGo = new GameObject("DialoguePanel", typeof(Image));
        panelGo.transform.SetParent(parent, false);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 40f);
        panelRect.sizeDelta = new Vector2(760f, 100f);
        panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);

        var textGo = new GameObject("Text", typeof(Text));
        textGo.transform.SetParent(panelGo.transform, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 12f);
        textRect.offsetMax = new Vector2(-20f, -12f);
        var text = textGo.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;

        var dialogueBoxGo = new GameObject("DialogueBox", typeof(DialogueBoxPlaceholder));
        var dialogueBox = dialogueBoxGo.GetComponent<DialogueBoxPlaceholder>();
        var soDialogueBox = new SerializedObject(dialogueBox);
        soDialogueBox.FindProperty("_panel").objectReferenceValue = panelGo;
        soDialogueBox.FindProperty("_text").objectReferenceValue = text;
        soDialogueBox.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Tile LoadOrCreateTile(string spriteName)
    {
        Directory.CreateDirectory(TilesFolder);
        string tileAssetPath = $"{TilesFolder}/{spriteName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Tile>(tileAssetPath);
        if (existing != null)
            return existing;

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/{spriteName}.png");
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        AssetDatabase.CreateAsset(tile, tileAssetPath);
        return tile;
    }

    /// <summary>
    /// 1x1の単色スプライトを、プロジェクトのアセットとして永続化して返す（2回目以降は既存アセットを
    /// 再利用）。Sprite.Create()でその場限りのSpriteを作ると、シーン保存後のドメインリロード
    /// （Playモード突入時など）で参照が切れてSpriteRendererが何も表示しなくなるため、
    /// LoadOrCreateTileと同じ「アセットとしてディスクへ書き出す」方式に統一している。
    /// 色味はSpriteRenderer.color側のティントで個別に付ける（CreateRestrictedZone/
    /// CreateEquipmentPickup参照）。
    /// </summary>
    private static Sprite LoadOrCreateSolidSprite()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(SolidSpritePath);
        if (existing != null)
            return existing;

        Directory.CreateDirectory(GeneratedFolder);

        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color32[texture.width * texture.height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        texture.SetPixels32(pixels);
        texture.Apply();

        File.WriteAllBytes(SolidSpritePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(SolidSpritePath);

        var importer = (TextureImporter)AssetImporter.GetAtPath(SolidSpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 4;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(SolidSpritePath);
    }
}
