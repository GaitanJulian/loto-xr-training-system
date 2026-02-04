using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Project.Core;
using Project.Procedure;
using Project.Procedure.Conditions;
using Project.Actions.Mapping;

public static class ProcedureValidationMenu
{
    [MenuItem("Tools/XR Training/Validate Selected Procedure")]
    public static void ValidateSelectedProcedure()
    {
        var procedure = Selection.activeObject as ProcedureDefinitionSO;
        if (procedure == null)
        {
            Debug.LogError("Select a ProcedureDefinitionSO asset first.");
            return;
        }

        var catalog = FindCatalog();
        if (catalog == null)
        {
            Debug.LogError("No StateKeyCatalogSO found. Create one and add your valid bool keys.");
            return;
        }

        ValidateProcedure(procedure, catalog);
    }

    [MenuItem("Tools/XR Training/Validate Selected Mapping Set")]
    public static void ValidateSelectedMappingSet()
    {
        var set = Selection.activeObject as ActionMappingSetSO;
        if (set == null)
        {
            Debug.LogError("Select an ActionMappingSetSO asset first.");
            return;
        }

        var catalog = FindCatalog();
        if (catalog == null)
        {
            Debug.LogError("No StateKeyCatalogSO found. Create one and add your valid bool keys.");
            return;
        }

        ValidateMappingSet(set, catalog);
    }

    private static StateKeyCatalogSO FindCatalog()
    {
        var guids = AssetDatabase.FindAssets("t:StateKeyCatalogSO");
        if (guids == null || guids.Length == 0) return null;

        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<StateKeyCatalogSO>(path);
    }

    private static void ValidateProcedure(ProcedureDefinitionSO procedure, StateKeyCatalogSO catalog)
    {
        int errors = 0;
        int warnings = 0;

        if (procedure.steps == null || procedure.steps.Length == 0)
        {
            Debug.LogError($"Procedure '{procedure.name}' has no steps.");
            return;
        }

        var visitedConditions = new HashSet<ConditionSO>();

        for (int i = 0; i < procedure.steps.Length; i++)
        {
            var step = procedure.steps[i];
            if (step == null)
            {
                errors++;
                Debug.LogError($"Procedure '{procedure.name}' has a null step at index {i}.");
                continue;
            }

            if (step.completionCondition == null)
            {
                warnings++;
                Debug.LogWarning($"Step '{step.name}' has no completionCondition.");
                continue;
            }

            ValidateCondition(step.completionCondition, catalog, visitedConditions, ref errors, ref warnings);
        }

        Debug.Log($"Validation finished for Procedure '{procedure.name}'. Errors: {errors}, Warnings: {warnings}");
    }

    private static void ValidateMappingSet(ActionMappingSetSO set, StateKeyCatalogSO catalog)
    {
        int errors = 0;
        int warnings = 0;

        if (set.mappings == null || set.mappings.Length == 0)
        {
            Debug.LogWarning($"Mapping set '{set.name}' has no mappings.");
            return;
        }

        for (int i = 0; i < set.mappings.Length; i++)
        {
            var m = set.mappings[i];
            if (m == null)
            {
                warnings++;
                Debug.LogWarning($"Mapping set '{set.name}' has a null mapping at index {i}.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(m.boolKey))
            {
                warnings++;
                Debug.LogWarning($"Mapping '{m.name}' has empty boolKey.");
                continue;
            }

            if (!catalog.Contains(m.boolKey))
            {
                errors++;
                Debug.LogError($"Mapping '{m.name}' uses boolKey '{m.boolKey}' not found in StateKeyCatalog.");
            }
        }

        Debug.Log($"Validation finished for MappingSet '{set.name}'. Errors: {errors}, Warnings: {warnings}");
    }

    private static void ValidateCondition(
        ConditionSO condition,
        StateKeyCatalogSO catalog,
        HashSet<ConditionSO> visited,
        ref int errors,
        ref int warnings)
    {
        if (condition == null) return;
        if (visited.Contains(condition)) return;
        visited.Add(condition);

        if (condition is BoolFlagConditionSO boolFlag)
        {
            if (string.IsNullOrWhiteSpace(boolFlag.key))
            {
                warnings++;
                Debug.LogWarning($"Condition '{boolFlag.name}' has empty key.");
                return;
            }

            if (!catalog.Contains(boolFlag.key))
            {
                errors++;
                Debug.LogError($"Condition '{boolFlag.name}' uses key '{boolFlag.key}' not found in StateKeyCatalog.");
            }
            return;
        }

        if (condition is CompositeConditionSO composite)
        {
            if (composite.conditions == null || composite.conditions.Length == 0)
            {
                warnings++;
                Debug.LogWarning($"Composite condition '{composite.name}' has no sub-conditions.");
                return;
            }

            foreach (var sub in composite.conditions)
                ValidateCondition(sub, catalog, visited, ref errors, ref warnings);

            return;
        }

        warnings++;
        Debug.LogWarning($"Condition '{condition.name}' is of type '{condition.GetType().Name}' and has no validator rule yet.");
    }
}
