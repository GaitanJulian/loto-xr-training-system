# LOTO XR Training System

Data-driven XR training system prototype for industrial **Lockout / Tagout (LOTO)** procedures built with Unity.

This project focuses on **procedure logic, scalability, and clean architecture**, not just XR interaction demos.

---

## 🎯 Purpose

Demonstrate how complex industrial procedures can be modeled as **data-driven workflows** that are:

- Independent from UI and XR hardware
- Easily extensible to new procedures
- Reusable across VR, MR, and desktop simulations

---

## 🧠 Core Concepts

- **Procedure-driven architecture** using ScriptableObjects
- **Action-based system** decoupled from interaction source
- **WorldState + Conditions** for deterministic step validation
- Designed to scale from simple LOTO cases to complex industrial workflows

---

## 🧱 Architecture Overview

- `ProcedureRunner` – Executes procedures step by step
- `ActionBus` – Centralized action event system
- `ActionMapping` – Translates physical actions into logical state changes
- `WorldState` – Tracks procedural conditions
- UI Debug layer used for early validation (XR-independent)

---

## 🛠️ Tech Stack

- Unity (XR-ready)
- C#
- ScriptableObjects
- OpenXR / Meta XR compatible architecture

---

## 🚧 Status

Prototype in active development.  
XR interaction layer will be added without modifying core procedure logic.

---

## 📌 Author

Developed by **Julián Andrés Gaitán Hernández**  
XR / Unity Developer
