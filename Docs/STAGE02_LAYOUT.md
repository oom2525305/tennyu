# ステージ2：背景・移動範囲・UI

`stage02bg.png` を使い、Stage_01と同じStageDirector・RouteWalker2D・GridPathfinder2D・Obstacleレイヤーの構成で、Stage_02の移動とUIを設定しました。

**この変更はステージ2の土台です。暗殺者・追跡・攻撃はまだありません。現状は能力を使わなくても門へ進むため、完成したパズルの難易度調整は今後の作業です。UnityでのPlay Mode確認も未実施です。**

## 配置した内容

| 対象 | 設定 |
| --- | --- |
| `Stage_02/World/Environment/Background` | 新しいstage02bgの既存Spriteを使用。画像・meta・SpriteのGUID/fileIDは維持。背景サイズ16.72 × 9.41、Scale 1。 |
| `Stage_02/Main Camera` | 背景中央、Orthographic Size 4.705。まずGameビューを16:9にして確認。 |
| `Stage_02/World/Route/Point_00`〜`Point_07` | 左下の橋から右上の門へ進む8個の経由地点。既存のfileIDとRouteWalker参照を維持。 |
| `Stage_02/World/Route/StageGoal` | 門の開口部に移動。既存のStageGoal2DとTriggerを使用。 |
| `Stage_02/World/Environment/Obstacle/NorthBankAndFort` | 川・北側の岩場・柵・関所側を通れなくするPolygonCollider2D。LayerはObstacle、Is Triggerはオフ。 |
| `Stage_02/World/Environment/Obstacle/SouthBankAndRocks` | 南側の川岸・岩場を通れなくするPolygonCollider2D。同じくObstacleレイヤー。 |
| `Stage_02/World/Navigation` | 探索範囲16.72 × 9.41、Cell Size 0.15、7,056セル（上限10,000）。門や脇道の幅に合わせ、ステージ1の0.3より細かくした。 |
| `Stage_02/Canvas/GameUI/AbilityBar` | 左下に風3回・幽霊2回。雷・雨はこのPrefab内のみ非表示、使用可能回数0。 |
| `Stage_02/Canvas/GameUI/ObservationPanel/StartButton` | ステージ1と同様に右下に配置。既存の開始イベントを維持。 |
| `Stage_02/Canvas/GameUI/HUD` | ステージ名、風・幽霊の操作説明、残り時間の背景を追加。既存の和風画像・日本語フォントを使用。装飾・説明はRaycast Targetをオフ。 |
| `Stage_02/Canvas` | Scale With Screen Size、基準1600 × 900、Match 0.5。ポーズ・クリア・失敗・リトライ・年表へ戻るUIと参照を維持。 |
| `StageData_02` | 年表の名前を「忍び寄る凶刃」にし、説明と背景サムネイルを設定。ステージID・解放条件・年の設定は維持。 |

脇道は左上と右下に残しています。川・柵・岩場の内部へは移動できない構成です。幽霊を置くときは道路の中央を狙ってください。既存の幽霊能力には誘導範囲3・持続4秒・クールタイム5秒があり、遠い脇道の奥へ一度で誘導できるという意味ではありません。

Stage_02のルート・World・Systemsを原点基準に整理しました。Stage_01は変更していません。元の落石2つは `World/LegacyHazards`、元のDryTreeは `World/Environment/DryTree` に残して無効化しています。共通Prefab自体には変更を加えていません。

## Unityで確認する手順

1. GitHub Desktopでこの変更のブランチを取得し、Unity **6000.3.21f1** でプロジェクトを開きます。ConsoleでMissing Script・参照・インポートエラーがないか確認してください。
2. `Assets/Project/Scenes/GameScene.unity` を開きます。
3. Hierarchyの **StageLoader** を選択し、Inspectorで **Use Editor Test Stageをオン、Editor Test Stage Idを2** にします。これで年表の解放状態に関係なくステージ2を確認できます。
4. Gameビューを **16:9** にしてPlayを押します。左下の橋に信長、右上に門、左下に風・幽霊、右下に開始ボタンが出ることを確認します。観察中のBGMは前回の音対応を引き継いでいます。
5. 「タップで時を動かす」を押します。橋 → 中央の道 → 右上の門へ移動してクリアするか確認します。静的検証では、信長のColliderと壁の余裕を含めて全7区間が通れることを確認していますが、Unityの物理挙動はまだ検証していません。
6. 風で川・柵・岩へ向けて押し、壁を抜けず、道の内側で止まるか確認します。幽霊を近くの脇道の中央へ置き、誘導と通常経路への復帰を確認します。
7. ポーズ→再開、ポーズからリトライ、クリアからリトライ、年表へ戻る操作を試します。リトライで橋の開始位置へ戻り、風3回・幽霊2回に回復するか確認します。
8. 1280×720・1920×1080・横長画面でUIの見え方を確認します。静的には16:9・21:9・4:3で主要UIの矩形が画面内に収まりますが、文字の描画や背景の見切れはUnityで確認が必要です。
9. 検証後は **Use Editor Test Stageをオフ** に戻します。Titleからの通常プレイでは、ステージ1クリア後に年表のステージ2を選択してください。

当たり判定を微調整するときは、Stage_02.prefabを開き、上記Obstacleの各子オブジェクトの **PolygonCollider2D → Edit Collider** で道路の境界を動かします。経由地点は `World/Route`、信長のPathfinder参照は `World/Charactors/Nobunaga` にあります。

## 実行した検証と限界

- 全22シーン／PrefabのYAML、Component所有者、親子関係、追加fileID参照、Camera・AudioListener・EventSystem・Audioの重複を検証。
- 背景のSpriteサブアセット、既存GUID、経由地点・ゴール・Pathfinder・UI参照を検証。
- 信長の半径0.4＋Obstacle Skin 0.02を考慮した道路判定。主経路7区間の最小壁距離は約0.493で、必要な0.42を上回る。
- 0.15単位の探索グリッドで、左の脇道・左上の奥・右の脇道への経路と主経路への帰り道を幾何学的に検証。左上の道は最初の検査で狭すぎたため、境界を調整して再検証済み。
- 川・中央の柵・関所・下側の岩場の代表点が通行禁止領域内であることを検証。
- 追加UI文字の日本語グリフと主要UIの画面内配置を検証。
- `git diff --check` 成功。画像と既存metaは変更なし。

検証スクリプトは `Tools/validate_stage02.py`。Pythonで再確認する場合は次を実行します。

```sh
python -m pip install PyYAML shapely
python Tools/validate_stage02.py
```

このスクリプトは保存されたColliderと経路の静的検査です。Unityの実際のCollider.Cast、入力、再描画、音の再生を実行するものではありません。Unity Editorがこのクラウド環境にないため、コンパイル・Play Mode・聴感確認は未実施です。

暗殺者・攻撃予告音・ノーマル／ハード切替は未実装です。既存StageDirectorの「45秒経過でもクリア」も今回変更していません。音源の空欄（幽霊SEなど）は `DEVELOPMENT_HANDOFF.md` の音設定を参照してください。
