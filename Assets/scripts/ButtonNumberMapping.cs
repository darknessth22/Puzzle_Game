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

public class ButtonNumberMapping : MonoBehaviour
{
    [Header("Button-Number Mappings")]
    [Tooltip("Define which buttons correspond to which number objects")]
    [SerializeField]
    public List<ButtonNumberPair> buttonNumberPairs = new List<ButtonNumberPair>();

    private Dictionary<GameObject, ButtonNumberPair> buttonToPairMap = new Dictionary<GameObject, ButtonNumberPair>();
    private Dictionary<int, ButtonNumberPair> numberToPairMap = new Dictionary<int, ButtonNumberPair>();

    private void Awake()
    {
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
        ButtonNumberPair existingPair = null;
        if (buttonToPairMap.TryGetValue(button, out existingPair))
        {
            existingPair.numberValue = numberValue;
            existingPair.numberObject = numberObject;

            numberToPairMap[numberValue] = existingPair;
        }
        else
        {
            var newPair = new ButtonNumberPair(button, numberValue, numberObject);
            buttonNumberPairs.Add(newPair);

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

        return -1;
    }

    public GameObject GetButtonForNumber(int numberValue)
    {
        ButtonNumberPair pair;
        if (numberToPairMap.TryGetValue(numberValue, out pair))
        {
            return pair.button;
        }

        return null;
    }

    public GameObject GetNumberObjectForButton(GameObject button)
    {
        if (button == null) return null;

        ButtonNumberPair pair;
        if (buttonToPairMap.TryGetValue(button, out pair))
        {
            return pair.numberObject;
        }

        return null;
    }

    public GameObject GetNumberObjectForNumber(int numberValue)
    {
        ButtonNumberPair pair;
        if (numberToPairMap.TryGetValue(numberValue, out pair))
        {
            return pair.numberObject;
        }

        return null;
    }

    public bool IsCorrectMatch(GameObject button, int numberValue)
    {
        if (button == null) return false;

        ButtonNumberPair pair;
        if (buttonToPairMap.TryGetValue(button, out pair))
        {
            return pair.numberValue == numberValue;
        }

        return false;
    }

    public List<ButtonNumberPair> GetAllPairs()
    {
        return new List<ButtonNumberPair>(buttonNumberPairs);
    }
}
