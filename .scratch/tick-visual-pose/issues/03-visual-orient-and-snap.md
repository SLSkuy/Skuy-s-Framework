# 03: 画面朝向插值与硬对齐

**What to build:** 位移已经平滑之后，身体和视角也要是画面位姿：呈现时 mesh 与 orientation 写成两拍之间的旋转，下一拍步进前掰回权威旋转，前进方向仍读权威。生成、传送、回滚写回后画面立刻等于权威，不从旧点滑过来。冲刺仍走普通两拍插值。相机继续跟 orientation。

**Blocked by:** 02 画面节点按残差插位移

**Status:** resolved

- [x] 呈现窗口内 mesh 与 orientation 落在上一拍与当前拍朝向之间；根旋转仍为单位
- [x] 下一拍步进开始前，mesh 前进方向与权威朝向一致，不会吃到上一帧画面旋转
- [x] 登记后的首次呈现、Teleport、回滚写回：画面位移与朝向立刻等于权威
- [x] 冲刺位移仍在两拍之间插值，不强制对齐
- [x] 相机无需另挂目标；orientation 在呈现窗口里已是画面朝向
- [x] 测试仍只走模拟器公开 Update

## Answer

呈现时 slerp mesh / orientation；Tick 前掰回权威。Teleport 与回滚写回立刻对齐。相机跟 `visual/orientation`。EditMode 接缝：`SimulatorVisualPoseTests`。

## Comments

EditMode 测试已通过。
