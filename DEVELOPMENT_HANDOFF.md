# 天祐：実装調査と音の引き継ぎ

> この資料は音対応時点の調査記録です。ステージ2はその後、新背景・道路・風／幽霊のUIに加え、暗殺者の追跡・斬撃予告・能力による妨害へ更新しています。以下の表は音対応当時の記録であり、ステージ2・時間切れ・暗殺者SEの現在の仕様はリンク先を優先してください。現在の構成と確認手順は [ステージ2の配置資料](Docs/STAGE02_LAYOUT.md) を参照してください。旧ステージ2での雨・炎の確認手順は、現在の通常プレイには当てはまりません。

> ステージ3も追加済みです。現在の宿場・炎・柵・裏道の仕様と確認手順は [ステージ3の資料](Docs/STAGE03_GAMEPLAY.md) を参照してください。StageLoaderは現在1〜3を登録しています。

> その後のステージ1〜3の移動・風・落石・時間・ゴール判定の修正は [精査記録](Docs/STAGE_REVIEW.md) を参照してください。以下の旧調査表より現在の修正記録を優先してください。

## 調査の前提

- 対象はこのリポジトリの `main`、調査開始時のコミットは `bbee21b`。別のWindows上で開いている未保存のUnity画面は確認できていません。
- 調査開始時の `git status --short` は空。`/workspace` とリポジトリ内に `AGENTS.md` はありませんでした。
- Unityの指定は **6000.3.21f1**、URPは17.3.0、Input Systemは1.20.0。新Input Systemが有効です。
- このクラウド環境にはUnity Editorがありません。下記の静的検証とUnityでの動作確認を区別してください。

## 実コードと説明の一致・差異

| 項目 | 実際の状態 |
| --- | --- |
| シーン遷移 | Title → TimelineScene → GameScene。Titleは画面全体のタップとフェード、年表は横スクロール・詳細・外側タップで閉じる処理が実装済み。 |
| ロック・クリア保存 | `StageProgress` が `PlayerPrefs` にステージIDごとに保存。前のステージのクリアで次が解放される。 |
| ステージ数 | `StageLoader` の登録は **1・2のみ**。Prefabも `Stage_01` / `Stage_02` の2つ。年表データ・ボタンは3つあり、3は解放後に挑戦しても対応Prefabがなくロードエラーになる。4・5は未配置。今回、ステージ構成は変更していない。 |
| 難易度 | `TimelineStageData.Difficulty` は1〜5の星表示。ノーマル／ハードの選択・保持・別クリア記録は未実装。3つのデータのステージ名は現在すべて「桶狭間の戦い」。 |
| GameSceneとPrefab | GameSceneはStageLoaderのみ。各Stage PrefabにMain Camera、Systems、World、Canvas、EventSystemを持つ。保存データを入れ子Prefabまで調べた範囲で、Camera・AudioListener・EventSystemは各ステージ1つずつ。 |
| Stage_01 | 風3回・雷2回、雨と幽霊は0回。落石はタイマーとTransform補間で落下する。落石Prefabに能力受信コンポーネントはなく、風で落石をそらす／雷で支えを壊す接続はまだない。信長を風で押す処理はある。 |
| Stage_02 | 現在は落石・DryTreeなどを含む構成。風3・雷2・雨2・幽霊2回で、構想の「風・幽霊だけの暗殺者ステージ」にはなっていない。暗殺者・攻撃予告処理は存在しないため、架空のイベントは追加していない。 |
| 時間・クリア | Observation / Playing / Paused / Cleared / Failedが存在。開始前は能力使用不可。`StageDirector` は残り時間が0になってもクリアにする。ゴール到達でもクリア可能。 |
| スロー | 実際のドラッグは `StageTime.BeginAbilityDrag/EndAbilityDrag` により移動・能力クールタイム・落石などを1/3速にする。`StageDirector.BeginAiming/EndAiming` の `Time.timeScale = 0.2` 経路は定義されているが、コード・保存されたイベントからの呼び出しは見つからない。ステージ残り時間は `Time.deltaTime` なので現在のドラッグでは遅くならない。今回この仕様は変更していない。 |
| 信長 | Rigidbody2D、経由地点、GridPathfinder2Dによる迂回、4方向Sprite、風による移動、幽霊への誘導を実装済み。 |
| 失敗イラスト | `FallSideways()` は互換用の名前として残り、回転させずFailed Spriteへ切替。リトライ後も保持し、Playingで通常表示へ戻す。Nobunaga PrefabのFailed Spriteは設定済み。 |
| 雷・雨・燃焼 | 雷で信長が失敗し、燃える対象に着火、雨で消火・濡れ付与。濡れた対象への雷伝播はコードに存在する。DryTreeの炎は `fireVisual.SetActive()` で表示を切り替えている。 |
| 今後の能力 | 土壁・対象単体の時間停止は未実装。ステージ全体のポーズとは別の処理が必要。 |
| 画面とFPS | TitleにGameInitializerがあり、60FPS目標・縦回転禁止・左右の横向き自動回転を設定。片側の横向きだけに固定してはいない。GameSceneから直接再生した場合、TitleのInitializerは生成されない。 |

