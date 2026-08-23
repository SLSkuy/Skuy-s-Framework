# Network Layer and MultiPlaySystem

## Network Responsibility

`Assets/Scripts/Network/` owns transport-independent client/server entry points, KCP/TCP transport abstractions, protobuf serialization, and message dispatch. It does not own gameplay entity simulation or replication policy.

## MultiPlaySystem Responsibility

`Assets/Scripts/GamePlay/MultiPlaySystem/` is the game-specific synchronization runtime for the third-person ARPG prototype. It contains:

- `NetworkTimeSystem` and `NetworkTickSystem` for the shared fixed tick.
- `NetworkObjectIdentity` and `EntitySimulationMode` for scene identity and role metadata.
- `CharacterReplicationSystem` and `CharacterReplicationEntry` for explicit character registration.
- Input validation/buffering, prediction history coordination, snapshot interpolation, and presentation adapters.
- The local client/server test simulator.

## Role Flow

```text
Network transport events
        |
        v
CharacterReplicationSystem
  input -> validate -> authority queue -> EntitySystem.EntitySimulation.Step
  snapshot -> predict reconciliation or replica interpolation
```

Authority, Predict, and LocalPlay share the EntitySystem simulation path. Replica is presentation-only.

## Protocol and Proxy Boundaries

Protocol remains under `Assets/Scripts/GamePlay/Protocol/Generated/`; generated classes keep the `NetSync` namespace and must not be edited manually. Proxy remains under `Assets/Scripts/GamePlay/Proxy/` and is not merged into MultiPlaySystem.

## Dependency Direction

```text
Framework
   ↑
Network (transport and dispatch)
   ↑
MultiPlaySystem (role-aware gameplay sync)
   ↑
EntitySystem (consumed simulation/state contracts)
```

In code terms, MultiPlaySystem consumes EntitySystem and Network services. EntitySystem has no dependency on MultiPlaySystem.
