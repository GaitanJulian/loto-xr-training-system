**LOTO Project Context (Unity)**

Este documento resume la arquitectura real del proyecto en `Assets/_Project` para que otro desarrollador o una IA pueda continuar sin reexplicaciones.

---

**1) Estructura de carpetas (real)**

- `Assets/_Project/Scripts/Actions`  
  Contiene el sistema de acciones (`ActionType`, `ActionEvent`, `TargetId`, `TargetIdentity`) y el bus de eventos (`ActionBus`). Incluye mapeo de acciones a estado en `Mapping/`.

- `Assets/_Project/Scripts/Actions/Mapping`  
  ScriptableObjects de mapeo (`ActionMappingSO`, `ActionMappingSetSO`) que convierten acciones en cambios de `WorldState`.

- `Assets/_Project/Scripts/Actions/Debug`  
  UI de debug para disparar acciones manualmente (`ActionDebugUI`).

- `Assets/_Project/Scripts/Core`  
  `WorldState` (diccionario de bools) y `StateKeyCatalogSO` (catálogo de keys válidas).

- `Assets/_Project/Scripts/Procedure`  
  Motor procedural: `ProcedureRunner`, `ProcedureDefinitionSO`, `StepDefinitionSO`.

- `Assets/_Project/Scripts/Procedure/Conditions`  
  Condiciones de pasos: `ConditionSO` base, `BoolFlagConditionSO`, `CompositeConditionSO`.

- `Assets/_Project/Scripts/Procedure/Debug`  
  Driver de debug del procedimiento (`ProcedureDebugDriver`).

- `Assets/_Project/Scripts/Interaction`  
  XR/Interacciones: palanca breaker, botones, lockout/padlock/tag, sockets, gating de máquina.

- `Assets/_Project/Scripts/Interaction/Transformers`  
  `BreakerDetentRotateTransformer` (fork de Meta/Oculus para detents/snap).

- `Assets/_Project/Scripts/UI`  
  UI de procedimiento (`ProcedureHUD`).

- `Assets/_Project/Scripts/Utility`  
  Utilidades comunes (`SnapUtility`).

- `Assets/_Project/ScriptableObjects`  
  Assets de datos:
  - `Actions/` (`ActionMappingSet.asset`, `ActionMapping_*.asset`)
  - `Conditions/` (`Cond_*.asset`, `Debug_Flag.asset`)
  - `Steps/` (`Step_*.asset`)
  - `Procedures/` (`Procedure_LOTO_Simple.asset`, subcarpetas por tipo)
  - `State/` (`StateKeyCatalog.asset`)
  - `Scoring/` (vacío o no usado en scripts)

- `Assets/_Project/Prefabs`  
  Prefabs de botones (`Botones/`).

- `Assets/_Project/3D Models`  
  Modelos de Lockout, Padlock, Breaker, Button, etc.

Si hay nuevos ScriptableObjects deben vivir en `Assets/_Project/ScriptableObjects/*` según su tipo.

---

**2) Mapa de sistemas principales (arquitectura)**

**Actions**
- `ActionType` enum define acciones (ej: `ToggleBreakerOff`, `ApplyLock`, `AttachTag`, `MachineStart`, `VerifyIsolation`).
- `TargetId` enum identifica target físico lógico (ej: `Breaker_01`, `PadlockSocket_01`).
- `ActionEvent` encapsula `ActionType` + `TargetId`.
- `TargetIdentity` es un MonoBehaviour que asigna `TargetId` al GameObject.

**Procedure**
- `ProcedureRunner` es el núcleo: crea `WorldState`, escucha acciones, aplica mappings, evalúa condiciones y avanza pasos.
- `ProcedureDefinitionSO` define lista de pasos.
- `StepDefinitionSO` define `stepId`, `instruction` y `completionCondition`.
- `ConditionSO` + derivados evalúan el `WorldState`.
- `WorldState` es un diccionario `string -> bool`.

**Actions.Mapping**
- `ActionMappingSO` define reglas `ActionType + TargetId -> boolKey/boolValue`.
- `ActionMappingSetSO` agrupa mappings.
- `ProcedureRunner.HandleAction` aplica mappings y luego `EvaluateAndAdvance`.

