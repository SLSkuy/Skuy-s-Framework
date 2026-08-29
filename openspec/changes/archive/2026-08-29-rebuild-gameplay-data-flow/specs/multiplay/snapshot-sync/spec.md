## REMOVED Requirements

### Requirement: One match room
**Reason**：联机玩法层耦合过高，本阶段拆除快照同步房间与 Host，改由独立战局名册承担玩家身份，同步延后。
**Migration**：成员与所有权改走 `gameplay/battle` 的玩家实体；Authority 生成与输入授权在后续同步 change 中重新接入，不得恢复本房间实现。

### Requirement: Client sends input and applies snapshots
**Reason**：客户端快照落地路径与传输、Host 缠在一起，本阶段删除联机客户端会话。
**Migration**：单机输入继续由 `simulator/local-play` 与 `gameplay/session-flow` 编排；网络输入与 Restore 在战局稳定后的同步 change 中重做。

### Requirement: Hosts mirror local session shape
**Reason**：联机 Host 虽同构单机，但仍直连传输并与复制残留并存，整体删除。
**Migration**：单机 Host 保留，由玩法会话编排器启动；联机 Host 待同步 change 再引入，且 MUST 只依赖战局与消息门面。

### Requirement: Existing sync test panel drives multiplay
**Reason**：同步测试面板是联机玩法入口，随联机框架一起移除，避免继续从面板拉起高耦合路径。
**Migration**：单机改由 `LocalPlayTestPanel` 经玩法会话编排器驱动；传输仍用 `NetworkTestPanel`。后续同步 change 再决定验收入口。