## 今回反映した音の接続

音スクリプト4本は最初から存在していました。TitleとTimelineSceneの直下 `Audio`、Stage_01の `Systems/Audio` もすでに設定されていました。

- Stage_02にも `Systems/Audio` と `TenyuSceneAudio` を追加。Stage_02自身のStageDirector・DivinePowerSystemを参照し、Stage_01と同じ既存BGM・SEを割り当てています。
- TimelineSceneの通常ボタン、各ステージのPause・Resume・Retry・年表へ戻るボタン、共通のStage01Button / PausePanel / ClearPanel / FailedPanelに `TenyuButtonSound` を接続しました。追加は計21コンポーネントです。共通Prefab内のScene Audio欄は空で正常です。操作時に同じシーンの音管理を取得します。
- **StartButton（時を動かす）と能力ドラッグボタンには通常ボタン音を追加していません。** 開始音はObservation → Playingの通知、風・雷・幽霊の発動音は使用回数減少の通知から再生します。キャンセル・補充では発動音を鳴らさない既存処理を維持しています。
- Titleの `TitleCanvas/FullScreenTapCatcher` はButtonではないため、TitleScreenControllerから既存Audioにタップ音を渡すようにしました。
- 雨PrefabのルートとDryTreeのFireVisualに `TenyuEffectLoopSound` を追加。雨はParticleSystemとRenderer、炎はSpriteRendererを監視します。GameObject無効化だけでなくRenderer.enabledをオフにした場合も停止対象です。カメラの画面外に出ることだけでは停止しません。
- FallingRockに `WarningSound` / `ImpactSound` 子オブジェクトを追加し、AudioSourceと `TenyuStageSourceGuard` を配置。FallingRock2Dの参照も設定済みです。予告は残り**ゲーム内2秒**以下で1回鳴り、落下開始で止まります。ポーズ中は停止位置を保持し、再開時に続きます。
- 予告音は結果画面で停止、着地の短い音は失敗原因として最後まで鳴らす設定です。リトライ・無効化では両方止めます。リトライ時には着地ParticleSystemもクリアします。
- 音管理をポーズ中に再有効化した場合のBGM音量を補正し、結果SEの直前に古いUI音を停止します。雨・炎・能力音のポーズ／リセット処理、シーン破棄時の停止とイベント購読解除を使用します。AudioをDontDestroyOnLoadにする変更はありません。

既存の音源8本は名前が `.wav` でしたが、実データはすべてMP3でした。`Assets/Project/Resouce/Audio` 内のファイルと対応する `.meta` を **`.mp3` / `.mp3.meta` に変更**しています。再エンコードはせず、音声データ・meta内容・GUIDは完全に保持しています。Inspector参照はGUIDなので参照先は維持されます。GitHub Desktopで変更を反映する際は、旧名の削除と新名の追加を両方含めてください。

## Unityで追加する音源とInspector設定

下表のComponent・参照先は今回追加済みです。音源は取得していないので、該当するクリップ欄だけが空です。ProjectウィンドウからPrefabをダブルクリックして設定し、保存してください。Prefabの変更は両ステージ内の入れ子インスタンスにも反映されます。

| 開くアセット | 選ぶGameObject | Component / 設定欄 | 割り当てる音の候補 |
| --- | --- | --- | --- |
| `Assets/Project/Prefabs/Effects/RainEffect.prefab` | ルート `RainEffect` | TenyuEffectLoopSound → **Loop Clip** | 雨が降る1 |
| `Assets/Project/Prefabs/Environment/DryTree.prefab` | `DryTree/FireVisual` | TenyuEffectLoopSound → **Loop Clip** | たき火 |
| `Assets/Project/Prefabs/Hazards/FallingRock.prefab` | `FallingRock/WarningSound` | AudioSource → **Audio Clip**（Unityの表示によってはAudio Resource） | 岩にヒビが入る |
| 同上 | `FallingRock/ImpactSound` | AudioSource → **Audio Clip / Audio Resource** | 崖崩れ |
| `Assets/Project/Prefabs/Stage/Stage_02.prefab` | `Stage_02/Systems/Audio` | TenyuSceneAudio → **Ghost Sound** | 幽霊が現れる |

