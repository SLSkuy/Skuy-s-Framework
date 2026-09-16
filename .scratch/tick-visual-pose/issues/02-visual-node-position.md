# 02: 画面节点按残差插位移

**What to build:** 本机 Tick 推进的实体有画面节点（包住 mesh）。碰撞根仍按 Tick 写权威位置；步进结束后按时钟残差把画面位置插在上一拍与当前拍之间。没有新 Tick 的渲染帧画面继续走；一帧追上多拍时只插最后两拍。模拟开始前画面节点回到单位局部。Replica 不步进，也不走这套残差位移。

**Blocked by:** 01 模拟器能以已知 Tick 率开钟

**Status:** resolved

- [x] CharacterController 仍在实体根上；画面节点包住 mesh；呈现时根位置仍是权威，画面世界位置落在上一拍与当前拍之间
- [x] 一帧没有新 Tick 时权威不动，画面仍随残差朝当前权威前进
- [x] 一帧追上多拍后，画面只由最后两拍权威位置决定
- [x] 每拍步进前画面节点是单位局部，碰撞不跟画面偏移走
- [x] Replica 不 Collect、不 Step，权威与画面都不被本刀残差驱动
- [x] 停钟后不再改画面节点；测试仍只走模拟器公开 Update

## Answer

画面节点包住 mesh（及视角节点）。模拟器在全部 Tick 之后按残差写画面位移。Replica 不呈现。EditMode 接缝：`SimulatorVisualPoseTests`。

## Comments

EditMode 测试已通过。
