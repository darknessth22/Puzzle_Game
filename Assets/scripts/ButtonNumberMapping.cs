using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ButtonNumberPair
{
    [Tooltip("The button that will be pressed")]
    public GameObject button;

    [Tooltip("The number value this button corresponds to (1-10)")]
    public int numberValue;

    [Tooltip("The number object that will light up when this button is pressed")]
    public GameObject numberObject;

    public ButtonNumberPair() { }

    public ButtonNumberPair(GameObject button, int numberValue, GameObject numberObject)
    {
        this.button = button;
        this.numberValue = numberValue;
        this.numberObject = numberObject;
    }
}

/// <summary>
/// This component manages the mappings between buttons and number objects.
/// To use this component:
/// 1. Add it to a GameObject in the scene
/// 2. In the Inspector, add entries to the Button-Number Mappings list
/// 3. For each entry, assign:
///    - The button GameObject
///    - The number value (1-10)
///    - The number object that should light up
/// </summary>
public class ButtonNumberMapping : MonoBehaviour
{
    [Header("Button-Number Mappings")]
    [Tooltip("Define which buttons correspond to which number objects")]
    [SerializeField]
    public List<ButtonNumberPair> buttonNumberPairs = new List<ButtonNumberPair>();

    // Dictionary for faster lookups
    private Dictionary<GameObject, ButtonNumberPair> buttonToPairMap = new Dictionary<GameObject, ButtonNumberPair>();
    private Dictionary<int, ButtonNumberPair> numberToPairMap = new Dictionary<int, ButtonNumberPair>();

    private void Awake()
    {
        // Initialize the dictionaries
        RebuildMappingDictionaries();
    }

    public void RebuildMappingDictionaries()
    {
        buttonToPairMap.Clear();
        numberToPairMap.Clear();

        foreach (var pair in buttonNumberPairs)
        {
            if (pair.button != null)
            {
                buttonToPairMap[pair.button] = pair;
            }

            numberToPairMap[pair.numberValue] = pair;
        }
    }

    public void SetMapping(GameObject button, int numberValue, GameObject numberObject)
    {
        // Check if this button already has a mapping
        ButtonNumberPair existingPair = null;
        if (buttonToPairMap.TryGetValue(button, out existingPair))
        {
            // Update the existing pair
            existingPair.numberValue = numberValue;
            existingPair.numberObject = numberObject;

            // Update the number mapping
            numberToPairMap[numberValue] = existingPair;
        }
        else
        {
            // Create a new pair
            var newPair = new ButtonNumberPair(button, numberValue, numberObject);
            buttonNumberPairs.Add(newPair);

            // Add to dictionaries
            buttonToPairMap[button] = newPair;
            numberToPairMap[numberValue] = newPair;
        }
    }

    public void ClearMappings()
    {
        buttonNumberPairs.Clear();
        buttonToPairMap.Clear();
        numberToPairMap.Clear();
    }

    public int GetNumberForButton(GameObject button)
    {
        if (button == null) return -1;

        ButtonNumberPair pair;
        if (buttonToPairMap.TryGetValue(button, out pair))
        {
            return pair.numberValue;
        }

        return -1; // No mapping found
    }

    public GameObject GetButtonForNumber(int numberValue)
    {
        ButtonNumberPair pair;
        if (numberToPairMap.TryGetValue(numberValue, out pair))
        {
            return pair.button;
        }

        return null; // No mapping found
    }

    public GameObject GetNumberObjectForButton(GameObject button)
    {
        if (button == null) return null;

        ButtonNumberPair pair;
        if (buttonToPairMap.TryGetValue(button, out pair))
        {
            return pair.numberObject;
        }

        return null; // No mapping found
    }

    public GameObject GetNumberObjectForNumber(int numberValue)
    {
        ButtonNumberPair pair;
        if (numberToPairMap.TryGetValue(numberValue, out pair))
        {
            return pair.numberObject;
        }

        return null; // No mapping found
    }

    public bool IsCorrectMatch(GameObject button, int numberValue)
    {
        if (button == null) return false;

        ButtonNumberPair pair;
        if (buttonToPairMap.TryGetValue(button, out pair))
        {
            return pair.numberValue == numberValue;
        }

        return false; // No mapping found
    }

    // Get all button-number pairs
    public List<ButtonNumberPair> GetAllPairs()
    {
        return new List<ButtonNumberPair>(buttonNumberPairs);
    }

    // This method has been removed as mappings are now defined in the Inspector
}
