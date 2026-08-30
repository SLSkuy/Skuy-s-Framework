## ADDED Requirements

### Requirement: Battle manager does not load scenes
战局管理器 MUST NOT 请求加载场景，MUST NOT 订阅场景加载完成或失败事件，MUST NOT 将菜单到第一张玩法图的导航作为开战合同的一部分。开战 API MUST 仅在调用时尝试启动对局，MUST NOT 以「先切场景再开战」封装菜单流程。

#### Scenario: Start match does not load a scene
- **WHEN** 调用方请求开战且当前不在玩法场景
- **THEN** 战局管理器 MUST NOT 因此发起场景加载

#### Scenario: No scene-load facade on battle
- **WHEN** 检查战局管理器对菜单单机的公开合同
- **THEN** MUST NOT 存在将建房、切场景与开战绑在同一入口的战局方法作为正式菜单路径
