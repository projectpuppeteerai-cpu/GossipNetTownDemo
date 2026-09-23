# GossipNetTownDemo

[GossipNet](https://github.com/projectpuppeteerai-cpu/gossipnet-ai-npc)（AI NPC Core Engine）を、
単なるAPI疎通確認用のボタン操作デモではなく、**実際に利用する側（消費者）の視点**で組み込んだ
2Dトップダウンの小さな町デモです。プレイヤーがキャラクターを操作して町を歩き回り、NPCに
話しかけたり、壺を壊したり盗んだりといった行動を取ると、それをGossipNet/JEVが認知・判定し、
NPCのセリフに反映されます。

## これは何のデモか

- GossipNetを**既存ゲームへ後付けする**想定の組み込み例（固定セリフ→LLM生成への差し替え、
  フォールバック時は元の固定セリフへ自然に戻る、など）
- 「JEV」（構造化知覚・判定レイヤー）の実配線例: プレイヤー行動を主要NPCが視覚・聴覚で
  知覚し、感情・脅威度・反応方針を判定してセリフの口調に反映する仕組み
- コンテキストイベント発生時に自動でキャッシュ更新API（`RefreshAllCachesManuallyAsync`等）を
  呼び出す、実際の呼び出しパターンの実装例

## セットアップ

1. Unity Editor `6000.3.23f1` で本プロジェクトを開く
   （`Window > Package Manager` がGossipNetパッケージをGit URL経由で自動解決します）
2. `gossipnet_config.json.example` をコピーして `gossipnet_config.json` にリネームし、
   `apiKey` に自分のAnthropic APIキーを記入する（**このファイルはコミットしないでください**。
   `.gitignore`で除外済みです）
   - 未設定のままでも動作します（APIキー未設定時は自動的にオフライン/フォールバック動作）
3. メニュー `GossipNetTownDemo > Build Demo Scene` でシーンを自動生成
4. メニュー `GossipNetTownDemo > Setup GossipNet Manager` で `GossipNetManager`
   （`ContextManager`/`NpcRegistry`/`LLMBridge`/`ActionDispatcher`/`AsyncCachePipeline`/
   `JevEvaluator`）を自動配線
5. Playモードで実行

## 操作方法

| キー | 内容 |
|---|---|
| 矢印キー / WASD | 移動 |
| Space（NPCに接触中） | 話しかける |
| X（NPCに接触中） | 攻撃する（暴力イベント） |
| H（困っているNPCに接触中） | 助ける |
| 壺・カゴ・装備などに接触 | それぞれのワールドイベント発火（壺=破壊、カゴ=盗み、装備=入手 等） |
| F3 | デバッグパネルの表示/非表示 |
| C（デバッグパネル表示中） | 近くの主要NPCのJEV判定をクリア |
| R（デバッグパネル表示中） | JEV・行動履歴・所持品・ワールドトリガーを全てリセット（Play再起動不要） |

頭上（村人スプライトの色）が赤=未準備／黄=生成中／緑=会話可能を表します。

## このミドルウェアが向いているゲーム・向いていないゲーム

GossipNetは「プレイヤー行動→裏で非同期にLLM生成・キャッシュ→話しかけた瞬間にキャッシュから
即座に返す」という設計です。**LLMの応答生成そのものは決して速くない**（実機検証では、
生成件数を1件に絞ってもAPI呼び出し自体の固定オーバーヘッド＝ネットワーク往復・キュー待ちが
支配的で、体感の速度はほぼ改善しなかった）ため、どんなゲームにも同じように向くわけではありません。

- **向いている**: テーブルトークRPG風の会話劇、『Slay the Spire』のような
  ターン制・デッキ構築系、アドベンチャー/ビジュアルノベルなど、**プレイヤーの意思決定と
  NPCの反応の間に自然な「間」がある**ゲーム。イベント発生からプレイヤーが実際に話しかける
  までの数百ms〜数秒を先読み生成の時間に充てられるため、体感の待ち時間をほぼ隠蔽できます
- **注意が必要**: アクション性・リアルタイム性が強いゲーム。イベント発生の直後に
  即座に反応セリフが欲しい場面では、キャッシュがまだ`Updating`（生成中）状態のことがあり、
  その間は会話不成立またはフォールバック文言（`NpcPersona.DefaultDialogue`／
  `fallbackDialogueOverride`）で埋めることになります。本デモの`major_guard`も、
  壺を壊した直後すぐ話しかけると生成中で待たされる場合があります
  （`AsyncCachePipeline`の`_inFlight`中は既存キャッシュがあっても`Updating`判定を優先する
  設計のため。詳細はGossipNet本体リポジトリのREADMEを参照）

対応が必要な場面（格闘ゲームの決着後の一言、弾幕が飛び交う中の即時リアクション等）では、
GossipNetの非同期キャッシュに頼らず、固定セリフ・短い定型パターンを併用することを推奨します。

## ライセンス・権利表記

- 本プロジェクトのソースコードは[MITライセンス](LICENSE)です
- 使用素材・依存ライブラリのライセンスは[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)を
  参照してください（Kenney.nl素材はCC0、GossipNet本体・Newtonsoft.JsonはMIT）
- GossipNet本体: https://github.com/projectpuppeteerai-cpu/gossipnet-ai-npc