**XR Interactions**
- `BreakerLeverControllerXR` lee estado físico de palanca y publica `ToggleBreakerOff`.
- `MachineButtonXR` publica `ActionEvent` al presionar y puede hacer gating por breaker.
- `LockoutSnapXR`, `PadlockSnapXR`, `TagSnapXR` hacen snap físico y publican acciones.
- `SnapObjectXR` + `SnapSocketXR` es un sistema genérico de snap alternativo.
- `BreakerDetentRotateTransformer` es el transformer del Meta SDK con detents.

**Machine state / gating**
- `MachineStateController` escucha `runner.OnActionPublished` y controla encendido de máquina.
- Opcionalmente usa `BreakerLeverControllerXR`, `ILotoLockState`, `IPanelDoorLock`.
- Si `TryStart` falla puede auto-publicar `VerifyIsolation`.

**LOTO objects**
- Lockout: `LockoutSnapXR` + `LockoutSocketXR` (montaje por estado del breaker, bloqueo de palanca).
- Padlock: `PadlockSnapXR` + `PadlockSocketXR` (solo se acepta si hay lockout montado).
- Tag: `TagSnapXR` + `TagSocketXR`.
- Locks: bloqueo de breaker vía `BreakerLeverControllerXR.SetLocked(true)`.

---

**3) Comunicación y contratos (evento/datos)**

**ActionBus / ProcedureRunner**
- Publica: `ProcedureRunner.PublishAction(ActionEvent)`
- Notifica externos: `ProcedureRunner.OnActionPublished`
- Escucha internamente: `ActionBus.OnAction += HandleAction`
- Datos: `ActionEvent` (ActionType + TargetId)
- ScriptableObjects: `ActionMappingSetSO` en `ProcedureRunner` define efectos sobre `WorldState`.

**ActionMappingSO**
- Método clave: `Apply(WorldState state)`
- Contrato: Si `Matches(ActionEvent)` -> `state.SetBool(boolKey, boolValue)`
- Configuración Inspector: `actionType`, `target`, `boolKey`, `boolValue`.

**Procedure**
- Publica: `OnStepChanged(StepDefinitionSO)`
- Usa: `StepDefinitionSO.completionCondition` (ConditionSO)
- Método clave: `EvaluateAndAdvance()`
- Datos: `WorldState` con keys de `StateKeyCatalogSO` (validación editor).

**XR Interactions**
- `BreakerLeverControllerXR.OnGrabEnded()` publica `ToggleBreakerOff` cuando llega a OFF.
- `MachineButtonXR.OnSelected()` publica `actionType` configurado.
- `TagSnapXR.TrySnap()` publica `AttachTag`.
- `PadlockSnapXR.TrySnap()` publica `ApplyLock`.
- `LockoutSnapXR.TrySnap()` publica `AttachLockoutDevice`.
- `SnapObjectXR` (genérico) publica `actionTypeOnSnap`.

**MachineStateController**
- Escucha: `runner.OnActionPublished += OnAction`
- Acciones que maneja: `PowerOff`, `MachineStart`, `TryStart`
- Puede publicar: `VerifyIsolation` si `TryStart` falla.
- Datos: `ActionEvent` filtrado por `TargetId` (`machineIdentity`).

**Interfaces**
- `ILotoLockState` y `IPanelDoorLock` existen, pero no hay implementaciones en `Assets/_Project` (TODO).

**Diagrama textual**
```
[XR Input] 
  -> ProcedureRunner.PublishAction(ActionEvent)
    -> OnActionPublished (MachineStateController, UI, etc.)
    -> ActionBus
      -> ProcedureRunner.HandleAction
        -> ActionMappingSO.Apply(WorldState)
        -> EvaluateAndAdvance()
          -> OnStepChanged
```

---

**4) Scripts clave y su rol**

