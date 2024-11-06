using BlueSky.Data;
using BlueSky.BuildingManipulation;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField]
    private List<TKey> keys = new List<TKey>();

    [SerializeField]
    private List<TValue> values = new List<TValue>();

    // save the dictionary to lists
    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        foreach (KeyValuePair<TKey, TValue> pair in this)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    // load dictionary from lists
    public void OnAfterDeserialize()
    {
        this.Clear();

        if (keys.Count != values.Count)
        {
            throw new System.Exception(string.Format("there are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable."));
        }
        for (int i = 0; i < keys.Count; i++)
        {
            this.Add(keys[i], values[i]);
        }
    }
}

[Serializable]
public class DictionaryOfIntAndGameObjects : SerializableDictionary<int, List<GameObject>> { }

[Serializable]
public class DictionaryOfIntAndFeatures : SerializableDictionary<int, Features> { }

[Serializable]
public class DictionaryOfIntAndBuildings : SerializableDictionary<int, Building> { }

[Serializable]
public class DictionaryOfStringAndGameObjects: SerializableDictionary<string, GameObject> { }

[Serializable]
public class DictionaryOfGameObjectsAndBuildings : SerializableDictionary<GameObject, Building> { }

[Serializable]
public class DictionaryOfIntAndDecalProjectors : SerializableDictionary<int, DecalProjector> { }

[Serializable]
public class DictionaryOfStringAndBuildingHighlight : SerializableDictionary<string, BuildingHighlight> { }

[Serializable]
public class DictionaryOfStringAndBuildingHighlights : SerializableDictionary<string, List<BuildingHighlight>> { }

[Serializable]
public class DictionaryOfIntAndBuildingHighlights : SerializableDictionary<int, BuildingHighlight> { }

[Serializable]
public class DictionaryOfLatLongAndBuildings : SerializableDictionary<List<double>, Building> { }