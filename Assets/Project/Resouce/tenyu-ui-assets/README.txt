天祐 ―TENYU― 追加UI素材（Unity 6.3 Universal 2D）

収録素材
- PauseButton.png：ゲーム中の一時停止ボタン
- StageNode_Clear.png：クリア済み年表ノード
- StageNode_Current.png：挑戦中・選択可能ノード
- StageNode_Locked.png：未解放ノード
- TimelineRail.png：横スクロール年表レール
- StageDetailPanel.png：ステージ情報表示用の空パネル
- StageThumbnailFrame.png：ステージプレビュー画像用の枠
- AbilitySlot.png：能力アイコン共通スロット
- Ability_Wind.png：風能力アイコン
- Ability_Thunder.png：雷能力アイコン
- SelectionMarker.png：対象物・地形の選択マーカー
- TimelineArrow_Left.png：年表の左スクロール矢印（右向きはUnityで左右反転）
- LabelSmall.png：時間・回数・状態名用の汎用小型ラベル
- TitleTapDecoration.png：タイトル画面のタップ開始文言に添える、囲みなしの筆装飾
- TapIndicator.png：タップ操作を示すアニメーション用アイコン
- StartTimeButton.png：「時を動かす」メイン操作ボタン
- ObserveIcon.png：観察・確認フェーズ用アイコン
- SettingsIcon.png：設定用アイコン
- AudioIcon.png：音量・サウンド用アイコン
- SliderTrack.png：設定画面用スライダーレール
- SliderHandle.png：設定画面用スライダーつまみ
- ResultHeadline.png：成功・失敗で共用する結果見出し背景
- HowToPlayIcon.png：遊び方・チュートリアル用アイコン

推奨Import Settings
- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single
- Mesh Type: Full Rect
- Alpha Is Transparency: ON
- Filter Mode: Bilinear
- Compression: None（制作中）

UIの文字・数字・ステージ名は画像に焼き込まず、TextMeshProで重ねてください。
StageDetailPanelとLabelSmallは、固定したい角を避けてSprite EditorのBorderを設定し、Image TypeをSlicedにします。
TimelineArrow_Leftの右向き版はRectTransformのScale Xを-1にするか、Imageの向きを反転して共用できます。
TitleTapDecorationの空いた中央付近に、TextMeshProで「TAP TO START」または「画面をタップ」を配置してください。装飾自体にはButtonを付けず、全画面の透明Imageまたは入力管理スクリプトでタップを受けます。
TapIndicatorはCanvasGroupのAlphaとRectTransformのScaleを周期的に変えると、軽量なタップ誘導アニメーションになります。
SliderTrackはImage TypeをSliced、SliderHandleはUnity UI SliderのHandle Rectに設定してください。
ResultHeadlineは成功時はそのまま、失敗時はImage.colorを低彩度の灰朱色にして共用できます。
