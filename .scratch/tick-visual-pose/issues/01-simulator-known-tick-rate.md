# 01: 模拟器能以已知 Tick 率开钟

**What to build:** 不走流程核也能把模拟器开起来：用已知 Tick 率开钟、登记实体、喂 dt，权威位姿仍按固定拍跳变。这一刀先把规格里的唯一接缝立住，后面的画面位姿测试都走同一条路。

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] EditMode 能以已知 Tick 率启动模拟器并开钟，不必先进入对局或加载关卡
- [x] 喂小于一拍的 dt：权威位置不变；喂满一拍：权威位置按该拍步进
- [x] 现有主机核仍能按配置资源启动时钟，注入已知 Tick 率不是第二套对局时钟
- [x] 测试只通过模拟器公开 Update 驱动，不测 Tick 累加器私有字段、不测流程核意图

## Answer

模拟器增加 `Init(tickRate, maxTicks)`，与房主核走配置的 `Init()` 共用开钟。EditMode 接缝：`SimulatorVisualPoseTests`。

## Comments

代码已接上 `Init(tickRate, maxTicks)`。EditMode 测试已通过。