| Script/Class | Ubicación | Rol | Métodos públicos relevantes | Dependencias (Inspector) |
|---|---|---|---|---|
| `ActionBus` | `Assets/_Project/Scripts/Actions/ActionBus.cs` | Bus simple de eventos de acción | `Publish(ActionEvent)` | Ninguna |
| `ActionEvent` | `Assets/_Project/Scripts/Actions/ActionEvent.cs` | Payload de acción | ctor, `ToString()` | N/A |
| `ActionType` | `Assets/_Project/Scripts/Actions/ActionType.cs` | Enum de acciones | N/A | N/A |
| `TargetId` | `Assets/_Project/Scripts/Actions/TargetId.cs` | Enum de targets | N/A | N/A |
| `TargetIdentity` | `Assets/_Project/Scripts/Actions/TargetIdentity.cs` | Identidad del objeto | `Id` | `TargetId` |
| `ActionMappingSO` | `Assets/_Project/Scripts/Actions/Mapping/ActionMappingSO.cs` | Regla de mapeo acción -> estado | `Apply(WorldState)`, `Matches(ActionEvent)` | `actionType`, `target`, `boolKey`, `boolValue` |
| `ActionMappingSetSO` | `Assets/_Project/Scripts/Actions/Mapping/ActionMappingSetSO.cs` | Colección de mappings | N/A | `mappings[]` |
| `ProcedureRunner` | `Assets/_Project/Scripts/Procedure/ProcedureRunner.cs` | Orquestador principal | `StartProcedure()`, `PublishAction()`, `EvaluateAndAdvance()` | `procedure`, `actionMappings` |
| `ProcedureDefinitionSO` | `Assets/_Project/Scripts/Procedure/ProcedureDefinitionSO.cs` | Definición de procedimiento | N/A | `steps[]` |
| `StepDefinitionSO` | `Assets/_Project/Scripts/Procedure/StepDefinitionSO.cs` | Definición de paso | N/A | `completionCondition` |
| `ConditionSO` | `Assets/_Project/Scripts/Procedure/Conditions/ConditionSO.cs` | Base de condiciones | `Evaluate(WorldState)` | N/A |
| `BoolFlagConditionSO` | `Assets/_Project/Scripts/Procedure/Conditions/BoolFlagConditionSO.cs` | Condición bool | `Evaluate` | `key`, `expectedValue` |
| `CompositeConditionSO` | `Assets/_Project/Scripts/Procedure/Conditions/CompositeConditionSO.cs` | AND/OR de condiciones | `Evaluate` | `conditions[]`, `op` |
| `WorldState` | `Assets/_Project/Scripts/Core/WorldState.cs` | Estado bool global | `SetBool`, `GetBool`, `HasBool` | N/A |
| `StateKeyCatalogSO` | `Assets/_Project/Scripts/Core/StateKeyCatalogSO.cs` | Catálogo de keys válidas | `Contains()` | `keys[]` |
| `ProcedureHUD` | `Assets/_Project/Scripts/UI/ProcedureHUD.cs` | UI de paso actual | N/A | `runner`, `stepText` |
| `MachineButtonXR` | `Assets/_Project/Scripts/Interaction/MachineButtonXR.cs` | Botón XR con gating | `OnSelected()`, `OnUnselected()` | `runner`, `targetIdentity`, `actionType`, `breaker` |
| `BreakerLeverControllerXR` | `Assets/_Project/Scripts/Interaction/BreakerLeverControllerXR.cs` | Palanca breaker, publica OFF | `OnGrabStarted()`, `OnGrabEnded()`, `SetLocked(bool)` | `runner`, `targetIdentity`, `leverAngleSource` |
| `MachineStateController` | `Assets/_Project/Scripts/Interaction/MachineStateController.cs` | Controla estado máquina | N/A (interno) | `runner`, `machineIdentity`, `breaker`, `lotoLockStateSource`, `panelDoorLockSource` |
| `LockoutSnapXR` | `Assets/_Project/Scripts/Interaction/LockoutSnapXR.cs` | Snap de lockout + bloqueo breaker | `OnGrabStarted()`, `TrySnap()`, `ApplyPadlock()` | `runner`, `grabbables` |
| `LockoutSocketXR` | `Assets/_Project/Scripts/Interaction/LockoutSocketXR.cs` | Socket de lockout | `GetActiveMount()` | `targetIdentity`, `mountOn`, `mountOff`, `_lever` |
| `PadlockSnapXR` | `Assets/_Project/Scripts/Interaction/PadlockSnapXR.cs` | Snap de padlock | `TrySnap()` | `runner`, `grabbables` |
| `PadlockSocketXR` | `Assets/_Project/Scripts/Interaction/PadlockSocketXR.cs` | Socket de padlock | `CanAcceptPadlock()`, `NotifyPadlockApplied()` | `targetIdentity`, `snapPose` |
| `TagSnapXR` | `Assets/_Project/Scripts/Interaction/TagSnapXR.cs` | Snap de tag | `TrySnap()` | `runner`, `grabbables` |
| `TagSocketXR` | `Assets/_Project/Scripts/Interaction/TagSocketXR.cs` | Socket de tag | N/A | `targetIdentity`, `snapPose` |
| `SnapObjectXR` | `Assets/_Project/Scripts/Interaction/SnapObjectXR.cs` | Sistema genérico de snap | N/A | `runner`, `actionTypeOnSnap`, `disableAfterSnap`, `breakerLockController` |
| `SnapSocketXR` | `Assets/_Project/Scripts/Interaction/SnapSocketXR.cs` | Socket genérico | N/A | `targetIdentity`, `snapPose` |
| `SnapUtility` | `Assets/_Project/Scripts/Utility/SnapUtility.cs` | Snap con conservación de escala | `SnapWorld()` | N/A |
| `BreakerDetentRotateTransformer` | `Assets/_Project/Scripts/Interaction/Transformers/BreakerDetentRotateTransformer.cs` | Detents/snap de palanca | `BeginTransform`, `UpdateTransform`, `EndTransform` | Meta XR/Oculus |
| `ProcedureValidationMenu` | `Assets/_Project/Editor/ProcedureValidationMenu.cs` | Validación de procedures/mappings | Menú Tools | Requiere `StateKeyCatalogSO` |

