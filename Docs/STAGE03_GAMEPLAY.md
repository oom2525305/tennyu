# ステージ3「炎上する宿場」

ステージ2クリア後、年表から選択できます。StageData_03の既存GUIDを維持し、GameSceneのStageLoaderへStage_03を登録しました。ノーマル相当の単一構成です。難易度切替・延焼・ステージ4以降は未実装です。

## 遊び方

信長は左から右の門へ進みます。炎に触れる、信長へ雷を当てる、45秒経過で失敗します。

- **本通りを消火**：道の西と東にある2つの炎を、それぞれ雨で消します。2か所を1回の雨では消せない距離に配置しています。雨は2回、クールタイム5秒。炎は自然には燃え尽きません。
- **裏道を開通**：開始後、信長を巻き込まないよう左上の路地にある柵へ雷を落とします。3秒燃えると柵の当たり判定が消え、信長は上側の裏道へ進路を変えます。信長が炎に達する前に開通させてください。
- **風で助ける**：消火した道で信長を前へ押す、または雷を使う前に信長を離す使い方ができます。炎の中へ押すと失敗します。風で炎を消す・延焼させる機能はありません。
- **濡れに注意**：柵を雨で濡らすと3秒間は雷で着火できません（追加の雨で最大6秒）。燃焼中の柵への雨は消火するため、燃え尽きるまでの時間も最初からになります。
- 能力回数は風2・雷2・雨2。幽霊は使えません。

上記は実装ルールです。実際の操作による各解法のクリア確認、難易度調整は未実施です。保存された経路の長さからの徒歩時間見積りは本通り約22秒、裏道約35秒です。能力操作中は移動がスローになりますが、既存仕様どおり残り時間は通常速度で減ります。

## Unityで開く手順

1. GitHub Desktopで `feature/stage02-layout` を選び、Pullします。
2. Unity **6000.3.21f1** で開き、インポート後のConsoleにエラーがないか確認します。
3. `Assets/Project/Scenes/GameScene.unity` を開き、Hierarchyの **StageLoader** を選択します。
4. **Use Editor Test Stage** をオン、**Editor Test Stage Id** を **3** にしてPlayします。確認後はテスト設定をオフに戻してください。
5. 通常の流れはTitle → 年表 → ステージ2クリアで解放したステージ3 → 挑戦です。

## Prefabの設定箇所

`Assets/Project/Prefabs/Stage/Stage_03.prefab` を開きます。新規Componentと参照は設定済みで、手動で追加する必要はありません。

| 対象 | 役割・調整 |
| --- | --- |
| World/Environment/MainStreetFireWest・MainStreetFireEast | FlammableObject2Dで雷・雨・燃焼を管理。BurningArea2DのDamage Radiusが炎の接触範囲（0.55）。受信ColliderはAbilityTargetのTrigger。 |
| 各炎とDryFenceのFireVisual | 炎の表示とTenyuEffectLoopSound。消火・結果・リトライ・シーン移動で停止する既存音処理を使用。 |
| World/Environment/DryFence | FlammableObject2DのBurn Durationが3秒。Prevent Ignition While Wetをオン。WettableObject2Dで濡れを管理。 |
| DryFence/FenceObstacle | ObstacleレイヤーのCollider。燃え尽きると無効、リトライで有効。能力受信用Colliderとは別。 |
| Systems/BurningTownRoute | BurningTownRoute2D。柵が燃え尽きるとBack Routeへ変更。リトライはMain Routeと信長の開始位置を復元。 |
| World/Route | 本通りの経由地点とBackRoute0〜4。 |
| World/Environment/Obstacle/TownBuildings | 建物の通行禁止境界はPolygonCollider2D。 |
| Systems内のStageDirector | Clear On Timeoutをオフ。45秒経過で失敗。 |
| DivinePowerSystemのオブジェクト | 風2・雷2・雨2・幽霊0。クールタイムなどの調整。 |

新規画像は `Assets/Project/Resouce/Visual/stage03bg.png` と `dryFence.png`。今回生成した宿場背景と柵です。ステージ1・2のPrefabは変更していません。

### 音源の割り当て

BGM・開始・失敗・クリア・風・雷のSEは既存設定を引き継いでいます。**炎と雨のループ音源はリポジトリにないため未割り当て**です。

- 炎：上記3か所の **FireVisual → TenyuEffectLoopSound → Loop Clip** に「たき火」などを割り当てます。
- 雨：`Assets/Project/Prefabs/Effects/RainEffect.prefab` の **TenyuEffectLoopSound → Loop Clip** に雨の音源を割り当てます。これは共通Prefabなので他ステージにも反映されます。

## 実施済みの検証

- `Tools/validate_stage03.py`：本通りと裏道の全区間が信長の半径0.4＋余裕0.02を確保。最小壁距離は本通り0.6、裏道0.5。
- 炎・柵・能力の参照、レイヤー、初期状態、ステージ3の登録、時間切れ失敗の設定。
- 柵の3秒燃焼が最初の炎への接触時間見積りより短いこと、2か所の雨が別々に必要なこと、雨のクールタイムに時間的余裕があること。
- HUDの日本語文字。未収録だった全角スラッシュを収録済みの中点へ変更。
- ステージ2の既存静的検査、C#38本の構文解析、23シーン／PrefabのYAML・親子関係・Component参照。各ステージのCamera・AudioListener・EventSystem・SceneAudioが1つであること。

これはUnityコンパイル・物理実行・実機での攻略確認ではありません。

## Unityでの確認項目（未実施）

1. 観察中に2つの炎と柵が表示され、信長が動かず能力を使用できない。
2. 無操作で炎に触れると失敗。雨2回で本通りを消火し、門へ到達できる。
3. 開始直後の雷で柵だけを燃やし、3秒後に上の裏道へ進んでクリアできる。
4. 雨で濡らした柵には着火できず、乾燥後は着火する。燃焼中に雨で消すと開通しない。雷を信長へ当てると失敗する。
5. 燃焼中・裏道移動中にポーズ→再開。移動と燃焼が停止・再開する。
6. 裏道の途中・失敗・クリアからリトライ。信長が左の開始位置へ戻り、柵と2つの炎が復元され、能力回数が戻る。失敗Spriteは再開まで維持される。
7. 炎／雨の音源を割り当てた場合、消火・ポーズ・リトライ・結果・年表への移動で音が重複・残留しない。
8. 年表からステージ3へ進み、クリア記録がステージ3に保存される。ステージ1・2の開始・風・リトライも回帰確認する。
9. 16:9と横長画面で、炎・柵・上の道・3つの能力ボタンが見えるか確認する。

```sh
python -m pip install PyYAML shapely
python Tools/validate_stage03.py
python Tools/validate_stage02.py
```