- FallingRock本体の **Warning Sound Lead Time** は2。短すぎる場合はここを調整します。0は予告音なしです。
- WarningSound / ImpactSoundの **Play On Awake、Loopはオフ**。Guardの **Stop On Result** はWarningSoundがオン、ImpactSoundがオフです。
- EffectLoopSoundとGuardの **Stage Directorは空で正常**です。実行時に読み込まれたステージのStageDirectorを取得します。
- 雨・炎の **Watched Particles / Watched Renderer** は設定済みなので変更不要です。
- 幽霊を他ステージでも使うときは、そのステージのAudioのGhost Soundも設定します。Stage_01は現在幽霊を使いません。
- 暗殺者の「刀を構える」は、暗殺者本体と攻撃予告処理の実装後に接続します。

## 実行できた検証

- C#全35ファイルをtree-sitter C#パーサーで解析し、構文エラーなし。**Unity APIの型チェック・コンパイルの代わりではありません。**
- シーン／Prefab計22アセットのYAML解析、fileID重複、Componentの所有者、Transformの親子参照、追加したローカル参照を検証。
- 入れ子Prefabを含め、Title・TimelineScene・Stage_01・Stage_02それぞれのCamera / AudioListener / EventSystem / TenyuSceneAudioが1つずつであることを検証。GameSceneには重複を作るCamera・AudioListener・音管理なし。
- 通常ボタンの接続、StartButton・能力ボタンへの重複音コンポーネントがないこと、雨・炎の監視先、落石音源とGuardの設定を検証。
- 既存8音源をffmpegで最後までデコードし、エラーなし。名前変更前後の音声とmetaをバイト比較し一致。
- `git diff --check` で空白エラーなし。

Unity Editorでのコンパイル、アセットの再インポート、Play Mode、音を聴いての確認は**未実施**です。上記は保存データの検証であり、実際のプレイで重複・残留しないことを確認済みとはしていません。

## Unityでの動作確認手順

1. Unity **6000.3.21f1** でプロジェクトを開き、再インポート終了を待ちます。Consoleにコンパイル・AudioImporter・Missing Scriptのエラーがないことを確認します。音源の拡張子変更後も、Title / TimelineScene / 各StageのAudio欄がMissingになっていないことを確認してください。
2. 上表の空欄に音源を設定します。まず `Assets/Project/Scenes/Title.unity` を開いて再生します。タイトルBGMと画面タップ音、年表への切り替えを確認します。
3. 年表で解放済みステージ、詳細の戻る、外側タップを試します。通常ボタンは1回鳴り、ロックされたボタンは鳴らないことを確認します。現在、通常ボタン音はクリック完了ではなく**押した瞬間**に鳴る仕様です。
4. Stage_01へ進み、観察中からBGMが1本だけ鳴ることを確認します。「時を動かす」は開始SEだけが1回鳴り、通常ボタン音が重ならないことを確認します。風・雷の成功時は各1回、ドラッグキャンセル時は発動音なしを確認します。
5. **ポーズ／再開**：長い雷SE・落石予告中にポーズします。BGMが小さくなり、能力音・予告音が止まり、再開すると続くことを確認します。再開では開始SEを再生しないことも確認します。
6. **失敗／リトライ**：信長へ雷を当てる、落石を受ける操作を試します。失敗SEは1回、BGM・ループ・予告は停止、原因となった雷／着地の短い音は最後まで鳴ることを確認します。直後にリトライすると前回の音が残らず、観察BGMに戻ることを確認します。信長は開始位置で失敗イラストを保持し、「時を動かす」で通常表示へ戻ることも確認します。
7. **クリア／リトライ**：ゴール到達または現在の45秒タイマー満了を試します。クリアSEは1回、環境音は停止、リトライで観察状態に戻ることを確認します。リトライを繰り返して音量や再生数が増えないか確認します。
8. Stage_02はStage_01クリア後に年表から選べます。直接試す場合は、Play Modeを止め、GameSceneの `StageLoader` で **Use Editor Test Stageをオン、Editor Test Stage Idを2** にします。試した後はオフへ戻してください。
9. Stage_02で雨・炎・幽霊を試します。DryTreeへ雷→炎ループ、雨→消火して炎ループ停止、雨エフェクト終了→雨ループ停止、幽霊使用成功→発動音1回を確認します。DryTreeのBurn Durationは現在1秒なので、消火を試すときはPlay Mode中だけ値を延ばすと確認しやすくなります。
10. 雨・炎の表示中にポーズ→再開、ポーズからリトライを試します。Play Mode中、FireVisualのSpriteRendererのチェックを外してループ音が止まることも確認します。確認後はチェックを戻します。
11. **シーン移動**：ポーズ中・失敗後・クリア後に「年表へ戻る」を試し、前のステージ音が消えて年表BGMだけになることを確認します。往復を繰り返してAudioオブジェクトやAudioListenerが増えないことをHierarchyとConsoleで確認します。シーン移動ボタンの短いSEはシーン破棄時に途切れる場合があります（シーンを越えて残さない構成です）。