---

**5) Guía: dónde guardar código nuevo**

- Si es XR controller/interacción -> `Assets/_Project/Scripts/Interaction/`
- Si es transformer custom (Meta/Oculus) -> `Assets/_Project/Scripts/Interaction/Transformers/`
- Si es ScriptableObject de actions -> `Assets/_Project/Scripts/Actions/Mapping/` y asset en `Assets/_Project/ScriptableObjects/Actions/`
- Si es Procedure/Steps -> `Assets/_Project/Scripts/Procedure/` y assets en `Assets/_Project/ScriptableObjects/Procedures/` y `Assets/_Project/ScriptableObjects/Steps/`
- Si es Condition -> `Assets/_Project/Scripts/Procedure/Conditions/` y asset en `Assets/_Project/ScriptableObjects/Conditions/`
- Si es UI de procedimiento -> `Assets/_Project/Scripts/UI/`
- Si es core/estado global -> `Assets/_Project/Scripts/Core/`
- Si es utilitario genérico -> `Assets/_Project/Scripts/Utility/`

---

**6) Integraciones típicas (recetas)**

**Cómo agregar un nuevo botón XR que dispare `ActionType.X`**
1. Crear o duplicar un GameObject con `MachineButtonXR`.
2. Asignar `runner` y `targetIdentity` en Inspector.
3. Configurar `actionType = ActionType.X`.
4. Conectar el evento del wrapper Meta/Oculus a `MachineButtonXR.OnSelected()` y `OnUnselected()`.
5. Si requiere gating por breaker, asignar `breaker` y `requireBreakerOn`.

**Cómo agregar un nuevo paso al `ProcedureDefinitionSO`**
1. Crear un `StepDefinitionSO` en `Assets/_Project/ScriptableObjects/Steps/`.
2. Configurar `stepId`, `instruction` y `completionCondition`.
3. Agregarlo al array `steps` del `ProcedureDefinitionSO` correspondiente.

**Cómo agregar un mapping en `ActionMappingSetSO`**
1. Crear un `ActionMappingSO` en `Assets/_Project/ScriptableObjects/Actions/`.
2. Configurar `actionType`, `target` (o `None`), `boolKey`, `boolValue`.
3. Agregarlo al array `mappings` del `ActionMappingSetSO` que usa el `ProcedureRunner`.
4. Asegurar que `boolKey` existe en `StateKeyCatalogSO`.

**Cómo hacer gating usando `MachineStateController` + `runner.OnActionPublished`**
1. Usar `MachineStateController` en la máquina.
2. Asignar `runner` y `machineIdentity`.
3. Para bloquear por LOTO, proveer un componente que implemente `ILotoLockState`.  
   TODO: no hay implementación en `Assets/_Project`, debe crearse.
4. Para bloquear interacción de puerta, proveer implementación de `IPanelDoorLock`.  
   TODO: no hay implementación en `Assets/_Project`.

---

**Notas y TODOs detectados**
- `ILotoLockState` y `IPanelDoorLock` solo están definidos en `MachineStateController` y no hay implementaciones reales en el proyecto.
- `Scripts/Logging` está vacío.
- Hay dos sistemas de snap: específico (Lockout/Padlock/Tag) y genérico (SnapObjectXR/SnapSocketXR). Elegir uno y mantener consistencia para evitar duplicidad de lógica.

---
